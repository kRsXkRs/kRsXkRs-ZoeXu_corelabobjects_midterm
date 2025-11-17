using UnityEngine;

public class ChangeBgmWhenGone : MonoBehaviour
{
    [Header("场景里负责播放BGM的AudioSource")]
    public AudioSource bgmSource;

    [Header("这个东西消失后要切到的BGM")]
    public AudioClip newBgm;

    private bool hasSwitched = false;

    // 物体被关掉（SetActive(false)）时会触发
    private void OnDisable()
    {
        SwitchBgm();
    }

    // 物体被Destroy时会触发
    private void OnDestroy()
    {
        SwitchBgm();
    }

    private void SwitchBgm()
    {
        if (hasSwitched) return;          // 防止OnDisable和OnDestroy双重调用
        hasSwitched = true;

        if (bgmSource == null || newBgm == null) return;

        bgmSource.clip = newBgm;
        bgmSource.Play();
    }
}
