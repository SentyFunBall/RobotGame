using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    [SerializeField]
    private List<ShopItem> possibleItems = new();

    [SerializeField]
    private TextMeshProUGUI textTemplate;

    private readonly ShopItem[] offers = new ShopItem[3];
    private readonly bool[] sold = new bool[3];
    private readonly Button[] buttons = new Button[3];
    private readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[3];
    private readonly Image[] icons = new Image[3];
    private TextMeshProUGUI balanceText;
    private WeaponManager weapons;
    private PowerUpManager powerUps;

    public void Open(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("Cannot open the shop without a player.", this);
            return;
        }

        weapons = player.GetComponent<WeaponManager>();
        powerUps = player.GetComponent<PowerUpManager>();
        if (weapons == null || powerUps == null || textTemplate == null)
        {
            Debug.LogError(
                "Shop requires a text template and player weapon and power-up managers.",
                this
            );
            return;
        }

        if (balanceText == null)
            CreateUI();
        GenerateShop();
    }

    public void GenerateShop()
    {
        if (weapons == null || powerUps == null || balanceText == null)
        {
            Debug.LogError("Open the shop with a player before generating offers.", this);
            return;
        }

        var available = new List<ShopItem>();
        foreach (ShopItem item in possibleItems)
        {
            if (item == null)
                continue;
            if (!IsValid(item))
            {
                Debug.LogError($"Invalid shop item: {item.name}.", item);
                continue;
            }
            bool duplicatePowerUp =
                item.type == ItemType.PowerUp
                && available.Exists(other =>
                    other.type == ItemType.PowerUp && other.powerUpType == item.powerUpType
                );
            if (!available.Contains(item) && !duplicatePowerUp && !IsOwned(item))
                available.Add(item);
        }

        for (int i = 0; i < offers.Length; i++)
        {
            sold[i] = false;
            offers[i] = null;
            if (available.Count == 0)
                continue;

            int index = Random.Range(0, available.Count);
            offers[i] = available[index];
            available.RemoveAt(index);
        }
        RefreshUI();
    }

    public bool TryPurchase(int slot)
    {
        if (slot < 0 || slot >= offers.Length)
        {
            Debug.LogError($"Invalid shop slot: {slot}.", this);
            return false;
        }

        ShopItem item = offers[slot];
        if (
            item == null
            || sold[slot]
            || !IsValid(item)
            || IsOwned(item)
            || PickupLoot.lootCount
                < item.price * DungeonGenerator.Instance.Difficulty.shopItemCostMult
        )
            return false;

        if (item.type == ItemType.Weapon)
        {
            if (!weapons.AddWeapon(item))
                return false;
        }
        else
        {
            powerUps.ActivatePowerUp(item.powerUpType);
        }

        PickupLoot.lootCount -= item.price * DungeonGenerator.Instance.Difficulty.shopItemCostMult;
        sold[slot] = true;
        RefreshUI();
        return true;
    }

    private static bool IsValid(ShopItem item)
    {
        return item.price >= 0;
    }

    private bool IsOwned(ShopItem item)
    {
        return item.type == ItemType.Weapon
            ? weapons.HasWeapon(item)
            : powerUps.IsPowerUpActive(item.powerUpType);
    }

    private void RefreshUI()
    {
        balanceText.text = $"Loot: {PickupLoot.lootCount}";
        for (int i = 0; i < offers.Length; i++)
        {
            ShopItem item = offers[i];
            icons[i].sprite = item != null ? item.icon : null;
            icons[i].enabled = item != null && item.icon != null;
            if (item == null)
            {
                labels[i].text = "Empty";
                buttons[i].interactable = false;
            }
            else
            {
                string status =
                    sold[i] ? "Sold"
                    : IsOwned(item) ? "Owned"
                    : $"{item.price * DungeonGenerator.Instance.Difficulty.shopItemCostMult} loot";
                labels[i].text = $"{item.itemName}\n{item.description}\n{status}";
                buttons[i].interactable =
                    !sold[i]
                    && !IsOwned(item)
                    && PickupLoot.lootCount
                        >= item.price * DungeonGenerator.Instance.Difficulty.shopItemCostMult;
            }
        }
    }

    private void CreateUI()
    {
        balanceText = ShopItemUI.CreateText(
            textTemplate,
            "Shop balance",
            transform,
            new Vector2(0f, 245f),
            new Vector2(700f, 45f)
        );
        balanceText.fontSize = 30f;
        for (int i = 0; i < buttons.Length; i++)
        {
            ShopItemUI.CreateCard(
                $"Shop slot {i + 1}",
                transform,
                textTemplate,
                new Vector2((i - 1) * 270f, -20f),
                out buttons[i],
                out icons[i],
                out labels[i]
            );
            int slot = i;
            buttons[i].onClick.AddListener(() => TryPurchase(slot));
        }
    }

    public ShopItem GetShopItem(string name)
    {
        foreach (ShopItem item in possibleItems)
        {
            if (item.name == name)
                return item;
        }
        return null;
    }
}

internal static class ShopItemUI
{
    public static void CreateCard(
        string objectName,
        Transform parent,
        TextMeshProUGUI template,
        Vector2 position,
        out Button button,
        out Image icon,
        out TextMeshProUGUI label
    )
    {
        GameObject card = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );
        card.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)card.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(245f, 410f);

        Image background = card.GetComponent<Image>();
        background.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        button = card.GetComponent<Button>();
        button.targetGraphic = background;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.layer = parent.gameObject.layer;
        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, 115f);
        iconRect.sizeDelta = new Vector2(110f, 110f);
        icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        label = CreateText(
            template,
            "Item details",
            rect,
            new Vector2(0f, -60f),
            new Vector2(220f, 220f)
        );
        label.fontSize = 24f;
    }

    public static TextMeshProUGUI CreateText(
        TextMeshProUGUI template,
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size
    )
    {
        TextMeshProUGUI text = Object.Instantiate(template, parent);
        text.gameObject.name = objectName;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        return text;
    }
}
