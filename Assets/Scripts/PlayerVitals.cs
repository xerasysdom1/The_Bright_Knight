using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerVitals : MonoBehaviour
{
    [SerializeField] int maxHealth = 20;
    [SerializeField] float maxMana = 10f;
    [SerializeField] float secondsPerMana = 4f;
    [SerializeField] Vector2 barSize = new Vector2(520f, 28f);
    [SerializeField] float bottomPadding = 34f;
    [SerializeField] float barSpacing = 8f;
    [SerializeField] float damageInvulnerability = 0.32f;

    int currentHealth;
    float currentMana;
    float manaRegenTimer;
    float nextDamageTime;
    RectTransform healthFill;
    RectTransform manaFill;
    TMP_Text healthText;
    TMP_Text manaText;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float CurrentMana => currentMana;
    public float MaxMana => maxMana;

    void Awake()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
    }

    void Start()
    {
        EnsureVitalsUI();
        UpdateUI();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (CheckDeath())
            return;

        if (currentMana >= maxMana)
        {
            manaRegenTimer = 0f;
            return;
        }

        manaRegenTimer += Time.deltaTime;
        if (manaRegenTimer < secondsPerMana)
            return;

        manaRegenTimer -= secondsPerMana;
        currentMana = Mathf.Min(maxMana, currentMana + 1f);
        UpdateUI();
    }

    public bool TryUseMana(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentMana >= amount)
        {
            currentMana -= amount;
            manaRegenTimer = 0f;
            UpdateUI();
            return true;
        }

        TakeDamage(1);
        return false;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0 || Time.time < nextDamageTime)
            return;

        nextDamageTime = Time.time + damageInvulnerability;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        GameAudio.PlayHurt();
        UpdateUI();
        CheckDeath();
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateUI();
    }

    public void IncreaseMaxHealth(int amount, bool fillAddedHealth = true)
    {
        if (amount <= 0)
            return;

        maxHealth += amount;
        if (fillAddedHealth)
            currentHealth += amount;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateUI();
        CheckDeath();
    }

    public void IncreaseMaxMana(float amount, bool fillAddedMana = true)
    {
        if (amount <= 0f)
            return;

        maxMana += amount;
        if (fillAddedMana)
            currentMana += amount;

        currentMana = Mathf.Clamp(currentMana, 0f, maxMana);
        UpdateUI();
    }

    void EnsureVitalsUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            canvas = CreateCanvas();

        CreateBar(canvas.transform, "Health Bar", "Health", new Color(0.84f, 0.06f, 0.05f, 1f), bottomPadding, out healthFill, out healthText);

        float manaOffset = bottomPadding + barSize.y + barSpacing;
        CreateBar(canvas.transform, "Mana Bar", "Mana", new Color(0.05f, 0.32f, 0.95f, 1f), manaOffset, out manaFill, out manaText);
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

    void CreateBar(Transform parent, string objectName, string label, Color fillColor, float yOffset, out RectTransform fillRect, out TMP_Text valueText)
    {
        GameObject barObject = new GameObject(objectName, typeof(RectTransform));
        barObject.transform.SetParent(parent, false);

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0f, yOffset);
        barRect.sizeDelta = barSize;

        GameObject backgroundObject = new GameObject($"{objectName} Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(barObject.transform, false);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject fillObject = new GameObject($"{objectName} Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(backgroundObject.transform, false);

        fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = fillColor;

        GameObject textObject = new GameObject($"{objectName} Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(barObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 0f);
        textRect.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        valueText = text;
    }

    void UpdateUI()
    {
        SetFill(healthFill, maxHealth > 0 ? (float)currentHealth / maxHealth : 0f);
        SetFill(manaFill, maxMana > 0f ? currentMana / maxMana : 0f);

        if (healthText != null)
            healthText.text = $"Health {currentHealth}/{maxHealth}";

        if (manaText != null)
            manaText.text = $"Mana {Mathf.FloorToInt(currentMana)}/{Mathf.CeilToInt(maxMana)}";
    }

    bool CheckDeath()
    {
        if (currentHealth >= 1 || GameManager.Instance == null)
            return false;

        GameManager.Instance.GameOver("You ran out of health!");
        return true;
    }

    void SetFill(RectTransform fillRect, float normalizedAmount)
    {
        if (fillRect == null)
            return;

        fillRect.anchorMax = new Vector2(Mathf.Clamp01(normalizedAmount), 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }
}
