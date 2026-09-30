using System;
using System.Reflection;
using NUnit.Framework;
using GameCore;
using ScriptableObjects.GameEvents;
using ScriptableObjects.Services;
using Shop;
using UnityEngine;
using Object = UnityEngine.Object;

public class ShopTests
{
    private CoinWallet wallet;
    private PlayerPrefsSaveService saveService;
    private string oldState;
    private int oldBalance;
    private bool hadState;
    private bool hadBalance;

    [SetUp]
    public void SetUp()
    {
        hadState = PlayerPrefs.HasKey("ShopStateV1");
        hadBalance = PlayerPrefs.HasKey("CoinBalance");
        oldState = PlayerPrefs.GetString("ShopStateV1", "");
        oldBalance = PlayerPrefs.GetInt("CoinBalance", 0);
        PlayerPrefs.DeleteKey("ShopStateV1");
        PlayerPrefs.SetInt("CoinBalance", 0);
        PlayerPrefs.Save();

        saveService = ScriptableObject.CreateInstance<PlayerPrefsSaveService>();
        wallet = ScriptableObject.CreateInstance<CoinWallet>();
        typeof(CoinWallet).GetField("saveService", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(wallet, saveService);
    }

    [TearDown]
    public void TearDown()
    {
        if (hadState) PlayerPrefs.SetString("ShopStateV1", oldState);
        else PlayerPrefs.DeleteKey("ShopStateV1");
        if (hadBalance) PlayerPrefs.SetInt("CoinBalance", oldBalance);
        else PlayerPrefs.DeleteKey("CoinBalance");
        PlayerPrefs.Save();
        Object.DestroyImmediate(wallet);
        Object.DestroyImmediate(saveService);
    }

    [Test]
    public void CatalogHasRequiredGameplayItemsAndDefaults()
    {
        Assert.IsNotNull(ShopCatalog.FindPowerup(ShopItemKind.Shield));
        Assert.IsNotNull(ShopCatalog.FindPowerup(ShopItemKind.DoubleScore));
        Assert.AreEqual(0, ShopCatalog.Find(ShopItemIds.BallDefault).price);
        Assert.AreEqual(0, ShopCatalog.Find(ShopItemIds.WorldDefault).price);
    }

    [Test]
    public void BuyingThemeUnlocksWithoutEquippingAndCannotBeRepeated()
    {
        ShopItemData ballTheme = FindOptionalTheme(ShopItemKind.BallTheme);
        ShopItemData worldTheme = FindOptionalTheme(ShopItemKind.WorldTheme);
        if (ballTheme == null || worldTheme == null)
            Assert.Ignore("The catalog has no paid ball and ring themes to purchase.");

        wallet.AddCoins(ballTheme.price + worldTheme.price);
        Assert.IsTrue(ShopStateService.TryPurchase(ballTheme.id, wallet, out _));
        Assert.IsTrue(ShopStateService.IsOwned(ballTheme.id));
        Assert.AreEqual("ball_default", ShopStateService.EquippedBallThemeId);
        Assert.AreEqual(worldTheme.price, wallet.Balance);
        Assert.IsFalse(ShopStateService.TryPurchase(ballTheme.id, wallet, out _));
        Assert.AreEqual(worldTheme.price, wallet.Balance);
        Assert.IsTrue(ShopStateService.TryEquip(ballTheme.id));
        Assert.AreEqual(ballTheme.id, ShopStateService.EquippedBallThemeId);
        Assert.AreEqual("world_default", ShopStateService.EquippedWorldThemeId);
        Assert.IsTrue(ShopStateService.TryPurchase(worldTheme.id, wallet, out _));
        Assert.IsTrue(ShopStateService.TryEquip(worldTheme.id));
        Assert.AreEqual(ballTheme.id, ShopStateService.EquippedBallThemeId);
        Assert.AreEqual(worldTheme.id, ShopStateService.EquippedWorldThemeId);
        ColorUtility.TryParseHtmlString(ballTheme.color, out Color ballColor);
        ColorUtility.TryParseHtmlString(worldTheme.color, out Color worldColor);
        Assert.AreEqual(ballColor, ShopCatalog.GetEquippedColor(ShopItemKind.BallTheme, Color.white));
        Assert.AreEqual(worldColor, ShopCatalog.GetEquippedColor(ShopItemKind.WorldTheme, Color.white));
        Assert.AreEqual(ballTheme.id, JsonUtility.FromJson<ShopSaveData>(
            PlayerPrefs.GetString("ShopStateV1")).equippedBallThemeId);
    }

    [Test]
    public void PowerupCountsPersistAndInsufficientFundsDoNotChangeInventory()
    {
        int price = ShopCatalog.Find("shield").price;
        if (price > 0)
        {
            Assert.IsFalse(ShopStateService.TryPurchase("shield", wallet, out _, out ShopPurchaseFailure failure));
            Assert.AreEqual(ShopPurchaseFailure.InsufficientFunds, failure);
        }
        Assert.AreEqual(0, wallet.Balance);
        Assert.AreEqual(0, ShopStateService.GetCount("shield"));
        wallet.AddCoins(price * 2);
        Assert.IsTrue(ShopStateService.TryPurchase("shield", wallet, out _));
        Assert.IsTrue(ShopStateService.TryPurchase("shield", wallet, out _));
        Assert.AreEqual(0, wallet.Balance);
        Assert.AreEqual(2, ShopStateService.GetCount("shield"));
        Assert.IsTrue(ShopStateService.TryConsume("shield"));
        Assert.AreEqual(1, ShopStateService.GetCount("shield"));
        Assert.AreEqual(1, JsonUtility.FromJson<ShopSaveData>(
            PlayerPrefs.GetString("ShopStateV1")).powerups[0].count);
    }

    [Test]
    public void InvalidCatalogIsRejected()
    {
        Assert.IsNull(ShopCatalog.Parse("{\"sections\":[{\"id\":\"x\",\"title\":\"X\",\"items\":[" +
            "{\"id\":\"a\",\"title\":\"A\",\"kind\":\"shield\",\"price\":-1}]}]}", out _));

        ShopCatalogData draft = JsonUtility.FromJson<ShopCatalogData>(JsonUtility.ToJson(ShopCatalog.Current));
        FindItem(draft, ShopItemIds.Shield).kind = (ShopItemKind)999;
        Assert.IsNull(ShopCatalog.Parse(JsonUtility.ToJson(draft), out string kindError));
        StringAssert.Contains("Unsupported item kind", kindError);
    }

    [Test]
    public void CatalogRequiresGameplayItemsAndFreeDefaults()
    {
        ShopCatalogData draft = JsonUtility.FromJson<ShopCatalogData>(JsonUtility.ToJson(ShopCatalog.Current));
        ShopSectionData powerups = Array.Find(draft.sections, section =>
            Array.Exists(section.items, item => item.id == "double_score"));
        powerups.items = Array.FindAll(powerups.items, item => item.id != "double_score");
        Assert.IsNull(ShopCatalog.Parse(JsonUtility.ToJson(draft), out string missingError));
        StringAssert.Contains("double_score", missingError);

        draft = JsonUtility.FromJson<ShopCatalogData>(JsonUtility.ToJson(ShopCatalog.Current));
        FindItem(draft, "ball_default").price = 1;
        Assert.IsNull(ShopCatalog.Parse(JsonUtility.ToJson(draft), out string defaultError));
        StringAssert.Contains("default", defaultError);
    }

    [Test]
    public void EffectSettingsCanBeEditedInCatalog()
    {
        ShopCatalogData draft = JsonUtility.FromJson<ShopCatalogData>(JsonUtility.ToJson(ShopCatalog.Current));
        FindItem(draft, "shield").shieldHits = 3;
        FindItem(draft, "double_score").duration = 7.5f;
        FindItem(draft, "double_score").scoreMultiplier = 4;
        FindItem(draft, "ball_default").color = "#123456";
        FindItem(draft, "world_default").color = "#654321";

        ShopCatalogData edited = ShopCatalog.Parse(JsonUtility.ToJson(draft), out string error);
        Assert.IsNull(error);
        Assert.AreEqual(3, FindItem(edited, "shield").shieldHits);
        Assert.AreEqual(7.5f, FindItem(edited, "double_score").duration);
        Assert.AreEqual(4, FindItem(edited, "double_score").scoreMultiplier);
        Assert.AreEqual("#123456", FindItem(edited, "ball_default").color);
        Assert.AreEqual("#654321", FindItem(edited, "world_default").color);

        FindItem(draft, "shield").shieldHits = 0;
        Assert.IsNull(ShopCatalog.Parse(JsonUtility.ToJson(draft), out _));
        FindItem(draft, "shield").shieldHits = 3;
        FindItem(draft, "double_score").scoreMultiplier = 0;
        Assert.IsNull(ShopCatalog.Parse(JsonUtility.ToJson(draft), out _));
    }

    [Test]
    public void PurchasedEffectValuesReachBallController()
    {
        wallet.AddCoins(ShopCatalog.Find("shield").price + ShopCatalog.Find("double_score").price);
        Assert.IsTrue(ShopStateService.TryPurchase("shield", wallet, out _));
        Assert.IsTrue(ShopStateService.TryPurchase("double_score", wallet, out _));

        var ballObject = new GameObject("ShopTestBall");
        ballObject.SetActive(false);
        NullEvent gameEnded = ScriptableObject.CreateInstance<NullEvent>();
        NullEvent startMoving = ScriptableObject.CreateInstance<NullEvent>();
        try
        {
            BallController ball = ballObject.AddComponent<BallController>();
            typeof(BallController).GetField("OnGameEnded", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ball, gameEnded);
            typeof(BallController).GetField("OnStartMoving", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ball, startMoving);
            ballObject.SetActive(true);
            typeof(BallController).GetField("canMove", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ball, true);

            int shieldCues = 0;
            int doubleScoreCues = 0;
            ball.PowerupFeedback += cue =>
            {
                if (cue == PowerupFeedbackType.ShieldActivated) shieldCues++;
                if (cue == PowerupFeedbackType.DoubleScoreActivated) doubleScoreCues++;
            };
            Assert.IsTrue(ball.TryActivateShield());
            Assert.AreEqual(ShopCatalog.Find("shield").shieldHits, ball.ShieldHitsRemaining);
            Assert.AreEqual(0, ShopStateService.GetCount("shield"));
            Assert.IsFalse(ball.TryActivateShield());
            Assert.AreEqual(1, shieldCues);
            Assert.IsTrue(ball.TryActivateDoubleScore());
            Assert.AreEqual(ShopCatalog.Find("double_score").duration, ball.DoubleScoreRemaining);
            Assert.AreEqual(ShopCatalog.Find("double_score").scoreMultiplier, ball.ScoreMultiplier);
            Assert.AreEqual(0, ShopStateService.GetCount("double_score"));
            Assert.IsFalse(ball.TryActivateDoubleScore());
            Assert.AreEqual(1, doubleScoreCues);
        }
        finally
        {
            Object.DestroyImmediate(ballObject);
            Object.DestroyImmediate(gameEnded);
            Object.DestroyImmediate(startMoving);
        }
    }

    [Test]
    public void CompletedLapsEmitOnlyCoinsActuallyAdded()
    {
        var parent = new GameObject("ShopTestCenter");
        var ballObject = new GameObject("ShopTestBall");
        ballObject.SetActive(false);
        ballObject.transform.SetParent(parent.transform);
        NullEvent gameEnded = ScriptableObject.CreateInstance<NullEvent>();
        NullEvent startMoving = ScriptableObject.CreateInstance<NullEvent>();
        try
        {
            BallController ball = ballObject.AddComponent<BallController>();
            Type type = typeof(BallController);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("OnGameEnded", flags).SetValue(ball, gameEnded);
            type.GetField("OnStartMoving", flags).SetValue(ball, startMoving);
            type.GetField("coinWallet", flags).SetValue(ball, wallet);
            type.GetField("center", flags).SetValue(ball, parent.transform);
            type.GetField("speed", flags).SetValue(ball, 0f);
            type.GetField("maxSpeed", flags).SetValue(ball, 0f);
            ballObject.SetActive(true);
            type.GetField("canMove", flags).SetValue(ball, true);

            int calls = 0;
            int total = 0;
            ball.CoinsEarned += amount => { calls++; total += amount; };
            type.GetField("lapProgress", flags).SetValue(ball, 720f);
            type.GetMethod("Update", flags).Invoke(ball, null);
            Assert.AreEqual(2, wallet.Balance);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(2, total);

            PlayerPrefs.SetInt("CoinBalance", int.MaxValue);
            type.GetField("lapProgress", flags).SetValue(ball, 360f);
            type.GetMethod("Update", flags).Invoke(ball, null);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(2, total);
        }
        finally
        {
            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(gameEnded);
            Object.DestroyImmediate(startMoving);
        }
    }

    private static ShopItemData FindItem(ShopCatalogData catalog, string id)
    {
        foreach (ShopSectionData section in catalog.sections)
            foreach (ShopItemData item in section.items)
                if (item.id == id) return item;
        return null;
    }

    private static ShopItemData FindOptionalTheme(ShopItemKind kind)
    {
        foreach (ShopSectionData section in ShopCatalog.Current.sections)
            foreach (ShopItemData item in section.items)
                if (item.kind == kind && item.id != ShopItemIds.BallDefault &&
                    item.id != ShopItemIds.WorldDefault)
                    return item;
        return null;
    }
}
