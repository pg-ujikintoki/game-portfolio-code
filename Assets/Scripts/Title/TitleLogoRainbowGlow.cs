using UnityEngine;
using TMPro;

/// <summary>
/// TitleLogo の文字を、赤・橙・黄・緑・青・藍・紫の順に発光させる。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TitleLogoRainbowGlow : MonoBehaviour
{
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowOffsetId = Shader.PropertyToID("_GlowOffset");
    private static readonly int GlowInnerId = Shader.PropertyToID("_GlowInner");
    private static readonly int GlowOuterId = Shader.PropertyToID("_GlowOuter");
    private static readonly int GlowPowerId = Shader.PropertyToID("_GlowPower");

    private static readonly Color[] Spectrum =
    {
        new Color(1f, 0.05f, 0.05f),
        new Color(1f, 0.45f, 0.02f),
        new Color(1f, 0.92f, 0.08f),
        new Color(0.1f, 1f, 0.2f),
        new Color(0.05f, 0.4f, 1f),
        new Color(0.18f, 0.08f, 0.85f),
        new Color(0.7f, 0.12f, 1f),
    };

    [SerializeField, Min(0.1f)] private float cycleDuration = 7f;
    [SerializeField, Range(0f, 1f)] private float glowInner = 0.1f;
    [SerializeField, Range(0f, 1f)] private float glowOuter = 0.5f;
    [SerializeField, Range(0.01f, 1f)] private float glowPower = 0.6f;
    [SerializeField, Range(0f, 1f)] private float glowAlpha = 1f;

    private const float ColorHoldPortion = 0.55f;

    private TMP_Text titleText;
    private Material glowMaterial;

    private void Awake()
    {
        titleText = GetComponent<TMP_Text>();
        SetupGlowMaterial();
    }

    private void LateUpdate()
    {
        if (titleText == null || glowMaterial == null)
        {
            return;
        }

        float cycle = Time.unscaledTime / cycleDuration;
        Color color = EvaluateSpectrum(cycle);
        titleText.color = color;

        float pulse = 0.82f + 0.18f * Mathf.Sin(Time.unscaledTime * 2.4f);
        Color glow = color;
        glow.a = glowAlpha * pulse;
        glowMaterial.SetColor(GlowColorId, glow);
    }

    /// <summary>発光つきの SDF マテリアルを用意し、光が文字のメッシュで切れない余白を取る。</summary>
    private void SetupGlowMaterial()
    {
        Material source = titleText.fontSharedMaterial;
        if (source != null && !source.HasProperty(GlowColorId))
        {
            Shader glowShader = Shader.Find("TextMeshPro/Distance Field Overlay");
            if (glowShader != null)
            {
                Material replacement = new Material(source);
                replacement.shader = glowShader;
                titleText.fontSharedMaterial = replacement;
            }
        }

        glowMaterial = titleText.fontMaterial;
        if (glowMaterial == null || !glowMaterial.HasProperty(GlowColorId))
        {
            glowMaterial = null;
            return;
        }

        glowMaterial.EnableKeyword("GLOW_ON");
        glowMaterial.SetFloat(GlowOffsetId, 0f);
        glowMaterial.SetFloat(GlowInnerId, glowInner);
        glowMaterial.SetFloat(GlowOuterId, glowOuter);
        glowMaterial.SetFloat(GlowPowerId, glowPower);
        titleText.extraPadding = true;
        titleText.UpdateMeshPadding();
    }

    /// <summary>七色を順に保ち、次の色へ滑らかにつなぐ。</summary>
    private static Color EvaluateSpectrum(float cycle)
    {
        float scaled = Mathf.Repeat(cycle, 1f) * Spectrum.Length;
        int index = Mathf.FloorToInt(scaled) % Spectrum.Length;
        float fraction = scaled - Mathf.Floor(scaled);
        if (fraction <= ColorHoldPortion)
        {
            return Spectrum[index];
        }

        float blend = (fraction - ColorHoldPortion) / (1f - ColorHoldPortion);
        blend = blend * blend * (3f - 2f * blend);
        int next = (index + 1) % Spectrum.Length;
        return Color.Lerp(Spectrum[index], Spectrum[next], blend);
    }
}
