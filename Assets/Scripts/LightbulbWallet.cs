using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LightbulbWallet : MonoBehaviour
{
    [SerializeField] int startingLightbulbs = 10;

    int currentLightbulbs;
    TMP_Text counterText;
    static Sprite lightbulbSprite;

    public event Action<int> LightbulbsChanged;
    public int Lightbulbs => currentLightbulbs;

    void Awake()
    {
        currentLightbulbs = Mathf.Max(0, startingLightbulbs);
    }

    void Start()
    {
        EnsureCounterUI();
        UpdateCounter();
    }

    public void AddLightbulbs(int amount)
    {
        if (amount <= 0)
            return;

        currentLightbulbs += amount;
        UpdateCounter();
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0)
            return true;

        if (currentLightbulbs < amount)
            return false;

        currentLightbulbs -= amount;
        UpdateCounter();
        return true;
    }

    void EnsureCounterUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            canvas = CreateCanvas();

        GameObject counterObject = new GameObject("Lightbulb Counter", typeof(RectTransform), typeof(Image));
        counterObject.transform.SetParent(canvas.transform, false);

        RectTransform counterRect = counterObject.GetComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(1f, 1f);
        counterRect.anchorMax = new Vector2(1f, 1f);
        counterRect.pivot = new Vector2(1f, 1f);
        counterRect.anchoredPosition = new Vector2(-28f, -24f);
        counterRect.sizeDelta = new Vector2(220f, 56f);

        Image backgroundImage = counterObject.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.68f);

        GameObject iconObject = new GameObject("Lightbulb Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(counterObject.transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(16f, 0f);
        iconRect.sizeDelta = new Vector2(34f, 34f);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = GetLightbulbSprite();
        iconImage.raycastTarget = false;

        GameObject textObject = new GameObject("Lightbulb Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(counterObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(58f, 0f);
        textRect.offsetMax = new Vector2(-18f, 0f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 30f;
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.color = new Color(1f, 0.88f, 0.36f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        counterText = text;
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

    void UpdateCounter()
    {
        if (counterText != null)
            counterText.text = currentLightbulbs.ToString();

        LightbulbsChanged?.Invoke(currentLightbulbs);
    }

    public static Sprite GetLightbulbSprite()
    {
        if (lightbulbSprite != null)
            return lightbulbSprite;

        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color transparent = new Color(0f, 0f, 0f, 0f);
        Color glow = new Color(1f, 0.9f, 0.28f, 1f);
        Color shadow = new Color(0.9f, 0.58f, 0.08f, 1f);
        Color baseColor = new Color(0.72f, 0.72f, 0.66f, 1f);
        Color baseShadow = new Color(0.42f, 0.42f, 0.4f, 1f);

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                Color pixel = transparent;
                float headX = (x - 32f) / 18f;
                float headY = (y - 37f) / 20f;
                float headDistance = (headX * headX) + (headY * headY);

                if (headDistance <= 1f)
                    pixel = headDistance > 0.72f ? shadow : glow;
                else if (x >= 25 && x <= 39 && y >= 14 && y <= 25)
                    pixel = shadow;
                else if (x >= 22 && x <= 42 && y >= 6 && y <= 15)
                    pixel = y < 10 ? baseShadow : baseColor;

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        lightbulbSprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
        return lightbulbSprite;
    }
}
