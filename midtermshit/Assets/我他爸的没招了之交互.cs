using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class InputPanelAllInOne : MonoBehaviour
{
    [Header("触发")]
    public string playerTag = "Player";
    public KeyCode key = KeyCode.E;
    public GameObject prompt;

    [Header("面板与文本")]
    public GameObject panel;         // 只包含输入框等可关闭的 UI
    public TMP_InputField input;     // 面板里的 TMP_InputField
    public TMP_Text output;          // 镜像显示（不含标签）
    public bool startClosed = true;

    [Header("面板打开时需要禁用的组件（可选）")]
    public Behaviour[] disableWhileOpen;
    public bool unlockCursorWhenOpen = true;

    [Header("固定模板")]
    [TextArea(3, 5)]
    public string template =
        "Your name:\n" +
        "Your birthday:\n" +
        "Your thoughts about the museum:";

    bool inside;
    string[] labels;

    //—— 安全重构新增 ——//
    bool _fixPending;        // 延后一帧修模板
    bool _closingPending;    // 延后一帧真正关闭
    bool _suppress;          // 抑制递归回调

    void Awake()
    {
        // 触发器
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (prompt) prompt.SetActive(false);
        if (panel && startClosed) panel.SetActive(false);

        // 预解析标签
        var lines = template.Replace("\r", "").Split('\n');
        labels = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            int k = lines[i].IndexOf(':');
            labels[i] = (k >= 0) ? lines[i].Substring(0, k + 1) : lines[i];
        }

        // 关闭 TMP 的 ESC 还原 / 失活重置
        if (input)
        {
            input.restoreOriginalTextOnEscape = false;
            input.resetOnDeActivation = false;
        }
    }

    void OnEnable()
    {
        if (!input) return;

        if (string.IsNullOrEmpty(input.text))
            input.SetTextWithoutNotify(template);

        input.onValueChanged.AddListener(OnInputChanged);
        MirrorAnswers(input.text);
    }

    void OnDisable()
    {
        if (input) input.onValueChanged.RemoveListener(OnInputChanged);
    }

    // —— 触发进/出 —— 
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inside = true;
        if (prompt && (panel == null || !panel.activeSelf)) prompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inside = false;
        if (prompt) prompt.SetActive(false);
        SafeClose();
    }

    void Update()
    {
        if (!panel || !input) return;

        // 面板开且输入框聚焦：Esc 关闭（延后一帧执行），其它键不打扰
        if (panel.activeSelf && input.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) SafeClose();
            return;
        }

        // 在圈内，面板没开 → 按 E 打开
        if (inside && !panel.activeSelf && Input.GetKeyDown(key))
        {
            SafeOpen();
            return;
        }

        // 面板已开但输入框不在焦点：E 或 Esc 都能关
        if (panel.activeSelf && (Input.GetKeyDown(key) || Input.GetKeyDown(KeyCode.Escape)))
        {
            SafeClose();
        }
    }

    // —— 打开/关闭（安全）——
    void SafeOpen()
    {
        panel.SetActive(true);

        // 重新挂监听（可能上次关闭时解绑了）
        if (input != null)
        {
            input.onValueChanged.RemoveListener(OnInputChanged);
            input.onValueChanged.AddListener(OnInputChanged);
        }

        if (string.IsNullOrEmpty(input.text))
            input.SetTextWithoutNotify(template);

        // 聚焦到输入框
        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(input.gameObject);
        input.ActivateInputField();

        // 光标放到第一行冒号后
        int idx = input.text.IndexOf(':');
        input.caretPosition = (idx >= 0) ? idx + 1 : input.text.Length;

        if (prompt) prompt.SetActive(false);

        foreach (var b in disableWhileOpen) if (b) b.enabled = false;

        if (unlockCursorWhenOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void SafeClose()
    {
        if (!panel || !panel.activeSelf) return;

        // 先拿走焦点，别在同帧又处理输入事件
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        input.DeactivateInputField();

        // 真正关闭延后一帧
        _closingPending = true;
    }

    void DoCloseNow()
    {
        // 关之前解绑，避免关闭过程中回调再触发
        if (input) input.onValueChanged.RemoveListener(OnInputChanged);

        panel.SetActive(false);

        foreach (var b in disableWhileOpen) if (b) b.enabled = true;

        if (prompt && inside) prompt.SetActive(true);


        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (_fixPending)
        {
            _fixPending = false;
            if (!input) return;

            string fixedStr = FixTemplate(input.text);
            if (fixedStr != input.text)
            {
                _suppress = true;
                int caret = input.caretPosition;
                input.SetTextWithoutNotify(fixedStr);
                input.caretPosition = Mathf.Clamp(caret, 0, input.text.Length);
                _suppress = false;
            }
        }

        if (_closingPending)
        {
            _closingPending = false;
            DoCloseNow();
        }
    }

    // —— 输入变动：只做镜像；修模板延后一帧 —— 
    void OnInputChanged(string _)
    {
        if (_suppress) return;
        if (input.wasCanceled) return; // Esc 取消的那次变更直接忽略

        MirrorAnswers(input.text);
        _fixPending = true;
    }

    // —— 工具函数 —— 
    string FixTemplate(string fullText)
    {
        var cur = fullText.Replace("\r", "").Split('\n');
        System.Text.StringBuilder fix = new System.Text.StringBuilder();
        for (int i = 0; i < labels.Length; i++)
        {
            string lab = labels[i];
            string line = (i < cur.Length) ? cur[i] : "";
            if (!line.StartsWith(lab))
            {
                int p = line.IndexOf(':');
                string user = (p >= 0 && p + 1 < line.Length) ? line.Substring(p + 1) : line;
                line = lab + user;
            }
            fix.Append(line);
            if (i < labels.Length - 1) fix.Append('\n');
        }
        return fix.ToString();
    }

    void MirrorAnswers(string fullText)
    {
        if (!output) return;
        var rows = fullText.Replace("\r", "").Split('\n');
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < labels.Length; i++)
        {
            string line = (i < rows.Length) ? rows[i] : labels[i];
            int p = line.IndexOf(':');
            string user = (p >= 0) ? line.Substring(p + 1) : line;
            sb.Append(user.TrimStart());
            if (i < labels.Length - 1) sb.Append('\n');
        }
        output.text = sb.ToString();
    }
}
