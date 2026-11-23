public interface IChargeableWeapon
{
    WeaponType WeaponType { get; }
    float ChargeDuration { get; }
    bool CanStartCharge(WeaponSlot slot);
    void OnChargeStarted(WeaponSlot slot);
    void OnChargeCanceled(WeaponSlot slot);
    void OnChargeCompleted(WeaponSlot slot);
    bool CanFireChargedAttack(WeaponSlot slot);
    bool TryFireChargedAttack(WeaponSlot slot);
}
