#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FruitMinigameBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/FruitMinigame.unity";
    private const string AssetRoot = "Assets/Game/Sprites/Minigames/Fruit";
    private const string RevisionMarker = "FruitMinigameRevision_3";
    private static readonly Color DeepBlue = Hex("2E6590");
    private static readonly Color Pink = Hex("F5A9C0");
    private static readonly Color Cream = Hex("FFF6DE");

    [InitializeOnLoadMethod]
    private static void BuildWhenFruitSceneIsOpen()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        QueueActiveSceneBuild();
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            QueueActiveSceneBuild();
    }

    private static void QueueActiveSceneBuild()
    {
        EditorApplication.delayCall += TryBuildActiveFruitScene;
    }

    private static void TryBuildActiveFruitScene()
    {
        if (EditorApplication.isCompiling
            || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        bool isFruitScene = scene.IsValid()
            && (string.Equals(scene.path, ScenePath, StringComparison.Ordinal)
                || string.Equals(scene.name, "FruitMinigame", StringComparison.Ordinal));
        if (!isFruitScene || FindSceneObject(scene, RevisionMarker) != null)
            return;

        try
        {
            BuildFruitMinigame();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Bearly in Time/Rebuild Fruit Minigame")]
    public static void BuildFruitMinigame()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureSpriteImports();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Camera camera = FindSceneComponent<Camera>(scene, "MainCamera");
        if (camera == null)
            camera = CreateCamera();
        ConfigureCamera(camera);

        RemoveSceneObject(scene, "FruitMinigameRoot");
        RemoveSceneObject(scene, "Canvas");

        GameObject gameRoot = new GameObject("FruitMinigameRoot");
        FruitMinigameController controller = gameRoot.AddComponent<FruitMinigameController>();

        Sprite backgroundSprite = LoadSprite($"{AssetRoot}/fondo 1.png");
        Sprite floorSprite = LoadSprite($"{AssetRoot}/madera 1.png");
        Sprite bodySheet = LoadSprite($"{AssetRoot}/BODY OSO.png");
        Sprite basketSheet = LoadSprite($"{AssetRoot}/canastaa.png");

        BuildEnvironment(gameRoot.transform, backgroundSprite, floorSprite);

        Transform fruitContainer = new GameObject("FallingFruit").transform;
        fruitContainer.SetParent(gameRoot.transform, false);

        FruitPlayerController player = BuildPlayer(
            gameRoot.transform,
            out SpriteRenderer bodyRenderer,
            out SpriteRenderer basketRenderer);
        BuildMissZone(gameRoot.transform);

        Canvas canvas = CreateCanvas();
        canvas.transform.SetParent(gameRoot.transform, false);
        BuildInterface(
            canvas.transform,
            out TMP_Text redCounter,
            out TMP_Text greenCounter,
            out TMP_Text lemonCounter,
            out TMP_Text timer,
            out UnityEngine.UI.Image redIcon,
            out UnityEngine.UI.Image greenIcon,
            out UnityEngine.UI.Image lemonIcon,
            out GameObject resultPanel,
            out TMP_Text resultTitle,
            out TMP_Text resultSummary,
            out UnityEngine.UI.Button continueButton);

        controller.Configure(
            bodySheet,
            basketSheet,
            bodyRenderer,
            basketRenderer,
            player,
            fruitContainer,
            redCounter,
            greenCounter,
            lemonCounter,
            timer,
            redIcon,
            greenIcon,
            lemonIcon,
            resultPanel,
            resultTitle,
            resultSummary,
            continueButton);
        resultPanel.SetActive(false);

        GameObject marker = new GameObject(RevisionMarker);
        marker.transform.SetParent(gameRoot.transform, false);
        marker.SetActive(false);

        EnsureEventSystem(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unity no pudo guardar FruitMinigame.unity.");

        AssetDatabase.SaveAssets();
        Debug.Log("[Bearly in Time] FruitMinigame construido correctamente.");
    }

    [MenuItem("Bearly in Time/Validate Fruit Minigame")]
    public static void ValidateFruitMinigame()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        List<string> errors = new List<string>();

        RequireComponent<FruitMinigameController>(scene, "FruitMinigameController", errors);
        RequireComponent<FruitPlayerController>(scene, "FruitPlayerController", errors);
        RequireComponent<FruitBasketCatcher>(scene, "FruitBasketCatcher", errors);
        RequireComponent<FruitMissZone>(scene, "FruitMissZone", errors);
        RequireComponent<Canvas>(scene, "Canvas", errors);
        RequireComponent<EventSystem>(scene, "EventSystem", errors);
        RequireNamed(scene, "BackgroundRepeats", errors);
        RequireNamed(scene, "WoodFloor", errors);
        RequireNamed(scene, "ResultPanel", errors);
        RequireNamed(scene, "ContinueButton", errors);
        RequireNamed(scene, RevisionMarker, errors);

        foreach (string fileName in new[] { "fondo 1.png", "madera 1.png", "BODY OSO.png", "canastaa.png" })
        {
            string path = $"{AssetRoot}/{fileName}";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite)
                errors.Add($"No está importado como Sprite: {path}");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", errors));

        Debug.Log("[Bearly in Time] FruitMinigame validado: escenario, jugador, física 2D, HUD, temporizador y resultado conectados.");
    }

    private static void ConfigureSpriteImports()
    {
        string[] expectedFiles = { "fondo 1.png", "madera 1.png", "BODY OSO.png", "canastaa.png" };
        foreach (string fileName in expectedFiles)
        {
            string assetPath = $"{AssetRoot}/{fileName}";
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"No se encontró el asset {assetPath}.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 50f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("MainCamera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static void ConfigureCamera(Camera camera)
    {
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Hex("97BA39");
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void BuildEnvironment(Transform parent, Sprite backgroundSprite, Sprite floorSprite)
    {
        GameObject background = new GameObject("BackgroundRepeats");
        background.transform.SetParent(parent, false);
        background.transform.localScale = Vector3.one * 2f;
        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = backgroundSprite;
        backgroundRenderer.sortingOrder = -20;

        GameObject floor = new GameObject("WoodFloor");
        floor.transform.SetParent(parent, false);
        floor.transform.localScale = Vector3.one * 2f;
        SpriteRenderer floorRenderer = floor.AddComponent<SpriteRenderer>();
        floorRenderer.sprite = floorSprite;
        floorRenderer.sortingOrder = -10;
    }

    private static FruitPlayerController BuildPlayer(
        Transform parent,
        out SpriteRenderer bodyRenderer,
        out SpriteRenderer basketRenderer)
    {
        GameObject playerObject = new GameObject("BearAndBasket");
        playerObject.transform.SetParent(parent, false);
        playerObject.transform.position = new Vector3(0f, -3.55f, 0f);

        Rigidbody2D rigidbody2D = playerObject.AddComponent<Rigidbody2D>();
        rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        rigidbody2D.gravityScale = 0f;
        rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;

        FruitPlayerController player = playerObject.AddComponent<FruitPlayerController>();
        player.Configure(8f, -8.1f, 8.1f);

        GameObject bodyObject = new GameObject("BearBody");
        bodyObject.transform.SetParent(playerObject.transform, false);
        bodyObject.transform.localScale = Vector3.one * 1.6f;
        bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
        bodyRenderer.sortingOrder = 10;

        GameObject basketObject = new GameObject("BasketCatchZone");
        basketObject.transform.SetParent(playerObject.transform, false);
        // La canasta pertenece al mismo root móvil que el oso y descansa sobre su cabeza.
        basketObject.transform.localPosition = new Vector3(0f, 0.10f, 0f);
        basketObject.transform.localScale = Vector3.one * 1.6f;
        basketRenderer = basketObject.AddComponent<SpriteRenderer>();
        basketRenderer.sortingOrder = 11;

        GameObject basketTrigger = new GameObject("BasketCollisionTrigger");
        basketTrigger.transform.SetParent(playerObject.transform, false);
        basketTrigger.transform.localPosition = new Vector3(0f, 2.20f, 0f);
        BoxCollider2D basketCollider = basketTrigger.AddComponent<BoxCollider2D>();
        basketCollider.isTrigger = true;
        basketCollider.size = new Vector2(1.8f, 0.35f);
        basketTrigger.AddComponent<FruitBasketCatcher>();

        return player;
    }

    private static void BuildMissZone(Transform parent)
    {
        GameObject missZone = new GameObject("FruitMissZone");
        missZone.transform.SetParent(parent, false);
        missZone.transform.position = new Vector3(0f, -5.05f, 0f);
        BoxCollider2D collider = missZone.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(20f, 0.7f);
        missZone.AddComponent<FruitMissZone>();
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void BuildInterface(
        Transform canvas,
        out TMP_Text redCounter,
        out TMP_Text greenCounter,
        out TMP_Text lemonCounter,
        out TMP_Text timer,
        out UnityEngine.UI.Image redIcon,
        out UnityEngine.UI.Image greenIcon,
        out UnityEngine.UI.Image lemonIcon,
        out GameObject resultPanel,
        out TMP_Text resultTitle,
        out TMP_Text resultSummary,
        out UnityEngine.UI.Button continueButton)
    {
        UnityEngine.UI.Image objectivePanel = CreateImage("ObjectivePanel", canvas, new Color(1f, 0.97f, 0.88f, 0.93f));
        SetTopLeft(objectivePanel.rectTransform, new Vector2(32f, -32f), new Vector2(540f, 260f));
        AddOutline(objectivePanel.gameObject, new Color(1f, 1f, 1f, 0.95f), 4f);

        TMP_Text objectiveTitle = CreateText("ObjectiveTitle", objectivePanel.transform, "Frutas para Mamá Oso", 34f, DeepBlue, TextAlignmentOptions.Left);
        SetTopLeft(objectiveTitle.rectTransform, new Vector2(24f, -18f), new Vector2(490f, 48f));
        objectiveTitle.fontStyle = FontStyles.Bold;

        CreateCounterRow(objectivePanel.transform, "Red", -80f, out redIcon, out redCounter);
        CreateCounterRow(objectivePanel.transform, "Green", -137f, out greenIcon, out greenCounter);
        CreateCounterRow(objectivePanel.transform, "Lemon", -194f, out lemonIcon, out lemonCounter);

        UnityEngine.UI.Image timerPanel = CreateImage("TimerPanel", canvas, new Color(1f, 0.72f, 0.82f, 0.95f));
        SetTopCenter(timerPanel.rectTransform, new Vector2(0f, -30f), new Vector2(310f, 84f));
        AddOutline(timerPanel.gameObject, Color.white, 4f);
        timer = CreateText("TimerText", timerPanel.transform, "Tiempo: 60", 42f, DeepBlue, TextAlignmentOptions.Center);
        Stretch(timer.rectTransform, 12f);
        timer.fontStyle = FontStyles.Bold;

        TMP_Text instructions = CreateText(
            "Instructions",
            canvas,
            "←   →   Mueve al osito y atrapa las frutas",
            30f,
            Color.white,
            TextAlignmentOptions.Center);
        SetBottomCenter(instructions.rectTransform, new Vector2(0f, 28f), new Vector2(760f, 52f));
        AddOutline(instructions.gameObject, DeepBlue, 2f);

        UnityEngine.UI.Image overlay = CreateImage("ResultPanel", canvas, new Color(0.08f, 0.16f, 0.22f, 0.72f));
        Stretch(overlay.rectTransform, 0f);
        resultPanel = overlay.gameObject;

        UnityEngine.UI.Image card = CreateImage("ResultCard", overlay.transform, Cream);
        SetCentered(card.rectTransform, Vector2.zero, new Vector2(660f, 410f));
        AddOutline(card.gameObject, Color.white, 5f);

        resultTitle = CreateText("ResultTitle", card.transform, "¡Misión completada!", 52f, DeepBlue, TextAlignmentOptions.Center);
        SetTopCenter(resultTitle.rectTransform, new Vector2(0f, -42f), new Vector2(590f, 80f));
        resultTitle.fontStyle = FontStyles.Bold;

        resultSummary = CreateText("ResultSummary", card.transform, string.Empty, 30f, DeepBlue, TextAlignmentOptions.Center);
        SetCentered(resultSummary.rectTransform, new Vector2(0f, 16f), new Vector2(560f, 150f));
        resultSummary.textWrappingMode = TextWrappingModes.Normal;

        continueButton = CreateButton("ContinueButton", card.transform, "Continuar");
        SetBottomCenter((RectTransform)continueButton.transform, new Vector2(0f, 42f), new Vector2(280f, 78f));
    }

    private static void CreateCounterRow(
        Transform parent,
        string prefix,
        float y,
        out UnityEngine.UI.Image icon,
        out TMP_Text label)
    {
        icon = CreateImage($"{prefix}Icon", parent, Color.white);
        SetTopLeft(icon.rectTransform, new Vector2(24f, y), new Vector2(46f, 46f));
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        label = CreateText($"{prefix}Counter", parent, string.Empty, 29f, DeepBlue, TextAlignmentOptions.Left);
        SetTopLeft(label.rectTransform, new Vector2(84f, y - 1f), new Vector2(420f, 48f));
    }

    private static UnityEngine.UI.Button CreateButton(string name, Transform parent, string label)
    {
        UnityEngine.UI.Image image = CreateImage(name, parent, Pink);
        image.raycastTarget = true;
        AddOutline(image.gameObject, Color.white, 4f);
        UnityEngine.UI.Button button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Pink;
        colors.highlightedColor = Hex("FFC4D3");
        colors.pressedColor = Hex("E88AA8");
        colors.selectedColor = Pink;
        button.colors = colors;

        TMP_Text text = CreateText("Label", image.transform, label, 34f, DeepBlue, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, 10f);
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static UnityEngine.UI.Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        UnityEngine.UI.Image image = gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.font = TMP_Settings.defaultFontAsset != null
            ? TMP_Settings.defaultFontAsset
            : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return text;
    }

    private static void AddOutline(GameObject gameObject, Color color, float distance)
    {
        UnityEngine.UI.Outline outline = gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
        outline.useGraphicAlpha = true;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetTopCenter(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetBottomCenter(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void EnsureEventSystem(Scene scene)
    {
        EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        if (systems.Length > 0)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        SceneManager.MoveGameObjectToScene(eventSystem, scene);
    }

    private static void RemoveSceneObject(Scene scene, string name)
    {
        GameObject existing = FindSceneObject(scene, name);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException($"No se pudo cargar el sprite {path}.");
        return sprite;
    }

    private static T FindSceneComponent<T>(Scene scene, string objectName) where T : Component
    {
        GameObject gameObject = FindSceneObject(scene, objectName);
        return gameObject != null ? gameObject.GetComponent<T>() : null;
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindRecursive(root.transform, name);
            if (match != null)
                return match.gameObject;
        }
        return null;
    }

    private static Transform FindRecursive(Transform current, string name)
    {
        if (string.Equals(current.name, name, StringComparison.Ordinal))
            return current;
        for (int i = 0; i < current.childCount; i++)
        {
            Transform match = FindRecursive(current.GetChild(i), name);
            if (match != null)
                return match;
        }
        return null;
    }

    private static void RequireNamed(Scene scene, string name, List<string> errors)
    {
        if (FindSceneObject(scene, name) == null)
            errors.Add($"Falta {name}.");
    }

    private static void RequireComponent<T>(Scene scene, string label, List<string> errors) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<T>(true) != null)
                return;
        }
        errors.Add($"Falta {label}.");
    }

    private static Color Hex(string rgb)
    {
        if (!ColorUtility.TryParseHtmlString("#" + rgb, out Color color))
            throw new InvalidOperationException($"Color inválido: {rgb}");
        return color;
    }
}
#endif
