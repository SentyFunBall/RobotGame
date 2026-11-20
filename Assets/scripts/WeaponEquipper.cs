using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponEquipper : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textTemplate;

    private readonly Button[] buttons = new Button[3];
    private readonly Image[] icons = new Image[3];
    private readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[3];
    private WeaponManager weapons;
    private TextMeshProUGUI pageText;
    private Button previousButton;
    private Button nextButton;
    private int page;

    public bool Open(GameObject player)
    {
        if (player == null || textTemplate == null)
        {
            Debug.LogError("Weapon selection requires a player and text template.", this);
            return false;
        }

        WeaponManager manager = player.GetComponent<WeaponManager>();
        if (manager == null)
        {
            Debug.LogError("Weapon selection requires a player weapon manager.", this);
            return false;
        }

        weapons = manager;
        if (pageText == null)
            CreateUI();
        page = 0;
        RefreshUI();
        return true;
    }

    private void CreateUI()
    {
        pageText = ShopItemUI.CreateText(
            textTemplate, "Weapon page", transform, new Vector2(0f, 245f), new Vector2(300f, 45f)
        );
        pageText.fontSize = 30f;
        for (int i = 0; i < buttons.Length; i++)
        {
            ShopItemUI.CreateCard(
                $"Weapon slot {i + 1}",
                transform,
                textTemplate,
                new Vector2((i - 1) * 270f, -20f),
                out buttons[i],
                out icons[i],
                out labels[i]
            );
            int slot = i;
            buttons[i].onClick.AddListener(() => Equip(slot));
        }

        previousButton = CreatePageButton("Previous weapons", "<", new Vector2(-210f, 245f), -1);
        nextButton = CreatePageButton("Next weapons", ">", new Vector2(210f, 245f), 1);
    }

    private Button CreatePageButton(string name, string caption, Vector2 position, int direction)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.layer = gameObject.layer;
        RectTransform rect = (RectTransform)obj.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(50f, 45f);
        Image background = obj.GetComponent<Image>();
        background.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() =>
        {
            page += direction;
            RefreshUI();
        });
        TextMeshProUGUI text = ShopItemUI.CreateText(
            textTemplate, "Label", rect, Vector2.zero, rect.sizeDelta
        );
        text.fontSize = 30f;
        text.text = caption;
        return button;
    }

    private void Equip(int slot)
    {
        int index = page * buttons.Length + slot;
        if (index >= weapons.Weapons.Count)
            return;
        if (weapons.EquipWeapon(weapons.Weapons[index]))
            RefreshUI();
    }

    private void RefreshUI()
    {
        int count = weapons.Weapons.Count;
        int pageCount = Mathf.Max(1, (count + buttons.Length - 1) / buttons.Length);
        pageText.text = count == 0 ? "No weapons unlocked" : $"Weapons {page + 1}/{pageCount}";
        previousButton.gameObject.SetActive(pageCount > 1);
        nextButton.gameObject.SetActive(pageCount > 1);
        previousButton.interactable = page > 0;
        nextButton.interactable = page + 1 < pageCount;
        int visibleCount = Mathf.Min(buttons.Length, count - page * buttons.Length);
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = page * buttons.Length + i;
            bool visible = i < visibleCount;
            buttons[i].gameObject.SetActive(visible);
            if (!visible)
                continue;

            ((RectTransform)buttons[i].transform).anchoredPosition =
                new Vector2((i - (visibleCount - 1) / 2f) * 270f, -20f);
            ShopItem item = weapons.Weapons[index];
            icons[i].sprite = item.icon;
            icons[i].enabled = item.icon != null;
            labels[i].text =
                $"{item.itemName}\n{item.description}\n{(item == weapons.EquippedWeapon ? "Equipped" : "Equip")}";
            buttons[i].interactable = item != weapons.EquippedWeapon;
        }
    }
}
