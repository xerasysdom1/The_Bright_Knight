using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] Transform player;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text finalScoreText;
    [SerializeField] Button restartButton;
    [SerializeField] bool createDefaultUI = true;
    [SerializeField] float fallY = -4f;
    [SerializeField] float timeScoreMultiplier = 10f;
    [SerializeField] float distanceScoreMultiplier = 1f;

    float startTime;
    float startZ;
    int currentScore;

    public bool IsGameOver { get; private set; }
    public int CurrentScore => currentScore;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        FindPlayer();

        startTime = Time.time;
        startZ = player != null ? player.position.z : 0f;

        if (createDefaultUI)
            EnsureDefaultUI();

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (scoreText != null)
            scoreText.color = Color.black;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateScoreText();
    }

    void Update()
    {
        if (IsGameOver)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                RestartGame();

            return;
        }

        if (player == null)
            FindPlayer();

        UpdateScoreText();

        if (player != null && player.position.y <= fallY)
            GameOver("You fell off!");
    }

    public void GameOver()
    {
        GameOver("Game Over");
    }

    public void GameOver(string reason)
    {
        if (IsGameOver)
            return;

        UpdateScoreText();
        IsGameOver = true;

        if (finalScoreText != null)
            finalScoreText.text = $"{reason}\nScore: {currentScore}\nPress R or Restart";

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void UpdateScoreText()
    {
        float survivedTime = Mathf.Max(0f, Time.time - startTime);
        float distanceTraveled = player != null ? Mathf.Max(0f, player.position.z - startZ) : 0f;

        currentScore = Mathf.FloorToInt((survivedTime * timeScoreMultiplier) + (distanceTraveled * distanceScoreMultiplier));

        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    void EnsureDefaultUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            canvas = CreateCanvas();

        EnsureEventSystem();

        if (scoreText == null)
        {
            scoreText = CreateText(canvas.transform, "Score Text", "Score: 0", 42f, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -25f), new Vector2(500f, 80f));
        }

        if (gameOverPanel == null)
            CreateGameOverPanel(canvas.transform);
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

    void CreateGameOverPanel(Transform canvasTransform)
    {
        gameOverPanel = new GameObject("Game Over Panel", typeof(RectTransform), typeof(Image));
        gameOverPanel.transform.SetParent(canvasTransform, false);

        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = gameOverPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        CreateText(gameOverPanel.transform, "Game Over Text", "Game Over", 72f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 125f), new Vector2(900f, 100f));

        finalScoreText = CreateText(gameOverPanel.transform, "Final Score Text", "Score: 0", 38f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(900f, 160f));

        restartButton = CreateRestartButton(gameOverPanel.transform);
        gameOverPanel.SetActive(false);
    }

    Button CreateRestartButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("Restart Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -95f);
        buttonRect.sizeDelta = new Vector2(280f, 76f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.92f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.92f);
        colors.highlightedColor = new Color(0.88f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.72f, 0.84f, 0.95f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TMP_Text buttonText = CreateText(buttonObject.transform, "Restart Text", "Restart", 34f, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        buttonText.color = new Color(0.05f, 0.08f, 0.12f, 1f);
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

        return textComponent;
    }
}
