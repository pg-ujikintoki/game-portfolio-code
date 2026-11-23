public enum WeaponType { Gun, Sword, Bit, Punch }

public static class Loadout
{
    public static WeaponType Main { get; private set; } = WeaponType.Gun;
    public static WeaponType Sub  { get; private set; } = WeaponType.Sword;

    public static event System.Action OnChanged;

    public static void SetMain(WeaponType w) { Main = w; OnChanged?.Invoke(); }
    public static void SetSub (WeaponType w) { Sub  = w; OnChanged?.Invoke(); }
}
