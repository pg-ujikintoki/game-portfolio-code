using UnityEngine;

public partial class PlayerController
{
    /// <summary>落下ミス救済を開始し、急上昇フェーズへ入る。</summary>
    public void StartFallRescue()
    {
        if (isFallRescueActive) return;

        isFallRescueActive = true;
        isFallRescueFloatingPhase = false;
        fallRescueTimer = 0f;

        isDashing = false;
        isJumping = false;
        wallMotor?.ClearKicking();
        velocity = Vector2.zero;
        velocity.y = fallRescueRiseSpeed;

        isAnimatorWalking = false;
        isAnimatorDashing = false;
        SetJumpAnimator(true, false);
    }

    /// <summary>急上昇のあと、浮遊しながら左右移動できる救済中の挙動。</summary>
    void HandleFallRescue(float moveX)
    {
        fallRescueTimer += Time.deltaTime;

        if (!isFallRescueFloatingPhase)
        {
            velocity.x = 0f;
            velocity.y = fallRescueRiseSpeed;

            isAnimatorWalking = false;
            isAnimatorDashing = false;
            SetJumpAnimator(true, false);

            if (fallRescueTimer >= fallRescueRiseDuration || isGrounded)
            {
                isFallRescueFloatingPhase = true;
            }

            return;
        }

        velocity.x = moveX * walkSpeed * fallRescueHorizontalSpeedMul;

        if (JumpPressed || JumpHeld)
        {
            velocity.y = fallRescueJumpPower;
            SetJumpAnimator(true, false);
        }
        else
        {
            velocity.y = -fallRescueFloatFallSpeed;
            SetJumpAnimator(false, !isGrounded);
        }

        if (fallRescueTimer >= fallRescueTotalDuration ||
            (isGrounded && fallRescueTimer > 0.2f))
        {
            EndFallRescue();
        }
    }

    /// <summary>落下救済を終了し、ジャンプ／ダッシュ状態を戻す。</summary>
    void EndFallRescue()
    {
        isFallRescueActive = false;
        isFallRescueFloatingPhase = false;
        fallRescueTimer = 0f;

        isJumping = false;
        wallMotor?.ClearKicking();
        isDashing = false;

        SetJumpAnimator(false, false);
        isAnimatorDashing = false;
    }
}
