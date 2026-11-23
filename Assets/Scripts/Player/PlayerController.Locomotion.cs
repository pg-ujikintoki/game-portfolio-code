using UnityEngine;

public partial class PlayerController
{
    /// <summary>歩行・ダッシュ・ジャンプを処理し、Animator へ反映する。</summary>
    private void HandleLocomotion(float moveX)
    {
        UpdateDashAndWalk(moveX);
        UpdateAirDashTurn(moveX);
        UpdateJump();
        UpdateCoyoteTimer();
        ApplyAnimatorParameters();
    }

    /// <summary>接地判定を更新し、着地／離脱時のフラグを整える。</summary>
    private void CheckGrounded()
    {
        if (isJumping || groundedIgnoreTimer > 0f)
        {
            isGrounded = false;
            return;
        }

        bool wasGrounded = isGrounded;
        isGrounded = ProbeGround();

        if (!isGrounded)
        {
            if (wasGrounded)
            {
                isJumping = false;
                SetJumpAnimator(false, true);
                isAnimatorDashing = false;
            }
        }
        else
        {
            SetJumpAnimator(false, false);
            wallMotor?.ClearKicking();
        }
    }

    /// <summary>下向き Cast のうち、上面（法線が上向き）のヒットがあれば接地。壁の側面だけなら接地しない。</summary>
    private bool ProbeGround()
    {
        if (groundCheck == null)
        {
            return false;
        }

        Vector2 origin = (Vector2)groundCheck.position + Vector2.up * 0.06f;
        Vector2 size = new Vector2(groundCheckSize.x, 0.02f);
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = groundLayer
        };

        int count = Physics2D.BoxCast(origin, size, 0f, Vector2.down, filter, groundHits, 0.14f);
        for (int i = 0; i < count; i++)
        {
            if (groundHits[i].collider != null && groundHits[i].normal.y >= 0.5f)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>入力方向に合わせて左右の向きを反転する。</summary>
    private void UpdateFacing(float moveX)
    {
        if (moveX != 0)
        {
            int newFacing = (int)Mathf.Sign(moveX);
            isChangedFacingDirection = (facingDirection != newFacing);
            facingDirection = newFacing;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facingDirection;
            transform.localScale = scale;
        }
        else
        {
            isChangedFacingDirection = false;
        }
    }

    /// <summary>地上ダッシュと通常歩行の横速度を更新する。</summary>
    private void UpdateDashAndWalk(float moveX)
    {
        if (IsHorizontalLocked)
        {
            return;
        }

        if (!isDashing && DashPressed && isGrounded)
        {
            isDashing = true;
            PlayDashSfx();
            dashTimer = 0f;
            velocity.x = dashSpeed * facingDirection;
        }

        if (isDashing)
        {
            if (isGrounded)
            {
                isAnimatorDashing = true;
                isAnimatorWalking = false;
            }

            dashTimer += Time.deltaTime;

            if (isGrounded &&
                (
                    dashTimer >= maxDashDuration ||
                    DashReleased ||
                    isChangedFacingDirection)
            )
            {
                isDashing = false;
                if (isGrounded)
                {
                    velocity.x = 0f;
                }
                isAnimatorDashing = false;
                isAnimatorWalking = false;
            }
        }
        else
        {
            velocity.x = moveX * walkSpeed;
        }
    }

    /// <summary>空中ダッシュ中の左右入力で向きと速度を切り替える。</summary>
    private void UpdateAirDashTurn(float moveX)
    {
        if (IsHorizontalLocked || isGrounded || !isDashing)
        {
            return;
        }

        if (moveX != 0)
        {
            facingDirection = (int)Mathf.Sign(moveX);
            velocity.x = dashSpeed * facingDirection;
        }
    }

    /// <summary>壁蹴りジャンプまたは通常ジャンプを処理する。</summary>
    private void UpdateJump()
    {
        if (JumpPressed && wallMotor != null && wallMotor.TryBeginKick(ref velocity, walkSpeed, DashHeld, facingDirection, out int kickFacing))
        {
            facingDirection = kickFacing;
            BeginJump();
            SetJumpAnimator(true, false);
        }
        else if (isGrounded && JumpPressed)
        {
            BeginJump();
        }

        if (!isJumping)
        {
            return;
        }

        // ポーズ中に JumpReleased が消費されても、押し続けていなければ上昇を終わる。
        if (!JumpHeld)
        {
            EndJumpAscent();
            return;
        }

        SetJumpAnimator(true, false);
        jumpTimer += Time.deltaTime;
        if (jumpTimer < maxJumpTime)
        {
            velocity.y = jumpPower;

            if (DashHeld && wallMotor != null && wallMotor.IsKicking)
            {
                velocity.y *= wallMotor.DashKickMultiplier;
            }

            return;
        }

        EndJumpAscent();
    }

    /// <summary>可変ジャンプの上昇を終了し、落下アニメへ切り替える。</summary>
    private void EndJumpAscent()
    {
        isJumping = false;
        SetJumpAnimator(false, true);
    }

    /// <summary>ジャンプ直後の接地無視時間を進める。</summary>
    private void UpdateCoyoteTimer()
    {
        if (groundedIgnoreTimer > 0f)
        {
            groundedIgnoreTimer -= Time.deltaTime;
        }
        else if (groundedIgnoreTimer != 0)
        {
            groundedIgnoreTimer = 0f;
        }
    }

    /// <summary>現在の移動フラグを Animator へ送る。接地中は空中フラグを落とす。</summary>
    private void ApplyAnimatorParameters()
    {
        isAnimatorWallSliding = !isGrounded && IsWallSliding;
        if (isGrounded)
        {
            isAnimatorJumpUping = false;
            isAnimatorJumpDowning = false;
            SetLocomotionAnimatorBools(isAnimatorWalking, false, false, isAnimatorDashing, false);
            return;
        }

        if (isAnimatorWallSliding)
        {
            isAnimatorWalking = false;
            isAnimatorDashing = false;
            isAnimatorJumpUping = false;
            isAnimatorJumpDowning = true;
            SetLocomotionAnimatorBools(false, false, true, false, true);
            return;
        }

        SetLocomotionAnimatorBools(isAnimatorWalking, isAnimatorJumpUping, isAnimatorJumpDowning, isAnimatorDashing, false);
    }

    /// <summary>ダッシュ開始 SE を再生する。</summary>
    private void PlayDashSfx()
    {
        if (dashSfx && Time.time - lastDashSfxTime > dashSfxCooldown)
        {
            sfxSource.pitch = Random.Range(dashSfxPitchRange.x, dashSfxPitchRange.y);
            sfxSource.PlayOneShot(dashSfx, dashSfxVolume);
            lastDashSfxTime = Time.time;
        }
    }
}
