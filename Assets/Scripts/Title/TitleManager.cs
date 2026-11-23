using System.Collections;
using System.Collections.Generic;
using Preparation;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TitleManager : MonoBehaviour
{
    private enum StartGameMode
    {
        NewGame,
        ContinueCurrentSession
    }

    [Header("Start Settings")]
    [SerializeField] private StartGameMode startGameMode = StartGameMode.NewGame;

    [Header("Title Menu UI (Optional)")]
    [SerializeField] private List<TMP_Text> startMenuLabels = new List<TMP_Text>();
    [SerializeField] private Color normalMenuColor = Color.white;
    [SerializeField] private Color selectedMenuColor = Color.cyan;

    [Header("Audio")]
    [SerializeField] private AudioClip titleBgm;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.45f;
    [SerializeField] private AudioClip startGameSfx;
    [SerializeField, Range(0f, 1f)] private float startGameSfxVolume = 0.9f;

    private PlayerControls inputActions;
    private bool transitioning;
    private bool submitPressed;
    private bool firePressed;

    void Awake()
    {
        inputActions = new PlayerControls();
    }

    void Start()
    {
        PlayTitleBgm();
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.FireMain.performed += OnFireMain;
        inputActions.Player.FireSub.performed += OnFireSub;
        inputActions.UI.Submit.performed += OnSubmit;
        transitioning = false;
        submitPressed = false;
        firePressed = false;
        RefreshStartMenuVisual();
    }

    void OnDisable()
    {
        inputActions.Player.FireMain.performed -= OnFireMain;
        inputActions.Player.FireSub.performed -= OnFireSub;
        inputActions.UI.Submit.performed -= OnSubmit;
        inputActions.Disable();
    }

    void Update()
    {
        bool shouldSubmit = submitPressed || firePressed;
        submitPressed = false;
        firePressed = false;
        if (transitioning || !shouldSubmit)
        {
            return;
        }

        StartNewGameFromTitle();
    }

    private void StartNewGameFromTitle()
    {
        var intermissionProgress = EnsureIntermissionProgressManager();
        var runtimeStateManager = EnsurePartRuntimeStateManager();

        if (startGameMode == StartGameMode.NewGame)
        {
            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.ResetProgress();
            }

            intermissionProgress?.InitializeInventory();

            if (runtimeStateManager != null)
            {
                runtimeStateManager.TryImportEquipmentStateSnapshot(null, out _);
            }

            HelpSequence.ClearShownHistory();
        }
        else
        {
            runtimeStateManager?.RefreshOwnedEquipmentStates();
        }

        transitioning = true;
        StartCoroutine(LoadActionStageAfterStartSfx());
    }

    /// <summary>
    /// ニューゲーム開始 SE を鳴らし、再生が終わってから ActionStageScene へ移る。
    /// クリップが未設定のときは待たずに移る。
    /// </summary>
    private IEnumerator LoadActionStageAfterStartSfx()
    {
        if (startGameSfx != null)
        {
            AudioSource sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.clip = startGameSfx;
            sfxSource.volume = startGameSfxVolume;
            sfxSource.Play();

            float endTime = Time.unscaledTime + startGameSfx.length;
            while (Time.unscaledTime < endTime)
            {
                yield return null;
            }
        }

        SceneLoader.Load("ActionStageScene");
    }

    /// <summary>タイトル開始時の BGM をループ再生する。未設定なら鳴らさない。</summary>
    private void PlayTitleBgm()
    {
        if (titleBgm == null)
        {
            return;
        }

        AudioSource bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.clip = titleBgm;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    private void RefreshStartMenuVisual()
    {
        if (startMenuLabels == null)
        {
            return;
        }

        for (int i = 0; i < startMenuLabels.Count; i++)
        {
            var label = startMenuLabels[i];
            if (label == null)
            {
                continue;
            }

            label.color = i == 0 ? selectedMenuColor : normalMenuColor;
        }
    }

    private static IntermissionProgressManager EnsureIntermissionProgressManager()
    {
        if (IntermissionProgressManager.Instance != null)
        {
            return IntermissionProgressManager.Instance;
        }

        var existing = FindFirstObjectByType<IntermissionProgressManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        var managerObject = new GameObject("IntermissionProgressManager");
        return managerObject.AddComponent<IntermissionProgressManager>();
    }

    private static PartRuntimeStateManager EnsurePartRuntimeStateManager()
    {
        if (PartRuntimeStateManager.Instance != null)
        {
            return PartRuntimeStateManager.Instance;
        }

        var existing = FindFirstObjectByType<PartRuntimeStateManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        var managerObject = new GameObject("PartRuntimeStateManager");
        return managerObject.AddComponent<PartRuntimeStateManager>();
    }

    private void OnFireMain(InputAction.CallbackContext _)
    {
        firePressed = true;
    }

    /// <summary>
    /// キーボードの X だけを開始にする。同じアクションの RB は開始にしない。
    /// </summary>
    private void OnFireSub(InputAction.CallbackContext context)
    {
        if (context.control?.device is Keyboard && context.control.name == "x")
        {
            firePressed = true;
        }
    }

    private void OnSubmit(InputAction.CallbackContext _)
    {
        submitPressed = true;
    }
}
