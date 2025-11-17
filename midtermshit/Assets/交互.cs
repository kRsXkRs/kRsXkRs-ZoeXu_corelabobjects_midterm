using UnityEngine;

public class GateOnly : MonoBehaviour
{
    [Header("基础")]
    public string playerTag = "Player";
    public KeyCode key = KeyCode.E;

    [Header("总数 & 目标")]
    public int totalRequired = 4;          // 需要激活的总点数
    public GameObject targetToHide;         // 全部完成后要消失的物体（任意一个点上填就行）

    [Header("可选：提示/面板（不用就留空）")]
    public GameObject prompt;               // “按E”提示
    public GameObject panel;                // 信息面板（按E时开关）

    // —— 共享计数（所有 GateOnly 共用）——
    static int activatedCount = 0;

    bool inside = false;
    bool activated = false;

    void Awake()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
        if (prompt) prompt.SetActive(false);
        if (panel) panel.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inside = true;
        if (prompt && !activated) prompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inside = false;
        if (prompt) prompt.SetActive(false);
        if (panel) panel.SetActive(false);
    }

    void Update()
    {
        if (!inside) return;

        if (Input.GetKeyDown(key))
        {
            // 首次激活
            if (!activated)
            {
                activated = true;
                if (prompt) prompt.SetActive(false);

                activatedCount++;
                // 全部完成 → 隐藏目标
                if (activatedCount >= totalRequired && targetToHide)
                    targetToHide.SetActive(false);
            }

            // （可选）开关面板
            if (panel) panel.SetActive(!panel.activeSelf);
        }

        // Esc 关闭面板（可选）
        if (panel && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            panel.SetActive(false);
    }

    // （可选）编辑器里点播放时重置共享计数
#if UNITY_EDITOR
    void OnEnable()
    {
        // 第一个启用的 Gate 重置计数
        if (activatedCount < 0 || Time.frameCount <= 1) activatedCount = 0;
    }
#endif
}
