using UnityEngine;

[DisallowMultipleComponent]
public class PlayerChargeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputRouter inputRouter;
    [SerializeField] private PlayerAttackController attackController;
    [SerializeField] private MonoBehaviour abilityProviderBehaviour;
    [SerializeField] private MonoBehaviour weaponProviderBehaviour;

    [Header("Effects")]
    [Tooltip("メインスロットのチャージ演出。装備武器では切り替わらない。")]
    [SerializeField] private ChargeEffectController mainChargeEffect;
    [Tooltip("サブスロットのチャージ演出。装備武器では切り替わらない。")]
    [SerializeField] private ChargeEffectController subChargeEffect;

    private IPlayerAbilityProvider abilityProvider;
    private IChargeWeaponProvider weaponProvider;

    private readonly ChargeSlotState[] slots =
    {
        new ChargeSlotState { Slot = WeaponSlot.Main },
        new ChargeSlotState { Slot = WeaponSlot.Sub }
    };

    private bool isPaused;

    private void Awake()
    {
        if (!IsOwnEffect(mainChargeEffect))
        {
            mainChargeEffect = FindEffect("MainChargeEffect");
        }

        if (!IsOwnEffect(subChargeEffect))
        {
            subChargeEffect = FindEffect("SubChargeEffect");
        }

        slots[0].Effect = mainChargeEffect;
        slots[1].Effect = subChargeEffect;

        slots[0].Effect?.Clear();
        slots[1].Effect?.Clear();
    }

    private bool IsOwnEffect(ChargeEffectController effect)
    {
        return effect && effect.transform.IsChildOf(transform);
    }

    private ChargeEffectController FindEffect(string childName)
    {
        var effects = GetComponentsInChildren<ChargeEffectController>(true);
        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] && effects[i].name == childName)
            {
                return effects[i];
            }
        }

        return null;
    }

    private void OnEnable()
    {
        if (!inputRouter)
        {
            inputRouter = GetComponent<PlayerInputRouter>();
        }

        if (!attackController)
        {
            attackController = GetComponent<PlayerAttackController>();
        }

        abilityProvider = ResolveAbilityProvider();
        weaponProvider = ResolveWeaponProvider();

        if (weaponProvider == null)
        {
            Debug.LogWarning("[PlayerChargeController] チャージ攻撃に対応する IChargeWeaponProvider が見つかりません。チャージショットは使用できません。", this);
        }

        if (inputRouter)
        {
            inputRouter.FireMainStarted += OnMainStarted;
            inputRouter.FireMainCanceled += OnMainCanceled;
            inputRouter.FireSubStarted += OnSubStarted;
            inputRouter.FireSubCanceled += OnSubCanceled;
        }

        Loadout.OnChanged += OnLoadoutChanged;
        PauseState.OnChanged += OnPauseChanged;
    }

    private void OnDisable()
    {
        if (inputRouter)
        {
            inputRouter.FireMainStarted -= OnMainStarted;
            inputRouter.FireMainCanceled -= OnMainCanceled;
            inputRouter.FireSubStarted -= OnSubStarted;
            inputRouter.FireSubCanceled -= OnSubCanceled;
        }

        Loadout.OnChanged -= OnLoadoutChanged;
        PauseState.OnChanged -= OnPauseChanged;

        CancelAllCharges(true);
    }

    private void Update()
    {
        if (isPaused)
        {
            return;
        }

        UpdateSlot(slots[0]);
        UpdateSlot(slots[1]);
    }

    private IPlayerAbilityProvider ResolveAbilityProvider()
    {
        if (abilityProviderBehaviour is IPlayerAbilityProvider configuredProvider)
        {
            return configuredProvider;
        }

        if (abilityProviderBehaviour)
        {
            Debug.LogWarning("[PlayerChargeController] abilityProviderBehaviour は IPlayerAbilityProvider を実装していません。同じ GameObject 上のプロバイダーを検索します。", this);
        }

        if (TryGetComponent<IPlayerAbilityProvider>(out var provider))
        {
            return provider;
        }

        Debug.LogWarning("[PlayerChargeController] IPlayerAbilityProvider が見つかりません。チャージショットはロック状態として扱います。購入状態で解放する場合は PurchasedPartsAbilityProvider を割り当ててください。", this);
        return new LockedAbilityProvider();
    }

    private IChargeWeaponProvider ResolveWeaponProvider()
    {
        if (weaponProviderBehaviour is IChargeWeaponProvider configured)
        {
            return configured;
        }

        if (attackController is IChargeWeaponProvider attackProvider)
        {
            return attackProvider;
        }

        if (weaponProviderBehaviour)
        {
            Debug.LogWarning("[PlayerChargeController] weaponProviderBehaviour は IChargeWeaponProvider を実装していません。", this);
        }

        return null;
    }

    private void UpdateSlot(ChargeSlotState slot)
    {
        if (slot.State == ChargeState.Charged)
        {
            slot.Effect?.ShowStage(ChargeEffectController.Stage2Index);
            return;
        }

        if (slot.State != ChargeState.Charging)
        {
            return;
        }

        slot.Effect?.ShowStage(ChargeEffectController.Stage1Index);

        if (!slot.IsButtonHeld)
        {
            return;
        }

        if (!HasChargeAbility())
        {
            CancelSlot(slot, true);
            return;
        }

        if (!IsWeaponValid(slot))
        {
            CancelSlot(slot, true);
            return;
        }

        slot.Elapsed += Time.deltaTime;
        if (slot.Elapsed >= slot.RequiredDuration)
        {
            slot.State = ChargeState.Charged;
            if (!slot.HasNotifiedChargeComplete)
            {
                slot.HasNotifiedChargeComplete = true;
                slot.Weapon?.OnChargeCompleted(slot.Slot);
                slot.Effect?.ShowStage(ChargeEffectController.Stage2Index);
            }
        }
    }

    private void OnMainStarted()
    {
        StartCharging(slots[0]);
    }

    private void OnMainCanceled()
    {
        HandleReleaseRequest(slots[0]);
    }

    private void OnSubStarted()
    {
        StartCharging(slots[1]);
    }

    private void OnSubCanceled()
    {
        HandleReleaseRequest(slots[1]);
    }

    private void StartCharging(ChargeSlotState slot)
    {
        slot.IsButtonHeld = true;
        slot.PendingReleaseAfterPause = false;

        if (isPaused)
        {
            return;
        }

        if (slot.State != ChargeState.None)
        {
            CancelSlot(slot, true);
            slot.IsButtonHeld = true;
        }

        if (!HasChargeAbility())
        {
            slot.Effect?.Clear();
            return;
        }

        var currentWeaponType = GetLoadoutWeaponType(slot.Slot);
        var weapon = GetChargeableWeapon(slot.Slot);

        if (weapon == null || weapon.WeaponType != currentWeaponType)
        {
            slot.Effect?.Clear();
            return;
        }

        if (!weapon.CanStartCharge(slot.Slot))
        {
            slot.Effect?.Clear();
            return;
        }

        slot.State = ChargeState.Charging;
        slot.Elapsed = 0f;
        slot.RequiredDuration = Mathf.Max(weapon.ChargeDuration, 0.0001f);
        slot.Weapon = weapon;
        slot.WeaponType = currentWeaponType;
        slot.HasNotifiedChargeComplete = false;
        slot.Effect?.ShowStage(ChargeEffectController.Stage1Index);
        weapon.OnChargeStarted(slot.Slot);
    }

    private void HandleReleaseRequest(ChargeSlotState slot)
    {
        slot.IsButtonHeld = false;

        if (isPaused)
        {
            slot.PendingReleaseAfterPause = true;
            return;
        }

        ReleaseOrCancel(slot);
    }

    private void ReleaseOrCancel(ChargeSlotState slot)
    {
        if (slot.State == ChargeState.None)
        {
            slot.Effect?.Clear();
            slot.PendingReleaseAfterPause = false;
            return;
        }

        if (!HasChargeAbility() || !IsWeaponValid(slot))
        {
            CancelSlot(slot, true);
            return;
        }

        if (slot.State != ChargeState.Charged)
        {
            CancelSlot(slot, true);
            return;
        }

        var weapon = slot.Weapon;
        if (weapon == null)
        {
            CancelSlot(slot, false);
            return;
        }

        if (!weapon.CanFireChargedAttack(slot.Slot))
        {
            CancelSlot(slot, true);
            return;
        }

        if (!weapon.TryFireChargedAttack(slot.Slot))
        {
            CancelSlot(slot, true);
            return;
        }

        ResetSlot(slot, true);
    }

    private void CancelSlot(ChargeSlotState slot, bool notifyWeapon)
    {
        if (notifyWeapon && slot.Weapon != null)
        {
            slot.Weapon.OnChargeCanceled(slot.Slot);
        }

        ResetSlot(slot, true);
    }

    private void ResetSlot(ChargeSlotState slot, bool clearEffect)
    {
        slot.State = ChargeState.None;
        slot.Elapsed = 0f;
        slot.RequiredDuration = 0f;
        slot.HasNotifiedChargeComplete = false;
        slot.PendingReleaseAfterPause = false;
        slot.IsButtonHeld = false;
        slot.Weapon = null;
        slot.WeaponType = default;

        if (clearEffect)
        {
            slot.Effect?.Clear();
        }
    }

    public void CancelChargesForStageLock()
    {
        CancelAllCharges(true);
    }

    private void CancelAllCharges(bool notifyWeapon)
    {
        CancelSlot(slots[0], notifyWeapon);
        CancelSlot(slots[1], notifyWeapon);
    }

    private void OnLoadoutChanged()
    {
        ValidateSlot(slots[0]);
        ValidateSlot(slots[1]);
    }

    private void ValidateSlot(ChargeSlotState slot)
    {
        if (slot.State == ChargeState.None)
        {
            slot.Effect?.Clear();
            return;
        }

        if (!IsWeaponValid(slot))
        {
            CancelSlot(slot, true);
        }
    }

    private void OnPauseChanged(bool paused)
    {
        isPaused = paused;

        if (!paused)
        {
            EvaluateResumeState(slots[0]);
            EvaluateResumeState(slots[1]);
        }
    }

    private void EvaluateResumeState(ChargeSlotState slot)
    {
        if (slot.PendingReleaseAfterPause)
        {
            slot.IsButtonHeld = false;
            slot.PendingReleaseAfterPause = false;
            ReleaseOrCancel(slot);
            return;
        }

        if (slot.State == ChargeState.None)
        {
            slot.Effect?.Clear();
            return;
        }

        if (IsInputPressed(slot.Slot))
        {
            slot.IsButtonHeld = true;
            slot.PendingReleaseAfterPause = false;
            return;
        }

        slot.IsButtonHeld = false;
        slot.PendingReleaseAfterPause = false;
        ReleaseOrCancel(slot);
    }

    private bool IsWeaponValid(ChargeSlotState slot)
    {
        if (slot.Weapon == null)
        {
            return false;
        }

        if (slot.WeaponType != GetLoadoutWeaponType(slot.Slot))
        {
            return false;
        }

        var current = GetChargeableWeapon(slot.Slot);
        return current == slot.Weapon;
    }

    private bool IsInputPressed(WeaponSlot slot)
    {
        if (!inputRouter)
        {
            return false;
        }

        return slot == WeaponSlot.Main ? inputRouter.IsFireMainPressed : inputRouter.IsFireSubPressed;
    }

    private bool HasChargeAbility()
    {
        return abilityProvider?.HasAbility(PlayerAbilityType.ChargeShot) ?? false;
    }

    private IChargeableWeapon GetChargeableWeapon(WeaponSlot slot)
    {
        return weaponProvider?.GetChargeableWeapon(slot);
    }

    private static WeaponType GetLoadoutWeaponType(WeaponSlot slot)
    {
        return slot == WeaponSlot.Main ? Loadout.Main : Loadout.Sub;
    }

    private sealed class ChargeSlotState
    {
        public WeaponSlot Slot;
        public ChargeEffectController Effect;
        public ChargeState State = ChargeState.None;
        public float Elapsed;
        public float RequiredDuration;
        public bool IsButtonHeld;
        public bool HasNotifiedChargeComplete;
        public bool PendingReleaseAfterPause;
        public IChargeableWeapon Weapon;
        public WeaponType WeaponType;
    }

    private sealed class LockedAbilityProvider : IPlayerAbilityProvider
    {
        public bool HasAbility(PlayerAbilityType abilityType) => false;
    }
}
