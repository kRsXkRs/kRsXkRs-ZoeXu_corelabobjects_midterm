using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoffinEndingController : MonoBehaviour
{
    [Header("玩家相关")]
    public Transform player;                 // 玩家根物体
    public MonoBehaviour playerController;   // 玩家控制脚本（移动/输入那个）

    [Header("棺材相关")]
    public Transform coffinInsidePoint;      // 棺材里玩家最终躺下的位置（朝向也一起用）
    public Animator coffinAnimator;          // 棺材盖 Animator
    public string coffinCloseTriggerName = "Close";

    [Header("玩家移动参数")]
    public float liftHeight = 1.5f;          // 第一步：原地举多高
    public float hoverExtraHeight = 0.5f;    // 第二步：在棺材上方再高出多少（保证在棺材上空，不穿模）
    public float liftDuration = 0.4f;        // 举起来时间
    public float moveToHoverDuration = 0.7f; // 从玩家上方移动到棺材上方 & 旋转成躺姿
    public float dropDuration = 0.4f;        // 从棺材上方垂直放下到棺材里的时间
    public Vector3 rotationOffsetEuler;      // 躺姿的额外旋转（比如 X = 90）

    [Header("黑屏 & 结束UI")]
    public CanvasGroup fadeCanvasGroup;      // 黑屏用的 CanvasGroup（黑色Image上）
    public float fadeDuration = 1.5f;        // 黑屏渐变时间
    public TextMeshProUGUI endingText;       // 结束文本（可选）
    public float endingTextDelay = 0.5f;     // 黑屏后多等一会再出现文本
    public string endSceneName = "";         // 如果想直接切场景，可以写场景名（留空则不切）

    [Header("交互")]
    public KeyCode interactKey = KeyCode.E;  // 按键（默认E）
    public GameObject interactPrompt;        // “按E”提示UI（可选）

    bool playerInRange = false;
    bool isEnding = false;

    // 让触发器能识别到玩家的父/子物体
    bool IsPlayer(Collider other)
    {
        if (player == null) return false;
        if (other.transform == player) return true;
        if (other.transform.IsChildOf(player)) return true;
        if (player.IsChildOf(other.transform)) return true;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInRange = true;
            if (interactPrompt != null)
                interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInRange = false;
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange || isEnding) return;

        if (Input.GetKeyDown(interactKey))
        {
            StartCoroutine(PlayEndingRoutine());
        }
    }

    private IEnumerator PlayEndingRoutine()
    {
        isEnding = true;

        // 关掉“按E”提示
        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        // 先锁住玩家：不再接受输入，也不被物理乱推
        if (playerController != null)
            playerController.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        Collider col = player.GetComponent<Collider>();
        if (col != null)
        {
            // 不想真的“撞”棺材，就变 trigger；视觉上不穿模是靠我们这条路径来保证的
            col.isTrigger = true;
        }

        // ====== 1. 原地往上举 ======
        Vector3 startPos = player.position;
        Quaternion startRot = player.rotation;

        Vector3 liftedPos = startPos + Vector3.up * liftHeight;
        Quaternion liftedRot = startRot; // 举起来这段不改变朝向

        float t = 0f;
        while (t < liftDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / liftDuration);

            player.position = Vector3.Lerp(startPos, liftedPos, lerp);
            player.rotation = Quaternion.Slerp(startRot, liftedRot, lerp);

            yield return null;
        }

        player.position = liftedPos;
        player.rotation = liftedRot;

        // ====== 2. 在空中移动到棺材上方 + 旋转成躺姿 ======
        // 棺材里最终躺下的位置 & 躺姿方向
        Vector3 finalPos = coffinInsidePoint.position;
        Quaternion finalRot = coffinInsidePoint.rotation * Quaternion.Euler(rotationOffsetEuler);

        // 棺材上方的一个“悬停点”
        float hoverY = Mathf.Max(liftedPos.y, finalPos.y) + hoverExtraHeight;
        Vector3 hoverPos = new Vector3(finalPos.x, hoverY, finalPos.z);

        t = 0f;
        while (t < moveToHoverDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / moveToHoverDuration);

            // 从举起来的位置移动到棺材上方
            player.position = Vector3.Lerp(liftedPos, hoverPos, lerp);
            // 从站姿旋成躺姿
            player.rotation = Quaternion.Slerp(liftedRot, finalRot, lerp);

            yield return null;
        }

        player.position = hoverPos;
        player.rotation = finalRot;

        // ====== 3. 垂直放下到棺材里 ======
        Vector3 dropStartPos = hoverPos;
        Vector3 dropEndPos = finalPos;

        t = 0f;
        while (t < dropDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / dropDuration);

            player.position = Vector3.Lerp(dropStartPos, dropEndPos, lerp);
            player.rotation = finalRot; // 这里保持躺姿不变

            yield return null;
        }

        player.position = dropEndPos;
        player.rotation = finalRot;

        // ====== 4. 棺材盖关上动画 ======
        if (coffinAnimator != null && !string.IsNullOrEmpty(coffinCloseTriggerName))
        {
            coffinAnimator.SetTrigger(coffinCloseTriggerName);
        }

        // 给棺材盖一点时间关上（按你动画长度来改）
        yield return new WaitForSeconds(1.0f);

        // ====== 5. 黑屏渐变 ======
        if (fadeCanvasGroup != null)
        {
            float f = 0f;
            while (f < fadeDuration)
            {
                f += Time.deltaTime;
                float lerp = Mathf.Clamp01(f / fadeDuration);
                fadeCanvasGroup.alpha = lerp;
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        // 黑屏之后，再保险一次把控制彻底关死
        if (playerController != null)
            playerController.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ====== 6. 黑屏后一段时间再出结束文字 ======
        yield return new WaitForSeconds(endingTextDelay);

        if (endingText != null)
        {
            endingText.gameObject.SetActive(true);
            Color c = endingText.color;
            c.a = 0f;
            endingText.color = c;

            float tt = 0f;
            float textFadeTime = 1f;
            while (tt < textFadeTime)
            {
                tt += Time.deltaTime;
                float lerp = Mathf.Clamp01(tt / textFadeTime);
                c.a = lerp;
                endingText.color = c;
                yield return null;
            }
        }

        // 如果你需要切到结算场景，可以在这里：
        // if (!string.IsNullOrEmpty(endSceneName))
        // {
        //     UnityEngine.SceneManagement.SceneManager.LoadScene(endSceneName);
        // }
    }
}
