namespace ProjectRac.Weapons
{
    public interface IWeapon
    {
        string WeaponName { get; }
        int SlotIndex { get; }
        bool IsEquipped { get; }
        void Equip();
        void Unequip();
    }
}
