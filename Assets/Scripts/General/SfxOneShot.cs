using UnityEngine;

/// <summary>
/// 2D のワンショット SE を、再生元の寿命と切り離して鳴らす。
/// クリップの割り当ては各キャラクターが持つ。
/// </summary>
public static class SfxOneShot
{
    public static void Play(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        GameObject host = new GameObject("SfxOneShot");
        host.hideFlags = HideFlags.HideInHierarchy;

        AudioSource source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.PlayOneShot(clip, volume);

        Object.Destroy(host, Mathf.Max(clip.length, 0.1f));
    }
}
