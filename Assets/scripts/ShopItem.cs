using UnityEngine;

public enum ItemType
{
    Weapon,
    PowerUp,
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
    public float weaponDamageBonus;
    public float weaponRangeBonus;

    [Header("Power-up")]
    public PowerUpType powerUpType;
}
