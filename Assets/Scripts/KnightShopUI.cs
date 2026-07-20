using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class KnightShopUI : MonoBehaviour
{
    enum ShopUpgrade
    {
        Health,
        Mana,
        SparkSpell,
        RadiantGuardSpell
    }

    class ShopRow
    {
        public ShopUpgrade upgrade;
        public int cost;
        public TMP_Text costText;
        public TMP_Text buttonText;
        public Button button;
    }

    static KnightShopUI instance;

    readonly List<ShopRow> rows = new List<ShopRow>();
    GameObject shopRoot;
    TMP_Text walletText;
    TMP_Text statusText;
    LightbulbWallet wallet;
    PlayerVitals vitals;
    KnightUpgradeState upgradeState;

    public static bool IsOpen => instance != null && instance.shopRoot != null && instance.shopRoot.activeSelf;

    public static void ToggleForPlayer(GameObject player)
    {
        KnightShopUI shop = GetOrCreate();
        if (IsOpen)
            shop.Close();
        else
            shop.Open(player);
    }

    public static void CloseIfOpen()
    {
        if (instance != null)
            instance.Close();
    }

    static KnightShopUI GetOrCreate()
    {
        if (instance != null)
            return instance;

        KnightShopUI foundShop = FindAnyObjectByType<KnightShopUI>();
        if (foundShop != null)
        {
            instance = foundShop;
            return instance;
        }

        GameObject shopObject = new GameObject("Knight Shop UI");
        instance = shopObject.AddComponent<KnightShopUI>();
        return instance;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildUI();
    }

    void Open(GameObject player)
    {
        CachePlayerComponents(player);
        statusText.text = "Trade lightbulbs for upgrades.";
        shopRoot.SetActive(true);
        UpdateShopState();
    }

    void Close()
    {
        if (shopRoot != null)
            shopRoot.SetActive(false);
    }

    void CachePlayerComponents(GameObject player)
    {
        if (player == null)
            return;

        wallet = player.GetComponent<LightbulbWallet>();
        if (wallet == null)
            wallet = player.AddComponent<LightbulbWallet>();

        vitals = player.GetComponent<PlayerVitals>();
        if (vitals == null)
            vitals = player.AddComponent<PlayerVitals>();

        upgradeState = player.GetComponent<KnightUpgradeState>();
        if (upgradeState == null)
            upgradeState = player.AddComponent<KnightUpgradeState>();
    }

    void BuildUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            canvas = CreateCanvas();

        EnsureEventSystem();

        shopRoot = new GameObject("Shop UI", typeof(RectTransform), typeof(Image));
        shopRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = shopRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image scrimImage = shopRoot.GetComponent<Image>();
        scrimImage.color = new Color(0f, 0f, 0f, 0.48f);

        GameObject panelObject = new GameObject("Shop Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(shopRoot.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(760f, 570f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.055f, 0.049f, 0.045f, 0.97f);

        TMP_Text titleText = CreateText(panelObject.transform, "Shop Title", "Goblin Shop", 38f, TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(-58f, 60f));
        titleText.color = new Color(1f, 0.83f, 0.32f, 1f);

        walletText = CreateText(panelObject.transform, "Shop Wallet", "Lightbulbs 0", 24f, TextAlignmentOptions.Right,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-34f, -78f), new Vector2(-58f, 44f));
        walletText.color = new Color(1f, 0.88f, 0.36f, 1f);

        Button closeButton = CreateButton(panelObject.transform, "Close Button", "X", 26f,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -20f), new Vector2(48f, 48f));
        closeButton.onClick.AddListener(Close);

        float rowY = -142f;
        CreateUpgradeRow(panelObject.transform, ShopUpgrade.Health, "Max Health +5", "Raises the knight's health cap and fills the added health.", 4, rowY);
        CreateUpgradeRow(panelObject.transform, ShopUpgrade.Mana, "Max Mana +2", "Raises the mana cap and fills the added mana.", 4, rowY - 86f);
        CreateUpgradeRow(panelObject.transform, ShopUpgrade.SparkSpell, "Spark Spell", "Adds 1 radiant damage to every sword attack.", 6, rowY - 172f);
        CreateUpgradeRow(panelObject.transform, ShopUpgrade.RadiantGuardSpell, "Radiant Guard", "Your parry counters shadows for 2 damage instead of 1.", 8, rowY - 258f);

        statusText = CreateText(panelObject.transform, "Shop Status", "", 22f, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(-70f, 44f));
        statusText.color = new Color(0.96f, 0.9f, 0.78f, 1f);

        shopRoot.SetActive(false);
    }

    void CreateUpgradeRow(Transform parent, ShopUpgrade upgrade, string title, string description, int cost, float y)
    {
        GameObject rowObject = new GameObject(title + " Row", typeof(RectTransform), typeof(Image));
        rowObject.transform.SetParent(parent, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowRect.sizeDelta = new Vector2(-70f, 70f);

        Image rowImage = rowObject.GetComponent<Image>();
        rowImage.color = new Color(0.12f, 0.105f, 0.09f, 0.95f);

        TMP_Text titleText = CreateText(rowObject.transform, title + " Title", title, 24f, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -7f), new Vector2(420f, 30f));
        titleText.color = Color.white;

        TMP_Text descriptionText = CreateText(rowObject.transform, title + " Description", description, 16f, TextAlignmentOptions.Left,
            new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(20f, -3f), new Vector2(420f, 30f));
        descriptionText.color = new Color(0.78f, 0.76f, 0.7f, 1f);

        TMP_Text costText = CreateText(rowObject.transform, title + " Cost", cost.ToString(), 22f, TextAlignmentOptions.Right,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-156f, 0f), new Vector2(60f, 40f));
        costText.color = new Color(1f, 0.86f, 0.3f, 1f);

        GameObject iconObject = new GameObject(title + " Cost Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(rowObject.transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(1f, 0.5f);
        iconRect.anchorMax = new Vector2(1f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-112f, 0f);
        iconRect.sizeDelta = new Vector2(28f, 28f);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = LightbulbWallet.GetLightbulbSprite();
        iconImage.raycastTarget = false;

        Button buyButton = CreateButton(rowObject.transform, title + " Buy Button", "Buy", 18f,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(86f, 42f));

        ShopRow row = new ShopRow
        {
            upgrade = upgrade,
            cost = cost,
            costText = costText,
            buttonText = buyButton.GetComponentInChildren<TMP_Text>(),
            button = buyButton
        };

        buyButton.onClick.AddListener(() => TryBuy(row));
        rows.Add(row);
    }

    Button CreateButton(Transform parent, string objectName, string text, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.pivot = pivot;
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = sizeDelta;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.92f, 0.72f, 0.24f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.92f, 0.72f, 0.24f, 1f);
        colors.highlightedColor = new Color(1f, 0.84f, 0.34f, 1f);
        colors.pressedColor = new Color(0.72f, 0.52f, 0.14f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.26f, 0.24f, 0.22f, 0.9f);
        button.colors = colors;

        TMP_Text buttonText = CreateText(buttonObject.transform, objectName + " Text", text, fontSize, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        buttonText.color = new Color(0.08f, 0.06f, 0.035f, 1f);
        buttonText.raycastTarget = false;

        return button;
    }

    TMP_Text CreateText(Transform parent, string objectName, string text, float fontSize, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        textComponent.raycastTarget = false;

        return textComponent;
    }

    void TryBuy(ShopRow row)
    {
        if (wallet == null || vitals == null || upgradeState == null)
            return;

        if (IsOwned(row.upgrade))
        {
            statusText.text = "Already owned.";
            UpdateShopState();
            return;
        }

        if (!wallet.TrySpend(row.cost))
        {
            int needed = row.cost - wallet.Lightbulbs;
            statusText.text = $"Need {needed} more lightbulbs.";
            UpdateShopState();
            return;
        }

        ApplyUpgrade(row.upgrade);
        statusText.text = "Upgrade bought.";
        UpdateShopState();
    }

    void ApplyUpgrade(ShopUpgrade upgrade)
    {
        switch (upgrade)
        {
            case ShopUpgrade.Health:
                vitals.IncreaseMaxHealth(5);
                break;
            case ShopUpgrade.Mana:
                vitals.IncreaseMaxMana(2f);
                break;
            case ShopUpgrade.SparkSpell:
                upgradeState.LearnSparkSpell();
                break;
            case ShopUpgrade.RadiantGuardSpell:
                upgradeState.LearnRadiantGuardSpell();
                break;
        }
    }

    bool IsOwned(ShopUpgrade upgrade)
    {
        if (upgradeState == null)
            return false;

        return upgrade switch
        {
            ShopUpgrade.SparkSpell => upgradeState.HasSparkSpell,
            ShopUpgrade.RadiantGuardSpell => upgradeState.HasRadiantGuardSpell,
            _ => false
        };
    }

    void UpdateShopState()
    {
        if (walletText != null)
            walletText.text = wallet != null ? $"Lightbulbs {wallet.Lightbulbs}" : "Lightbulbs 0";

        foreach (ShopRow row in rows)
        {
            bool owned = IsOwned(row.upgrade);
            row.costText.text = row.cost.ToString();
            row.button.interactable = !owned;
            row.buttonText.text = owned ? "Owned" : "Buy";
        }
    }

    Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        EventSystem.current = eventSystemObject.GetComponent<EventSystem>();
    }
}
