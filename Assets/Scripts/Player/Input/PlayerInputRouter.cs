// Assets/Scripts/Player/Input/PlayerInputRouter.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputRouter : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool JumpReleased { get; private set; }
    public bool DashPressed { get; private set; }
    public bool DashHeld { get; private set; }
    public bool DashReleased { get; private set; }

    public bool IsFireMainHeld { get; private set; }
    public bool IsFireSubHeld { get; private set; }

    public event System.Action FireMain; // Z = メイン
    public event System.Action FireSub;  // X = サブ
    public event System.Action FireMainStarted;
    public event System.Action FireMainCanceled;
    public event System.Action FireSubStarted;
    public event System.Action FireSubCanceled;

    public bool IsFireMainPressed => controls?.Player.FireMain.IsPressed() ?? false;
    public bool IsFireSubPressed => controls?.Player.FireSub.IsPressed() ?? false;

    private PlayerControls controls;
    private bool gameplayBlocked;


    void Awake()
    {
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        controls.Player.Enable();

        controls.Player.Move.performed += OnMovePerformed;
        controls.Player.Move.canceled += OnMoveCanceled;

        controls.Player.Jump.started += OnJumpStarted;
        controls.Player.Jump.canceled += OnJumpCanceled;

        controls.Player.Dash.started += OnDashStarted;
        controls.Player.Dash.canceled += OnDashCanceled;

        controls.Player.FireMain.started += OnFireMainStarted;
        controls.Player.FireMain.canceled += OnFireMainCanceled;
        controls.Player.FireMain.performed += OnFireMainPerformed;

        controls.Player.FireSub.started += OnFireSubStarted;
        controls.Player.FireSub.canceled += OnFireSubCanceled;
        controls.Player.FireSub.performed += OnFireSubPerformed;

        PauseState.OnChanged += OnPauseChanged;
        if (PauseState.IsPaused)
        {
            ClearEdgeFlags();
        }
        else
        {
            SyncHeldStateFromActions();
        }
    }


    void LateUpdate()
    {
        if (PauseState.IsPaused)
        {
            return;
        }

        ClearEdgeFlags();
    }

    public void SetGameplayInputBlocked(bool blocked)
    {
        gameplayBlocked = blocked;
        if (blocked)
        {
            ClearInputState();
            IsFireMainHeld = false;
            IsFireSubHeld = false;
            return;
        }

        SyncHeldStateFromActions();
    }

    public void ClearInputState()
    {
        Move = Vector2.zero;
        JumpPressed = false;
        JumpHeld = false;
        JumpReleased = false;
        DashPressed = false;
        DashHeld = false;
        DashReleased = false;
    }

    void OnDisable()
    {
        controls.Player.Move.performed -= OnMovePerformed;
        controls.Player.Move.canceled -= OnMoveCanceled;

        controls.Player.Jump.started -= OnJumpStarted;
        controls.Player.Jump.canceled -= OnJumpCanceled;

        controls.Player.Dash.started -= OnDashStarted;
        controls.Player.Dash.canceled -= OnDashCanceled;

        controls.Player.FireMain.started -= OnFireMainStarted;
        controls.Player.FireMain.canceled -= OnFireMainCanceled;
        controls.Player.FireMain.performed -= OnFireMainPerformed;

        controls.Player.FireSub.started -= OnFireSubStarted;
        controls.Player.FireSub.canceled -= OnFireSubCanceled;
        controls.Player.FireSub.performed -= OnFireSubPerformed;

        PauseState.OnChanged -= OnPauseChanged;

        controls.Player.Disable();

        ClearInputState();
        IsFireMainHeld = false;
        IsFireSubHeld = false;
    }

    private void OnPauseChanged(bool paused)
    {
        if (paused)
        {
            ClearEdgeFlags();
            return;
        }

        SyncHeldStateFromActions();
    }

    private void ClearEdgeFlags()
    {
        JumpPressed = false;
        JumpReleased = false;
        DashPressed = false;
        DashReleased = false;
    }

    /// <summary>
    /// ポーズ中に消費された押下／解放を捨て、解除時の実ボタン状態へ合わせる。
    /// ジャンプ押し続け中なら Held を復元し、離していれば Released を立てて上昇を終了させる。
    /// </summary>
    private void SyncHeldStateFromActions()
    {
        if (controls == null)
        {
            return;
        }

        Vector2 moveNow = controls.Player.Move.ReadValue<Vector2>();
        bool jumpHeldNow = controls.Player.Jump.IsPressed();
        bool dashHeldNow = controls.Player.Dash.IsPressed();

        JumpReleased = JumpHeld && !jumpHeldNow;
        DashReleased = DashHeld && !dashHeldNow;
        JumpPressed = false;
        DashPressed = false;

        Move = moveNow;
        JumpHeld = jumpHeldNow;
        DashHeld = dashHeldNow;
    }


    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused || gameplayBlocked)
        {
            return;
        }

        Move = ctx.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused || gameplayBlocked)
        {
            return;
        }

        Move = Vector2.zero;
    }

    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused || gameplayBlocked)
        {
            return;
        }

        JumpPressed = true;
        JumpHeld = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused || gameplayBlocked)
        {
            return;
        }

        JumpHeld = false;
        JumpReleased = true;
    }

    private void OnDashStarted(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused || gameplayBlocked)
        {
            return;
        }

        DashPressed = true;
        DashHeld = true;
    }

    private void OnDashCanceled(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused || gameplayBlocked)
        {
            return;
        }

        DashHeld = false;
        DashReleased = true;
    }

    private void OnFireMainStarted(InputAction.CallbackContext ctx)

    {
        if (gameplayBlocked)
        {
            return;
        }

        IsFireMainHeld = true;
        if (!PauseState.IsPaused)
        {
            FireMainStarted?.Invoke();
        }
    }

    private void OnFireMainCanceled(InputAction.CallbackContext ctx)
    {
        if (gameplayBlocked)
        {
            return;
        }

        IsFireMainHeld = false;
        FireMainCanceled?.Invoke();
    }

    private void OnFireSubStarted(InputAction.CallbackContext ctx)
    {
        if (gameplayBlocked)
        {
            return;
        }

        IsFireSubHeld = true;
        if (!PauseState.IsPaused)
        {
            FireSubStarted?.Invoke();
        }
    }

    private void OnFireSubCanceled(InputAction.CallbackContext ctx)
    {
        if (gameplayBlocked)
        {
            return;
        }

        IsFireSubHeld = false;
        FireSubCanceled?.Invoke();
    }

    private void OnFireMainPerformed(InputAction.CallbackContext ctx)
    {
        if (!PauseState.IsPaused && !gameplayBlocked)
        {
            FireMain?.Invoke();
        }
    }

    private void OnFireSubPerformed(InputAction.CallbackContext ctx)
    {
        if (!PauseState.IsPaused && !gameplayBlocked)
        {
            FireSub?.Invoke();
        }
    }
}
