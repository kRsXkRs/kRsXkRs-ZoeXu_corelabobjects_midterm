using UnityEngine;

public class ShowTextWhenPlayerInside : MonoBehaviour
{
    public string playerTag = "Player";   // 玩家物体的 Tag
    public GameObject textObject;         // 要显示的那段文字（UI）

    void Start()
    {
        // 一开始先关掉文字
        if (textObject != null)
            textObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (textObject != null)
                textObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (textObject != null)
                textObject.SetActive(false);
        }
    }
}
