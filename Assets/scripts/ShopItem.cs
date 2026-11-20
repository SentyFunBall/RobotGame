using UnityEngine;

public enum ItemType
{
    Weapon,
    PowerUp,
}

public enum WeaponType
{
    Ranged,
    EMP,
    Bazooka,
    Rail,
}

[CreateAssetMenu(fileName = "ShopItem")]
public class ShopItem : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite icon;
    public int price;

    public ItemType type;

    [Header("Weapon modifiers")]
    public float weaponDamage;
    public float weaponRange;
    public float weaponCooldown;
    public WeaponType weaponType;
    public GameObject bulletPrefab;

    [Header("Power-up")]
    public PowerUpType powerUpType;
}
