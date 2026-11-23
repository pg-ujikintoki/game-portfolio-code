using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine;

public class PauseManager : MonoBehaviour

{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] GameObject pauseMenuCanvas;
    [SerializeField] GameObject helpWindowCanvas;
    [SerializeField] HelpWindowController helpWindowController;
    [Tooltip("HelpTrigger 内でヘルプを開けるあいだ表示する画面上のマーク。")]
    [SerializeField] HelpAvailableMark helpAvailableMark;

    enum PauseContext

    {
        None,
        PauseMenu,
        Help,
    }

    // InputActions を自前で持ってるならこれ
    private PlayerControls controls;
    private PauseContext _currentContext = PauseContext.None;

    // 解除用に保持しておく
    private System.Action<InputAction.CallbackContext> _cancelHandler;

    HelpSequence _activeHelpSequence;
    HelpTrigger _activeHelpTrigger;
    bool pauseInputLocked;

    void Awake()

    {
        // なければ拾う
        if (!playerInput) playerInput = FindFirstObjectByType<PlayerInput>();
        if (!helpAvailableMark)
        {
            helpAvailableMark = FindFirstObjectByType<HelpAvailableMark>(FindObjectsInactive.Include);
        }

        controls = new PlayerControls();

        if (helpWindowCanvas)
        {
            helpWindowCanvas.SetActive(false);
        }

        helpWindowController?.Close();
    }

    void OnEnable()
    {
        // 自前の Pause
        controls.System.Enable();
        controls.System.Pause.performed += OnPausePerformed;
        controls.System.Help.performed += OnHelpPerformed;

        // UI の Cancel（Escで閉じる）
        if (playerInput && playerInput.actions != null)
        {
            var actions = playerInput.actions;
            var cancel = actions["Cancel"];
            if (cancel != null)
            {
                _cancelHandler = ctx =>
                {
                    if (_currentContext != PauseContext.None)
                    {
                        SetContext(PauseContext.None);
                    }
                };
                cancel.performed += _cancelHandler;
            }
        }

        RefreshHelpAvailableMark();
    }

    void OnDisable()
    {
        // ① 自前の controls があればちゃんと外す
        if (controls != null)
        {
            controls.System.Pause.performed -= OnPausePerformed;
            controls.System.Help.performed -= OnHelpPerformed;
            controls.System.Disable();
        }

        // ② PlayerInput 側の Cancel も存在を見てから外す
        if (playerInput && playerInput.actions != null && _cancelHandler != null)
        {
            var cancel = playerInput.actions["Cancel"];
            if (cancel != null)
                cancel.performed -= _cancelHandler;
        }

        _cancelHandler = null;

        if (_currentContext != PauseContext.None)
        {
            SetContext(PauseContext.None);
        }
    }

    public void SetPauseInputLocked(bool locked)
    {
        pauseInputLocked = locked;
        RefreshHelpAvailableMark();
    }

    void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (pauseInputLocked)
        {
            return;
        }

        TogglePause();
    }

    void OnHelpPerformed(InputAction.CallbackContext ctx)
    {
        if (pauseInputLocked)
        {
            return;
        }

        if (IsKeyboardB(ctx) && _currentContext == PauseContext.Help)
        {
            SetContext(PauseContext.None);
            return;
        }

        ToggleHelp();
    }

    static bool IsKeyboardB(InputAction.CallbackContext context)
    {
        return context.control?.device is Keyboard && context.control.name == "b";
    }

    public void TogglePause()
    {
        if (!pauseMenuCanvas)
        {
            Debug.LogError("[Pause] pauseMenuCanvas が未割り当てです。処理を中止します。");
            return;
        }

        if (_currentContext == PauseContext.Help)
        {
            return;
        }

        var next = _currentContext == PauseContext.PauseMenu ? PauseContext.None : PauseContext.PauseMenu;
        SetContext(next);
    }

    public void ToggleHelp()
    {
        if (!helpWindowCanvas)
        {
            Debug.LogError("[Pause] helpWindowCanvas が未割り当てです。処理を中止します。");
            return;
        }

        if (_currentContext == PauseContext.PauseMenu)
        {
            return;
        }

        if (_currentContext == PauseContext.Help)
        {
            if (!AdvanceHelpEntry())
            {
                SetContext(PauseContext.None);
            }
            return;
        }

        if (_activeHelpSequence == null)
        {
            Debug.Log("[Pause] 現在利用可能なヒントは登録されていません。");
            return;
        }

        SetContext(PauseContext.Help);
    }

    public void SetActiveHelp(HelpSequence sequence, HelpTrigger source, bool autoShow)
    {
        if (!sequence)
        {
            if (source != null && source == _activeHelpTrigger)
            {
                ClearActiveHelp(source);
            }
            return;
        }

        if (!sequence.IsValid)
        {
            Debug.LogWarning($"[Pause] HelpSequence '{sequence.name}' に表示可能なエントリがありません。登録をスキップします。");
            return;
        }

        if (sequence.IsAlreadyShown)
        {
            return;
        }

        _activeHelpSequence = sequence;
        _activeHelpTrigger = source;

        if (_currentContext == PauseContext.Help)
        {
            helpWindowController?.Show(_activeHelpSequence);
            RefreshHelpAvailableMark();
            return;
        }

        if (autoShow && _currentContext != PauseContext.PauseMenu)
        {
            SetContext(PauseContext.Help);
        }

        RefreshHelpAvailableMark();
    }

    public void ClearActiveHelp(HelpTrigger source)
    {
        if (source != null && source != _activeHelpTrigger)
        {
            return;
        }

        _activeHelpTrigger = null;
        _activeHelpSequence = null;

        if (_currentContext == PauseContext.Help)
        {
            SetContext(PauseContext.None);
        }

        RefreshHelpAvailableMark();
    }

    bool AdvanceHelpEntry()
    {

        if (!helpWindowController)
        {
            Debug.LogWarning("[Pause] helpWindowController が未割り当てです。ヘルプを進められません。");
            return false;
        }

        if (!helpWindowController.IsShowing)
        {
            return false;
        }

        return helpWindowController.ShowNext();
    }

    bool CanEnterHelpContext()
    {
        if (_activeHelpSequence == null)
        {
            Debug.Log("[Pause] 開始可能なヒントが登録されていません。");
            return false;
        }

        if (!helpWindowCanvas)
        {
            Debug.LogError("[Pause] helpWindowCanvas が未割り当てです。ヘルプを開けません。");
            return false;
        }

        if (!helpWindowController)
        {
            Debug.LogError("[Pause] helpWindowController が未割り当てです。ヘルプを開けません。");
            return false;
        }

        if (!_activeHelpSequence.IsValid)
        {
            Debug.LogWarning($"[Pause] HelpSequence '{_activeHelpSequence.name}' に表示可能なエントリがありません。");
            return false;
        }

        if (_activeHelpSequence.IsAlreadyShown)
        {
            return false;
        }

        return true;
    }

    static bool AreContextsExclusive(PauseContext current, PauseContext next)
    {
        return (current == PauseContext.PauseMenu && next == PauseContext.Help)
            || (current == PauseContext.Help && next == PauseContext.PauseMenu);
    }

    void SetContext(PauseContext next)
    {

        if (pauseInputLocked && next != PauseContext.None)
        {
            return;
        }

        var previous = _currentContext;
        if (previous == next) return;

        if (AreContextsExclusive(previous, next))
        {
            return;
        }

        if (next == PauseContext.Help && !CanEnterHelpContext())
        {
            return;
        }

        var es = EventSystem.current ?? FindFirstObjectByType<EventSystem>();

        switch (previous)

        {
            case PauseContext.PauseMenu:
                if (pauseMenuCanvas) pauseMenuCanvas.SetActive(false);
                break;
            case PauseContext.Help:
                if (helpWindowController) helpWindowController.Close();
                if (helpWindowCanvas) helpWindowCanvas.SetActive(false);
                ReleaseShownOnceHelp();
                break;

        }

        _currentContext = next;

        bool paused = next != PauseContext.None;
        PauseState.Set(paused);

        if (paused)
        {
            Time.timeScale = 0f;
            if (playerInput) playerInput.SwitchCurrentActionMap("UI");

            if (next == PauseContext.PauseMenu)
            {
                pauseMenuCanvas?.SetActive(true);
                if (isActiveAndEnabled)
                    StartCoroutine(FocusNextFrame());
            }
            else if (next == PauseContext.Help)
            {
                helpWindowCanvas?.SetActive(true);
                helpWindowController?.Show(_activeHelpSequence);
            }

        }
        else
        {
            Time.timeScale = 1f;
            if (playerInput) playerInput.SwitchCurrentActionMap("Player");
            if (es) es.SetSelectedGameObject(null);

            if (pauseMenuCanvas) pauseMenuCanvas.SetActive(false);
            if (helpWindowCanvas) helpWindowCanvas.SetActive(false);

            // 解除時に Bit の削除予約を実行
            var bitMgr = FindFirstObjectByType<BitManager>(); // 参照持ってるなら SerializeField で直接
            if (bitMgr) bitMgr.ApplyRemovalIfNeeded();
        }

        RefreshHelpAvailableMark();

        // デバッグ確認用（一時的に）
        Debug.Log($"[PauseManager] Context = {_currentContext}, IsPaused = {PauseState.IsPaused}");
    }

    void RefreshHelpAvailableMark()
    {
        if (!helpAvailableMark)
        {
            return;
        }

        bool visible = isActiveAndEnabled
            && _activeHelpSequence != null
            && !_activeHelpSequence.IsAlreadyShown
            && !pauseInputLocked
            && _currentContext == PauseContext.None;
        helpAvailableMark.SetVisible(visible);
    }

    void ReleaseShownOnceHelp()
    {
        if (_activeHelpSequence != null && _activeHelpSequence.ShowOnlyOnce)
        {
            _activeHelpSequence = null;
        }
    }

    IEnumerator FocusNextFrame()
    {
        yield return null; // 1フレーム待つ
    }
}