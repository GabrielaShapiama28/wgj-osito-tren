using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FruitMinigameController : MonoBehaviour
{
    private const int TotalTarget = 10;

    [Header("Fuentes visuales")]
    [SerializeField] private Sprite bodySheet;
    [SerializeField] private Sprite basketSheet;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer basketRenderer;

    [Header("Juego")]
    [SerializeField] private FruitPlayerController playerController;
    [SerializeField] private Transform fruitContainer;
    [SerializeField] private float roundDurationSeconds = 60f;
    [SerializeField] private float spawnIntervalSeconds = 0.8f;
    [SerializeField] private float fruitFallSpeed = 3.2f;
    [SerializeField] private float spawnMinimumX = -8.1f;
    [SerializeField] private float spawnMaximumX = 8.1f;
    [SerializeField] private float spawnY = 4.85f;

    [Header("Interfaz")]
    [SerializeField] private TMP_Text redCounterText;
    [SerializeField] private TMP_Text greenCounterText;
    [SerializeField] private TMP_Text lemonCounterText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Image redIcon;
    [SerializeField] private Image greenIcon;
    [SerializeField] private Image lemonIcon;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultSummaryText;
    [SerializeField] private Button continueButton;

    private readonly Sprite[] fruitSprites = new Sprite[3];
    private readonly Texture2D[] generatedTextures = new Texture2D[3];
    private Sprite generatedBodySprite;
    private Sprite generatedBasketSprite;
    private float timeRemaining;
    private float spawnCountdown;
    private int redCount;
    private int greenCount;
    private int lemonCount;
    private int redTarget;
    private int greenTarget;
    private int lemonTarget;
    private bool isFinished;
    private bool victory;
    private bool hasContinued;

    public bool IsFinished => isFinished;
    public bool WasVictorious => victory;

    private void Awake()
    {
        GenerateRandomTargets();
        BuildRuntimeSprites();
        timeRemaining = roundDurationSeconds;
        spawnCountdown = 0.25f;
        if (resultPanel != null)
            resultPanel.SetActive(false);
        RefreshInterface();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.PauseGame();

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueToGameplay);
    }

    private void Update()
    {
        if (isFinished)
            return;

        timeRemaining = Mathf.Max(0f, timeRemaining - Time.unscaledDeltaTime);
        spawnCountdown -= Time.unscaledDeltaTime;
        if (spawnCountdown <= 0f)
        {
            SpawnFruit();
            spawnCountdown += spawnIntervalSeconds;
        }

        RefreshTimer();
        if (timeRemaining <= 0f)
            FinishRound(false);
    }

    public void RegisterCaught(FruitPickup fruit)
    {
        if (fruit == null)
            return;

        if (!isFinished)
        {
            switch (fruit.Kind)
            {
                case FruitKind.RedApple:
                    redCount = Mathf.Min(redTarget, redCount + 1);
                    break;
                case FruitKind.GreenApple:
                    greenCount = Mathf.Min(greenTarget, greenCount + 1);
                    break;
                case FruitKind.Lemon:
                    lemonCount = Mathf.Min(lemonTarget, lemonCount + 1);
                    break;
            }

            RefreshCounters();
            if (redCount >= redTarget && greenCount >= greenTarget && lemonCount >= lemonTarget)
                FinishRound(true);
        }

        Destroy(fruit.gameObject);
    }

    public void RegisterMissed(FruitPickup fruit)
    {
        if (fruit != null)
            Destroy(fruit.gameObject);
    }

    public void ContinueToGameplay()
    {
        if (hasContinued || !isFinished)
            return;

        hasContinued = true;
        if (GameManager.Instance != null)
            GameManager.Instance.ApplyMinigameResult(victory, 2);

        SceneNavigator.Load(SceneNavigator.Gameplay);
    }

    public void Configure(
        Sprite configuredBodySheet,
        Sprite configuredBasketSheet,
        SpriteRenderer configuredBodyRenderer,
        SpriteRenderer configuredBasketRenderer,
        FruitPlayerController configuredPlayer,
        Transform configuredFruitContainer,
        TMP_Text configuredRedCounter,
        TMP_Text configuredGreenCounter,
        TMP_Text configuredLemonCounter,
        TMP_Text configuredTimer,
        Image configuredRedIcon,
        Image configuredGreenIcon,
        Image configuredLemonIcon,
        GameObject configuredResultPanel,
        TMP_Text configuredResultTitle,
        TMP_Text configuredResultSummary,
        Button configuredContinueButton)
    {
        bodySheet = configuredBodySheet;
        basketSheet = configuredBasketSheet;
        bodyRenderer = configuredBodyRenderer;
        basketRenderer = configuredBasketRenderer;
        playerController = configuredPlayer;
        fruitContainer = configuredFruitContainer;
        redCounterText = configuredRedCounter;
        greenCounterText = configuredGreenCounter;
        lemonCounterText = configuredLemonCounter;
        timerText = configuredTimer;
        redIcon = configuredRedIcon;
        greenIcon = configuredGreenIcon;
        lemonIcon = configuredLemonIcon;
        resultPanel = configuredResultPanel;
        resultTitleText = configuredResultTitle;
        resultSummaryText = configuredResultSummary;
        continueButton = configuredContinueButton;
    }

    private void SpawnFruit()
    {
        FruitKind kind = (FruitKind)Random.Range(0, 3);
        GameObject fruitObject = new GameObject($"{kind}Fruit");
        fruitObject.transform.SetParent(fruitContainer, false);
        fruitObject.transform.position = new Vector3(
            Random.Range(spawnMinimumX, spawnMaximumX),
            spawnY,
            0f);

        SpriteRenderer renderer = fruitObject.AddComponent<SpriteRenderer>();
        renderer.sprite = fruitSprites[(int)kind];
        renderer.sortingOrder = 20;

        Rigidbody2D rigidbody2D = fruitObject.AddComponent<Rigidbody2D>();
        rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        rigidbody2D.gravityScale = 0f;
        rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (kind == FruitKind.Lemon)
        {
            CapsuleCollider2D collider = fruitObject.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = new Vector2(0.9f, 0.58f);
        }
        else
        {
            CircleCollider2D collider = fruitObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.43f;
        }

        FruitPickup pickup = fruitObject.AddComponent<FruitPickup>();
        pickup.Initialize(this, kind, rigidbody2D, fruitFallSpeed);
    }

    private void FinishRound(bool won)
    {
        if (isFinished)
            return;

        isFinished = true;
        victory = won;
        playerController?.SetMovementEnabled(false);

        FruitPickup[] remainingFruit = fruitContainer != null
            ? fruitContainer.GetComponentsInChildren<FruitPickup>()
            : FindObjectsByType<FruitPickup>(FindObjectsSortMode.None);
        foreach (FruitPickup fruit in remainingFruit)
            Destroy(fruit.gameObject);

        if (resultTitleText != null)
            resultTitleText.text = won ? "¡Misión completada!" : "¡Se acabó el tiempo!";
        if (resultSummaryText != null)
        {
            resultSummaryText.text = won
                ? "¡Recogiste todas las frutas que pidió Mamá Oso!"
                : $"Recogiste {redCount + greenCount + lemonCount}/{TotalTarget} frutas.\nPuedes continuar el viaje.";
        }
        if (resultPanel != null)
            resultPanel.SetActive(true);
    }

    private void RefreshInterface()
    {
        RefreshCounters();
        RefreshTimer();
    }

    private void RefreshCounters()
    {
        if (redCounterText != null)
            redCounterText.text = $"Manzanas rojas: {redCount}/{redTarget}";
        if (greenCounterText != null)
            greenCounterText.text = $"Manzanas verdes: {greenCount}/{greenTarget}";
        if (lemonCounterText != null)
            lemonCounterText.text = $"Limones: {lemonCount}/{lemonTarget}";
    }

    private void GenerateRandomTargets()
    {
        // Dos puntos de corte distintos entre 1 y 9 producen tres cantidades
        // positivas que siempre suman exactamente diez.
        int firstCut = Random.Range(1, TotalTarget - 1);
        int secondCut = Random.Range(firstCut + 1, TotalTarget);
        redTarget = firstCut;
        greenTarget = secondCut - firstCut;
        lemonTarget = TotalTarget - secondCut;
    }

    private void RefreshTimer()
    {
        if (timerText != null)
            timerText.text = $"Tiempo: {Mathf.CeilToInt(timeRemaining):00}";
    }

    private void BuildRuntimeSprites()
    {
        generatedBodySprite = CreateMiddleFrame(bodySheet, "BearBody");
        generatedBasketSprite = CreateMiddleFrame(basketSheet, "BearBasket");
        if (bodyRenderer != null)
            bodyRenderer.sprite = generatedBodySprite;
        if (basketRenderer != null)
            basketRenderer.sprite = generatedBasketSprite;

        generatedTextures[0] = CreateAppleTexture(new Color32(220, 64, 70, 255), "RedAppleTexture");
        generatedTextures[1] = CreateAppleTexture(new Color32(92, 184, 79, 255), "GreenAppleTexture");
        generatedTextures[2] = CreateLemonTexture();

        for (int i = 0; i < fruitSprites.Length; i++)
        {
            fruitSprites[i] = Sprite.Create(
                generatedTextures[i],
                new Rect(0f, 0f, 64f, 64f),
                new Vector2(0.5f, 0.5f),
                64f,
                0,
                SpriteMeshType.FullRect);
            fruitSprites[i].name = ((FruitKind)i).ToString();
        }

        if (redIcon != null)
            redIcon.sprite = fruitSprites[0];
        if (greenIcon != null)
            greenIcon.sprite = fruitSprites[1];
        if (lemonIcon != null)
            lemonIcon.sprite = fruitSprites[2];
    }

    private static Sprite CreateMiddleFrame(Sprite sheet, string spriteName)
    {
        if (sheet == null || sheet.texture == null)
            return null;

        float frameWidth = sheet.texture.width / 3f;
        Sprite sprite = Sprite.Create(
            sheet.texture,
            new Rect(frameWidth, 0f, frameWidth, sheet.texture.height),
            new Vector2(0.5f, 0.5f),
            32f,
            0,
            SpriteMeshType.FullRect);
        sprite.name = spriteName;
        return sprite;
    }

    private static Texture2D CreateAppleTexture(Color32 bodyColor, string textureName)
    {
        Texture2D texture = CreateTransparentTexture(textureName);
        Color32[] pixels = new Color32[64 * 64];
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float nx = ((x + 0.5f) / 64f) * 2f - 1f;
                float ny = ((y + 0.5f) / 64f) * 2f - 1f;
                bool leftLobe = Square(nx + 0.27f) + Square(ny - 0.16f) <= Square(0.43f);
                bool rightLobe = Square(nx - 0.27f) + Square(ny - 0.16f) <= Square(0.43f);
                bool lowerBody = Square(nx / 0.65f) + Square((ny + 0.20f) / 0.67f) <= 1f;
                Color32 color = leftLobe || rightLobe || lowerBody ? bodyColor : new Color32(0, 0, 0, 0);

                if (Mathf.Abs(nx) < 0.055f && ny > 0.42f && ny < 0.78f)
                    color = new Color32(112, 70, 35, 255);
                if (Square((nx - 0.24f) / 0.28f) + Square((ny - 0.61f) / 0.16f) <= 1f)
                    color = new Color32(55, 133, 65, 255);

                pixels[y * 64 + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D CreateLemonTexture()
    {
        Texture2D texture = CreateTransparentTexture("LemonTexture");
        Color32[] pixels = new Color32[64 * 64];
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float nx = ((x + 0.5f) / 64f) * 2f - 1f;
                float ny = ((y + 0.5f) / 64f) * 2f - 1f;
                bool ellipse = Square(nx / 0.72f) + Square(ny / 0.43f) <= 1f;
                bool leftTip = nx > -0.92f && nx < -0.62f && Mathf.Abs(ny) < (nx + 0.92f) * 0.9f;
                bool rightTip = nx < 0.92f && nx > 0.62f && Mathf.Abs(ny) < (0.92f - nx) * 0.9f;
                Color32 color = ellipse || leftTip || rightTip
                    ? new Color32(250, 211, 64, 255)
                    : new Color32(0, 0, 0, 0);

                if (Square((nx - 0.18f) / 0.25f) + Square((ny - 0.52f) / 0.13f) <= 1f)
                    color = new Color32(61, 145, 69, 255);

                pixels[y * 64 + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D CreateTransparentTexture(string textureName)
    {
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.name = textureName;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private static float Square(float value) => value * value;

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(ContinueToGameplay);

        foreach (Sprite sprite in fruitSprites)
        {
            if (sprite != null)
                Destroy(sprite);
        }
        foreach (Texture2D texture in generatedTextures)
        {
            if (texture != null)
                Destroy(texture);
        }
        if (generatedBodySprite != null)
            Destroy(generatedBodySprite);
        if (generatedBasketSprite != null)
            Destroy(generatedBasketSprite);
    }
}
