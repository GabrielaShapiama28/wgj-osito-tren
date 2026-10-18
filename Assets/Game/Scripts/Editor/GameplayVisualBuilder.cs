#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameplayVisualBuilder
{
    // Construcción idempotente: solo regenera los tres grupos visuales de Gameplay.
    private const string ScenePath = "Assets/Game/Scenes/Gameplay.unity";
    private const string AssetRoot = "Assets/Game/Sprites/Gameplay";
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const float BackgroundSegmentWidth = 3840f;
    private const int BackgroundRepeatCount = 3;
    private const string VisualRevisionMarker = "GameplayVisualRevision_3";

    private static readonly string[] BackgroundFiles =
    {
        "fondoamarillo.png",
        "fondocherry.png",
        "fondociudad.png",
        "fondoazul.png"
    };

    [InitializeOnLoadMethod]
    private static void BuildOnceWhenGameplayIsOpen()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling
                || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene activeScene = SceneManager.GetActiveScene();
            bool isGameplayScene = activeScene.IsValid()
                && (string.Equals(activeScene.path, ScenePath, StringComparison.Ordinal)
                    || string.Equals(activeScene.name, "Gameplay", StringComparison.Ordinal));
            if (!activeScene.IsValid()
                || !isGameplayScene
                || FindSceneObject(activeScene, VisualRevisionMarker) != null)
                return;

            try
            {
                BuildGameplayVisuals();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("Bearly in Time/Rebuild Gameplay Visuals")]
    public static void BuildGameplayVisuals()
    {
        ConfigureSpriteImports();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = FindSceneComponent<Canvas>(scene, "Canvas");
        if (canvas == null)
            throw new InvalidOperationException("Gameplay no contiene el Canvas esperado.");

        ConfigureCanvas(canvas);
        DisableLegacyVisuals(scene);
        RemovePreviousGeneratedVisuals(scene);

        RectTransform canvasRect = (RectTransform)canvas.transform;
        BuildScrollingBackground(canvasRect, scene);
        BuildGameplayForeground(canvasRect);
        BuildLeftSidebar(canvasRect, scene);
        GameObject revisionMarker = CreateUiObject(VisualRevisionMarker, FindSceneObject(scene, "LeftSidebar").transform);
        revisionMarker.SetActive(false);
        KeepExistingGameplayUiOnTop(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unity no pudo guardar Gameplay.unity.");

        AssetDatabase.SaveAssets();
        Debug.Log("[Bearly in Time] Gameplay visual construido correctamente.");
    }

    [MenuItem("Bearly in Time/Validate Gameplay Visuals")]
    public static void ValidateGameplayVisuals()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        List<string> errors = new List<string>();

        ValidateGeneratedRoot(scene, "ScrollingBackground", errors);
        ValidateGeneratedRoot(scene, "GameplayForeground", errors);
        ValidateGeneratedRoot(scene, "LeftSidebar", errors);

        GameObject scrolling = FindSceneObject(scene, "ScrollingBackground");
        GameplayBackgroundScroller scroller = scrolling != null
            ? scrolling.GetComponent<GameplayBackgroundScroller>()
            : null;
        if (scroller == null)
            errors.Add("ScrollingBackground no tiene GameplayBackgroundScroller.");

        if (scrolling != null)
        {
            Transform track = scrolling.transform.Find("BackgroundTrack");
            int expectedCount = BackgroundFiles.Length * BackgroundRepeatCount;
            if (track == null || track.childCount != expectedCount)
                errors.Add($"BackgroundTrack no contiene los {expectedCount} tramos esperados.");
            else
            {
                int childIndex = 0;
                for (int backgroundIndex = 0; backgroundIndex < BackgroundFiles.Length; backgroundIndex++)
                {
                    string baseName = Path.GetFileNameWithoutExtension(BackgroundFiles[backgroundIndex]);
                    for (int repeatIndex = 0; repeatIndex < BackgroundRepeatCount; repeatIndex++)
                    {
                        string expected = $"{baseName}_{repeatIndex + 1:00}";
                        if (!string.Equals(track.GetChild(childIndex).name, expected, StringComparison.Ordinal))
                            errors.Add($"Tramo {childIndex + 1}: se esperaba {expected}.");
                        childIndex++;
                    }
                }
            }
        }

        GameObject clockObject = FindSceneObject(scene, "Clock");
        if (clockObject == null || clockObject.GetComponent<GameplayClockHandsUI>() == null)
            errors.Add("El reloj lateral no tiene GameplayClockHandsUI.");

        foreach (string png in Directory.GetFiles(AssetRoot, "*.png", SearchOption.AllDirectories))
        {
            string normalized = png.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(normalized) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite)
                errors.Add($"No está importado como Sprite: {normalized}");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", errors));

        Debug.Log("[Bearly in Time] Validación visual de Gameplay superada: 18 sprites, 12 tramos ordenados, reloj animado y 3 grupos visuales conectados.");
    }

    private static void ConfigureSpriteImports()
    {
        string[] pngFiles = Directory.GetFiles(AssetRoot, "*.png", SearchOption.AllDirectories);
        if (pngFiles.Length != 18)
            throw new InvalidOperationException($"Se esperaban 18 PNG y se encontraron {pngFiles.Length}.");

        foreach (string file in pngFiles)
        {
            string assetPath = file.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency
                || importer.mipmapEnabled
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.maxTextureSize != 4096
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.filterMode != FilterMode.Bilinear;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            if (changed)
                importer.SaveAndReimport();
        }
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void DisableLegacyVisuals(Scene scene)
    {
        GameObject legacyBackground = FindSceneObject(scene, "Backgrounds");
        if (legacyBackground != null)
            legacyBackground.SetActive(false);

        GameObject placeholder = FindSceneObject(scene, "GameplayPlaceholder");
        if (placeholder != null)
            placeholder.SetActive(false);

        GameObject hud = FindSceneObject(scene, "HUD");
        if (hud != null)
        {
            foreach (Graphic graphic in hud.GetComponentsInChildren<Graphic>(true))
                graphic.gameObject.SetActive(false);
        }
    }

    private static void RemovePreviousGeneratedVisuals(Scene scene)
    {
        string[] names = { "ScrollingBackground", "GameplayForeground", "LeftSidebar" };
        foreach (string name in names)
        {
            GameObject existing = FindSceneObject(scene, name);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing);
        }
    }

    private static void BuildScrollingBackground(RectTransform canvasRect, Scene scene)
    {
        GameObject root = CreateUiObject("ScrollingBackground", canvasRect);
        StretchToParent((RectTransform)root.transform);
        root.transform.SetAsFirstSibling();

        RectMask2D mask = root.AddComponent<RectMask2D>();
        mask.padding = Vector4.zero;

        GameObject trackObject = CreateUiObject("BackgroundTrack", root.transform);
        RectTransform track = (RectTransform)trackObject.transform;
        track.anchorMin = new Vector2(0f, 0.5f);
        track.anchorMax = new Vector2(0f, 0.5f);
        track.pivot = new Vector2(0f, 0.5f);
        track.anchoredPosition = Vector2.zero;
        int segmentCount = BackgroundFiles.Length * BackgroundRepeatCount;
        track.sizeDelta = new Vector2(segmentCount * BackgroundSegmentWidth, ReferenceHeight);

        int segmentIndex = 0;
        for (int backgroundIndex = 0; backgroundIndex < BackgroundFiles.Length; backgroundIndex++)
        {
            Sprite sprite = LoadSprite($"{AssetRoot}/{BackgroundFiles[backgroundIndex]}");
            string baseName = Path.GetFileNameWithoutExtension(BackgroundFiles[backgroundIndex]);
            for (int repeatIndex = 0; repeatIndex < BackgroundRepeatCount; repeatIndex++)
            {
                Image image = CreateImage(
                    $"{baseName}_{repeatIndex + 1:00}",
                    track,
                    sprite);
                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(segmentIndex * BackgroundSegmentWidth, 0f);
                rect.sizeDelta = new Vector2(BackgroundSegmentWidth, ReferenceHeight);
                image.preserveAspect = true;
                segmentIndex++;
            }
        }

        GameClock clock = FindSceneComponent<GameClock>(scene, "GameSystems");
        GameplayBackgroundScroller scroller = root.AddComponent<GameplayBackgroundScroller>();
        scroller.Configure(track, clock, 0f, ReferenceWidth - track.sizeDelta.x);
    }

    private static void BuildGameplayForeground(RectTransform canvasRect)
    {
        GameObject root = CreateUiObject("GameplayForeground", canvasRect);
        StretchToParent((RectTransform)root.transform);

        Sprite sprite = LoadSprite($"{AssetRoot}/oso-pantalla-game-main.png");
        Image image = CreateImage("OsoPantallaGameMain", root.transform, sprite);
        RectTransform rect = image.rectTransform;
        StretchToParent(rect);
        image.preserveAspect = true;

        AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
    }

    private static void BuildLeftSidebar(RectTransform canvasRect, Scene scene)
    {
        GameObject root = CreateUiObject("LeftSidebar", canvasRect);
        StretchToParent((RectTransform)root.transform);

        Sprite baseSprite = LoadSidebarSprite("SOLAPA IZQ.png");
        float sidebarTileHeight = ReferenceHeight * 0.5f;
        float sidebarTileWidth = sidebarTileHeight * (baseSprite.rect.width / baseSprite.rect.height);
        for (int tileIndex = 0; tileIndex < 2; tileIndex++)
        {
            Image baseTile = CreateImage($"SidebarBase_{tileIndex + 1:00}", root.transform, baseSprite);
            SetTopLeftRect(
                baseTile.rectTransform,
                new Vector2(0f, -tileIndex * sidebarTileHeight),
                new Vector2(sidebarTileWidth, sidebarTileHeight),
                new Vector2(0f, 1f));
            baseTile.preserveAspect = true;
        }

        RectTransform content = (RectTransform)CreateUiObject("SidebarContent", root.transform).transform;
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(480f, 0f);

        GameClock gameClock = FindSceneComponent<GameClock>(scene, "GameSystems");
        BuildClock(content, gameClock);
        BuildMomStatus(content);
        BuildPlayerStatus(content);
    }

    private static void BuildClock(RectTransform parent, GameClock gameClock)
    {
        RectTransform clock = (RectTransform)CreateUiObject("Clock", parent).transform;
        SetTopLeftRect(clock, new Vector2(240f, -155f), new Vector2(220f, 220f), new Vector2(0.5f, 0.5f));

        Image face = CreateImage("ClockFace", clock, LoadSidebarSprite("RELOJ FONDO.png"));
        SetCenteredRect(face.rectTransform, Vector2.zero, new Vector2(205f, 205f));
        face.preserveAspect = true;

        Image hours = CreateImage("HoursHand", clock, LoadSidebarSprite("Horas.png"));
        RectTransform hoursRect = hours.rectTransform;
        hoursRect.anchorMin = hoursRect.anchorMax = new Vector2(0.5f, 0.5f);
        hoursRect.pivot = new Vector2(0f, 0.5f);
        hoursRect.anchoredPosition = Vector2.zero;
        hoursRect.sizeDelta = new Vector2(45.5f, 10.7f);
        hours.preserveAspect = true;

        Image minutes = CreateImage("MinutesHand", clock, LoadSidebarSprite("Minutos.png"));
        RectTransform minutesRect = minutes.rectTransform;
        minutesRect.anchorMin = minutesRect.anchorMax = new Vector2(0.5f, 0.5f);
        minutesRect.pivot = new Vector2(0.5f, 0f);
        minutesRect.anchoredPosition = Vector2.zero;
        minutesRect.sizeDelta = new Vector2(10f, 69.5f);
        minutes.preserveAspect = true;

        Image reference = CreateImage("ClockReference", clock, LoadSidebarSprite("RELOJ TODO.png"));
        SetCenteredRect(reference.rectTransform, Vector2.zero, new Vector2(205f, 205f));
        reference.preserveAspect = true;
        reference.gameObject.SetActive(false);

        GameplayClockHandsUI clockHands = clock.gameObject.AddComponent<GameplayClockHandsUI>();
        clockHands.Configure(hoursRect, minutesRect, gameClock);
    }

    private static void BuildMomStatus(RectTransform parent)
    {
        RectTransform section = (RectTransform)CreateUiObject("MomStatus", parent).transform;
        SetTopLeftRect(section, new Vector2(240f, -500f), new Vector2(430f, 220f), new Vector2(0.5f, 0.5f));

        Image title = CreateImage("MomLabel", section, LoadSidebarSprite("Mamá Oso Texto.png"));
        SetCenteredRect(title.rectTransform, new Vector2(0f, 82f), new Vector2(150f, 31f));
        title.preserveAspect = true;

        RectTransform moods = (RectTransform)CreateUiObject("MoodVariants", section).transform;
        SetCenteredRect(moods, new Vector2(-115f, -20f), new Vector2(130f, 134f));

        Image happy = CreateImage("MamaHappy", moods, LoadSidebarSprite("Mamá feliz.png"));
        StretchToParent(happy.rectTransform);
        happy.preserveAspect = true;

        Image serious = CreateImage("MamaSerious", moods, LoadSidebarSprite("Mamá seria.png"));
        StretchToParent(serious.rectTransform);
        serious.preserveAspect = true;
        serious.gameObject.SetActive(false);

        Image sad = CreateImage("MamaSad", moods, LoadSidebarSprite("Mamá triste.png"));
        StretchToParent(sad.rectTransform);
        sad.preserveAspect = true;
        sad.gameObject.SetActive(false);

        BuildLayeredBar(section, new Vector2(82f, -20f));
    }

    private static void BuildPlayerStatus(RectTransform parent)
    {
        RectTransform section = (RectTransform)CreateUiObject("PlayerStatus", parent).transform;
        SetTopLeftRect(section, new Vector2(240f, -835f), new Vector2(430f, 220f), new Vector2(0.5f, 0.5f));

        Image title = CreateImage("PlayerLabel", section, LoadSidebarSprite("Yo Texto.png"));
        SetCenteredRect(title.rectTransform, new Vector2(0f, 82f), new Vector2(62f, 43f));
        title.preserveAspect = true;

        Image player = CreateImage("PlayerPortrait", section, LoadSidebarSprite("YO.png"));
        SetCenteredRect(player.rectTransform, new Vector2(-115f, -20f), new Vector2(130f, 134f));
        player.preserveAspect = true;

        BuildLayeredBar(section, new Vector2(82f, -20f));
    }

    private static void BuildLayeredBar(RectTransform parent, Vector2 position)
    {
        RectTransform bar = (RectTransform)CreateUiObject("StatusBar", parent).transform;
        SetCenteredRect(bar, position, new Vector2(240f, 63f));

        Image blue = CreateImage("BlueBackground", bar, LoadSidebarSprite("BARRA AZUL DE FONDO.png"));
        StretchToParent(blue.rectTransform);
        blue.preserveAspect = true;

        Image yellow = CreateImage("YellowFill", bar, LoadSidebarSprite("BARRA AMARILLA.png"));
        SetCenteredRect(yellow.rectTransform, Vector2.zero, new Vector2(221f, 42f));
        yellow.preserveAspect = true;
    }

    private static void KeepExistingGameplayUiOnTop(Scene scene)
    {
        GameObject gameplayUi = FindSceneObject(scene, "MVPGameplayUI");
        if (gameplayUi != null && gameplayUi.transform.parent != null)
            gameplayUi.transform.SetAsLastSibling();
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject gameObject = CreateUiObject(name, parent);
        Image image = gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        return image;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void SetTopLeftRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static Sprite LoadSidebarSprite(string fileName)
    {
        return LoadSprite($"{AssetRoot}/barra-lateral/{fileName}");
    }

    private static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
            throw new InvalidOperationException($"No se pudo cargar el sprite: {assetPath}");
        return sprite;
    }

    private static T FindSceneComponent<T>(Scene scene, string gameObjectName) where T : Component
    {
        GameObject gameObject = FindSceneObject(scene, gameObjectName);
        return gameObject != null ? gameObject.GetComponent<T>() : null;
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindTransformRecursive(root.transform, name);
            if (match != null)
                return match.gameObject;
        }

        return null;
    }

    private static Transform FindTransformRecursive(Transform current, string name)
    {
        if (string.Equals(current.name, name, StringComparison.Ordinal))
            return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform match = FindTransformRecursive(current.GetChild(i), name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static void ValidateGeneratedRoot(Scene scene, string name, List<string> errors)
    {
        GameObject gameObject = FindSceneObject(scene, name);
        if (gameObject == null)
            errors.Add($"Falta {name}.");
        else if (gameObject.transform.parent == null || gameObject.transform.parent.name != "Canvas")
            errors.Add($"{name} no está organizado bajo Canvas.");
    }
}
#endif
