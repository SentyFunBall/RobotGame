using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    private readonly List<ShopItem> weapons = new();

    public IReadOnlyList<ShopItem> Weapons => weapons;
    public ShopItem EquippedWeapon { get; private set; }

    public bool HasWeapon(ShopItem weapon)
    {
        return weapons.Contains(weapon);
    }

    public bool AddWeapon(ShopItem weapon)
    {
        if (weapon == null || weapon.type != ItemType.Weapon)
        {
            Debug.LogError("Cannot add an invalid weapon.", this);
            return false;
        }
        if (HasWeapon(weapon))
            return false;

        weapons.Add(weapon);
        EquippedWeapon = weapon;
        return true;
    }

    public bool EquipWeapon(ShopItem weapon)
    {
        if (!HasWeapon(weapon))
        {
            Debug.LogError("Cannot equip a weapon that is not in the inventory.", this);
            return false;
        }
        EquippedWeapon = weapon;
        return true;
    }
}
