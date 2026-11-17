using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class ShopTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject shopObject;
    private GameObject player;
    private Component shop;
    private Component weapons;
    private Component powerUps;
    private Type shopType;
    private Type itemType;
    private Type itemKind;
    private Type powerUpType;
    private FieldInfo loot;
    private int previousLoot;
    private Random.State previousRandom;
    private readonly List<Object> items = new();

    [SetUp]
    public void SetUp()
    {
        shopType = Type.GetType("Shop, Assembly-CSharp", true);
        itemType = Type.GetType("ShopItem, Assembly-CSharp", true);
        itemKind = Type.GetType("ItemType, Assembly-CSharp", true);
        powerUpType = Type.GetType("PowerUpType, Assembly-CSharp", true);
        loot = Type.GetType("PickupLoot, Assembly-CSharp", true).GetField("lootCount");
        previousLoot = (int)loot.GetValue(null);
        previousRandom = Random.state;
        loot.SetValue(null, 0);

        player = new GameObject("Shop test player");
        player.SetActive(false);
        weapons = player.AddComponent(Type.GetType("WeaponManager, Assembly-CSharp", true));
        powerUps = player.AddComponent(Type.GetType("PowerUpManager, Assembly-CSharp", true));

        shopObject = new GameObject("Shop test", typeof(RectTransform));
        shop = shopObject.AddComponent(shopType);
        GameObject template = new GameObject("Text template", typeof(RectTransform));
        template.transform.SetParent(shopObject.transform, false);
        Component text = template.AddComponent(Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true));
        shopType.GetField("textTemplate", PrivateInstance).SetValue(shop, text);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(shopObject);
        Object.DestroyImmediate(player);
        foreach (Object item in items)
            Object.DestroyImmediate(item);
        items.Clear();
        loot.SetValue(null, previousLoot);
        Random.state = previousRandom;
    }

    private Object AddItem(string kind, int price)
    {
        Object item = ScriptableObject.CreateInstance(itemType);
        itemType.GetField("itemName").SetValue(item, kind);
        itemType.GetField("type").SetValue(item, Enum.Parse(itemKind, kind));
        itemType.GetField("price").SetValue(item, price);
        items.Add(item);
        ((IList)shopType.GetField("possibleItems", PrivateInstance).GetValue(shop)).Add(item);
        return item;
    }

    private void Open()
    {
        shopType.GetMethod("Open").Invoke(shop, new object[] { player });
    }

    private bool Buy(int slot)
    {
        return (bool)shopType.GetMethod("TryPurchase").Invoke(shop, new object[] { slot });
    }

    [Test]
    public void EmptyCatalogDisplaysThreeEmptySlots()
    {
        Open();

        Array offers = (Array)shopType.GetField("offers", PrivateInstance).GetValue(shop);
        Assert.That(offers.Length, Is.EqualTo(3));
        for (int i = 0; i < offers.Length; i++)
        {
            Assert.That(offers.GetValue(i), Is.Null);
            Assert.That(shopObject.transform.Find($"Shop slot {i + 1}"), Is.Not.Null);
        }
    }

    [Test]
    public void PurchasesSpendLootOnceAndGrantAllThreeOffers()
    {
        Object first = AddItem("Weapon", 2);
        Object second = AddItem("Weapon", 3);
        AddItem("PowerUp", 4);
        loot.SetValue(null, 9);
        Open();

        Array offers = (Array)shopType.GetField("offers", PrivateInstance).GetValue(shop);
        Assert.That(new HashSet<object> { offers.GetValue(0), offers.GetValue(1), offers.GetValue(2) }.Count, Is.EqualTo(3));
        Assert.That(Buy(0), Is.True);
        int afterFirst = (int)loot.GetValue(null);
        Assert.That(Buy(0), Is.False);
        Assert.That(loot.GetValue(null), Is.EqualTo(afterFirst));
        Assert.That(Buy(1), Is.True);
        Assert.That(Buy(2), Is.True);
        Assert.That(loot.GetValue(null), Is.EqualTo(0));
        Assert.That(weapons.GetType().GetMethod("HasWeapon").Invoke(weapons, new[] { first }), Is.EqualTo(true));
        Assert.That(weapons.GetType().GetMethod("HasWeapon").Invoke(weapons, new[] { second }), Is.EqualTo(true));
        Assert.That(powerUps.GetType().GetMethod("IsPowerUpActive").Invoke(powerUps,
            new[] { Enum.Parse(powerUpType, "Speed") }), Is.EqualTo(true));
        Assert.That(weapons.GetType().GetMethod("EquipWeapon").Invoke(weapons, new[] { first }), Is.EqualTo(true));
        Assert.That(weapons.GetType().GetProperty("EquippedWeapon").GetValue(weapons), Is.EqualTo(first));

        Open();
        offers = (Array)shopType.GetField("offers", PrivateInstance).GetValue(shop);
        foreach (object offer in offers)
            Assert.That(offer, Is.Null);
    }

    [Test]
    public void UnaffordableItemCannotBePurchasedAndDuplicatePowerUpsAreNotOffered()
    {
        AddItem("PowerUp", 5);
        AddItem("PowerUp", 5);
        loot.SetValue(null, 4);
        Open();

        Array offers = (Array)shopType.GetField("offers", PrivateInstance).GetValue(shop);
        Assert.That(offers.GetValue(0), Is.Not.Null);
        Assert.That(offers.GetValue(1), Is.Null);
        Assert.That(Buy(0), Is.False);
        Assert.That(loot.GetValue(null), Is.EqualTo(4));
        Assert.That(powerUps.GetType().GetMethod("IsPowerUpActive").Invoke(powerUps,
            new[] { Enum.Parse(powerUpType, "Speed") }), Is.EqualTo(false));
    }

    [Test]
    public void OffersAreRandomizedWithoutRepeats()
    {
        for (int i = 0; i < 5; i++)
            AddItem("Weapon", 1);
        Open();

        var firstSlots = new HashSet<object>();
        for (int seed = 0; seed < 10; seed++)
        {
            Random.InitState(seed);
            shopType.GetMethod("GenerateShop").Invoke(shop, null);
            Array offers = (Array)shopType.GetField("offers", PrivateInstance).GetValue(shop);
            var selected = new HashSet<object>();
            foreach (object offer in offers)
                selected.Add(offer);
            Assert.That(selected.Count, Is.EqualTo(3));
            firstSlots.Add(offers.GetValue(0));
        }
        Assert.That(firstSlots.Count, Is.GreaterThan(1));
    }

    [Test]
    public void ClickingAnOfferRefreshesTheOtherButtonsAndBalance()
    {
        Object cheap = AddItem("Weapon", 2);
        AddItem("Weapon", 5);
        loot.SetValue(null, 5);
        Open();

        Array offers = (Array)shopType.GetField("offers", PrivateInstance).GetValue(shop);
        Array buttons = (Array)shopType.GetField("buttons", PrivateInstance).GetValue(shop);
        int cheapSlot = offers.GetValue(0) == cheap ? 0 : 1;
        int expensiveSlot = 1 - cheapSlot;
        object cheapButton = buttons.GetValue(cheapSlot);
        Type buttonType = cheapButton.GetType();
        var click = (UnityEvent)buttonType.GetProperty("onClick").GetValue(cheapButton);
        click.Invoke();

        Assert.That(loot.GetValue(null), Is.EqualTo(3));
        Assert.That(buttonType.GetProperty("interactable").GetValue(cheapButton), Is.EqualTo(false));
        Assert.That(buttonType.GetProperty("interactable").GetValue(buttons.GetValue(expensiveSlot)), Is.EqualTo(false));
        Component balance = (Component)shopType.GetField("balanceText", PrivateInstance).GetValue(shop);
        Assert.That(balance.GetType().GetProperty("text").GetValue(balance), Is.EqualTo("Loot: 3"));
    }

    [Test]
    public void WeaponSelectionShowsInventoryAndEquipsClickedWeapon()
    {
        Object first = AddItem("Weapon", 1);
        Object second = AddItem("Weapon", 1);
        AddItem("PowerUp", 1);
        weapons.GetType().GetMethod("AddWeapon").Invoke(weapons, new[] { first });
        weapons.GetType().GetMethod("AddWeapon").Invoke(weapons, new[] { second });

        GameObject menu = new GameObject("Weapon selection", typeof(RectTransform));
        try
        {
            Type equipperType = Type.GetType("WeaponEquipper, Assembly-CSharp", true);
            Component equipper = menu.AddComponent(equipperType);
            equipperType.GetField("textTemplate", PrivateInstance).SetValue(
                equipper, shopType.GetField("textTemplate", PrivateInstance).GetValue(shop)
            );
            Assert.That(equipperType.GetMethod("Open").Invoke(equipper, new object[] { player }), Is.True);

            Array buttons = (Array)equipperType.GetField("buttons", PrivateInstance).GetValue(equipper);
            Array labels = (Array)equipperType.GetField("labels", PrivateInstance).GetValue(equipper);
            Type buttonType = buttons.GetValue(0).GetType();
            Assert.That(buttonType.GetProperty("interactable").GetValue(buttons.GetValue(2)), Is.False);
            Assert.That(labels.GetValue(1).GetType().GetProperty("text").GetValue(labels.GetValue(1)), Does.Contain("Equipped"));

            ((UnityEvent)buttonType.GetProperty("onClick").GetValue(buttons.GetValue(0))).Invoke();
            Assert.That(weapons.GetType().GetProperty("EquippedWeapon").GetValue(weapons), Is.EqualTo(first));
            Assert.That(labels.GetValue(0).GetType().GetProperty("text").GetValue(labels.GetValue(0)), Does.Contain("Equipped"));
            Assert.That(buttonType.GetProperty("interactable").GetValue(buttons.GetValue(1)), Is.True);

            Assert.That(equipperType.GetMethod("Open").Invoke(equipper, new object[] { player }), Is.True);
            Assert.That(menu.transform.Find("Weapon slot 1"), Is.Not.Null);
            Assert.That(menu.transform.childCount, Is.EqualTo(6));
        }
        finally
        {
            Object.DestroyImmediate(menu);
        }
    }

    [Test]
    public void WeaponSelectionHandlesEmptyInventoryAndAdditionalPages()
    {
        GameObject menu = new GameObject("Weapon selection", typeof(RectTransform));
        try
        {
            Type equipperType = Type.GetType("WeaponEquipper, Assembly-CSharp", true);
            Component equipper = menu.AddComponent(equipperType);
            equipperType.GetField("textTemplate", PrivateInstance).SetValue(
                equipper, shopType.GetField("textTemplate", PrivateInstance).GetValue(shop)
            );
            equipperType.GetMethod("Open").Invoke(equipper, new object[] { player });
            Component pageText = (Component)equipperType.GetField("pageText", PrivateInstance).GetValue(equipper);
            Assert.That(pageText.GetType().GetProperty("text").GetValue(pageText), Is.EqualTo("No weapons unlocked"));

            Object last = null;
            for (int i = 0; i < 4; i++)
            {
                last = AddItem("Weapon", 1);
                weapons.GetType().GetMethod("AddWeapon").Invoke(weapons, new[] { last });
            }
            weapons.GetType().GetMethod("EquipWeapon").Invoke(weapons, new[] { items[0] });
            equipperType.GetMethod("Open").Invoke(equipper, new object[] { player });
            Component next = (Component)equipperType.GetField("nextButton", PrivateInstance).GetValue(equipper);
            ((UnityEvent)next.GetType().GetProperty("onClick").GetValue(next)).Invoke();
            Assert.That(pageText.GetType().GetProperty("text").GetValue(pageText), Is.EqualTo("Weapons 2/2"));
            Array buttons = (Array)equipperType.GetField("buttons", PrivateInstance).GetValue(equipper);
            object firstButton = buttons.GetValue(0);
            ((UnityEvent)firstButton.GetType().GetProperty("onClick").GetValue(firstButton)).Invoke();
            Assert.That(weapons.GetType().GetProperty("EquippedWeapon").GetValue(weapons), Is.EqualTo(last));
        }
        finally
        {
            Object.DestroyImmediate(menu);
        }
    }

    [Test]
    public void SceneShopAndPlayerPrefabAreWired()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Meshes/Player/Player.prefab");
        Assert.That(prefab.GetComponent(weapons.GetType()), Is.Not.Null);

        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SampleScene.unity");
        try
        {
            Component sceneShop = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                sceneShop = root.GetComponentInChildren(shopType, true);
                if (sceneShop != null)
                    break;
            }
            Assert.That(sceneShop, Is.Not.Null);
            Assert.That(shopType.GetField("textTemplate", PrivateInstance).GetValue(sceneShop), Is.Not.Null);
            Transform continueButton = sceneShop.transform.Find("ContinueBtn");
            Type buttonType = Type.GetType("UnityEngine.UI.Button, UnityEngine.UI", true);
            Component button = continueButton.GetComponent(buttonType);
            var onClick = (UnityEventBase)buttonType.GetProperty("onClick").GetValue(button);
            Assert.That(onClick.GetPersistentMethodName(0), Is.EqualTo("ShowWeaponSelectMenu"));
            Object target = onClick.GetPersistentTarget(0);
            Assert.That(target, Is.Not.Null);
            Component menus = (Component)target;
            GameObject selection = (GameObject)menus.GetType().GetField("weaponSelectMenu", PrivateInstance).GetValue(menus);
            Assert.That(selection, Is.Not.Null);
            Type equipperType = Type.GetType("WeaponEquipper, Assembly-CSharp", true);
            Component equipper = selection.GetComponent(equipperType);
            Assert.That(equipper, Is.Not.Null);
            Assert.That(equipperType.GetField("textTemplate", PrivateInstance).GetValue(equipper), Is.Not.Null);
            Component selectionContinue = selection.transform.Find("ContinueBtn").GetComponent(buttonType);
            var selectionClick = (UnityEventBase)buttonType.GetProperty("onClick").GetValue(selectionContinue);
            Assert.That(selectionClick.GetPersistentMethodName(0), Is.EqualTo("AdvanceLevel"));
            Assert.That(selectionClick.GetPersistentTarget(0).GetType().GetMethod("AdvanceLevel").IsStatic, Is.False);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
