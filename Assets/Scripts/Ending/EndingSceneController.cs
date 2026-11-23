using System.Collections;
using UnityEngine;

/// <summary>
/// エンディング開始時に BGM を再生し、一定時間後にフェードアウトしてタイトルへ戻す。
/// </summary>
public class EndingSceneController : MonoBehaviour
{
    private const string TitleSceneName = "TitleScene";

    [Header("Audio")]
    [SerializeField] private AudioClip endingBgm;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.45f;
    [SerializeField, Min(0f)] private float bgmFadeStartTime = 2.5f;
    [SerializeField, Min(0f)] private float bgmFadeDuration = 2.5f;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float titleTransitionDelay = 5f;

    private AudioSource bgmSource;
    private bool transitionStarted;

    void Start()
    {
        Time.timeScale = 1f;
        PlayEndingBgm();
        StartCoroutine(FadeOutBgm());
        StartCoroutine(TransitionToTitle());
    }

    private void PlayEndingBgm()
    {
        if (endingBgm == null)
        {
            return;
        }

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.clip = endingBgm;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    /// <summary>指定秒数までは音量を保ち、その後タイトル遷移までに無音へ下げる。</summary>
    private IEnumerator FadeOutBgm()
    {
        if (bgmSource == null)
        {
            yield break;
        }

        if (bgmFadeStartTime > 0f)
        {
            yield return new WaitForSecondsRealtime(bgmFadeStartTime);
        }

        float duration = bgmFadeDuration;
        float startVolume = bgmSource.volume;
        if (duration <= 0f)
        {
            bgmSource.volume = 0f;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            yield return null;
        }

        bgmSource.volume = 0f;
    }

    private IEnumerator TransitionToTitle()
    {
        if (transitionStarted)
        {
            yield break;
        }

        transitionStarted = true;
        if (titleTransitionDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(titleTransitionDelay);
        }

        SceneLoader.Load(TitleSceneName);
    }
}
