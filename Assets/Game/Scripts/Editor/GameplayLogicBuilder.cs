#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class GameplayLogicBuilder
{
    private const string GameplayScenePath = "Assets/Game/Scenes/Gameplay.unity";
    private const string BirdScenePath = "Assets/Game/Scenes/BirdPhotoMinigame.unity";
    private const string AssetRoot = "Assets/Game/Sprites/GameplayLogic";
    private const string RevisionMarker = "GameplayLogicRevision_3";
    private const string SessionKey = "BearlyInTime.GameplayLogicBuilt3";
    private static readonly Color DeepBlue = Hex("2E6590");
    private static readonly Color Pink = Hex("F5A9C0");
    private static readonly Color Cream = Hex("FFF6DE");

    //[InitializeOnLoadMethod]
    private static void QueueAutomaticBuild()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EditorApplication.delayCall += TryAutomaticBuild;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += TryAutomaticBuild;
    }

    private static void TryAutomaticBuild()
    {
        if (SessionState.GetBool(SessionKey, false)
            || EditorApplication.isCompiling
            || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        try
        {
            BuildGameplaySystems();
            SessionState.SetBool(SessionKey, true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Bearly in Time/Rebuild Gameplay Logic")]
    public static void BuildGameplaySystems()
    {
        ConfigureSpriteImports();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            BuildGameplayScene();
        }
        finally
        {
            if (previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        AssetDatabase.SaveAssets();
        SessionState.SetBool(SessionKey, true);
        Debug.Log("[Bearly in Time] Lógica de Gameplay, pájaros, barras, final y fotografía conectada.");
    }

    [MenuItem("Bearly in Time/Validate Gameplay Logic")]
    public static void ValidateGameplayLogic()
    {
        List<string> errors = new List<string>();
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene gameplay = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            Require<GameplayEventController>(gameplay, errors);
            Require<GameplayStatusUI>(gameplay, errors);
            Require<GameplayFinalUI>(gameplay, errors);
            RequireNamed(gameplay, "BirdButton", errors);
            RequireNamed(gameplay, "KingBirdAnnouncement", errors);
            RequireNamed(gameplay, "FinalPanel", errors);
            RequireNamed(gameplay, RevisionMarker, errors);
            if (FindNamed(gameplay, "SelfLabel") != null)
                errors.Add("Gameplay: todavía existe el texto Relación Yo.");

            Scene bird = EditorSceneManager.OpenScene(BirdScenePath, OpenSceneMode.Single);
            Require<PointerController>(bird, errors);
            Require<WinPopupAnimation>(bird, errors);
            RequireNamed(bird, "BirdVisual", errors);
            RequireNamed(bird, "Safe Zone", errors);
            RequireNamed(bird, "PhotoTimerText", errors);
            RequireNamed(bird, "BirdPhotoIntegrationRevision_1", errors);
        }
        finally
        {
            if (previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", errors));

        Debug.Log("[Bearly in Time] Gameplay validado: intervalos, barras, pájaros, fotografía y final conectados.");
    }

    private static void BuildGameplayScene()
    {
        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        Canvas canvas = FindComponent<Canvas>(scene);
        GameplayEventController controller = FindComponent<GameplayEventController>(scene);
        EventPopupUI popup = FindComponent<EventPopupUI>(scene);
        if (canvas == null || controller == null || popup == null)
            throw new InvalidOperationException("Gameplay necesita Canvas, GameplayEventController y EventPopupUI existentes.");

        RemoveNamed(scene, "GameplayBirdLayer");
        RemoveNamed(scene, "KingBirdAnnouncement");
        RemoveNamed(scene, "GameplayLogicOverlay");
        RemoveNamed(scene, RevisionMarker);

        RectTransform canvasRect = (RectTransform)canvas.transform;
        BuildBirdLayer(canvasRect, controller, out GameObject birdRoot, out UnityEngine.UI.Image birdImage, out TMP_Text birdTimer);
        KingBirdAnnouncementUI kingAnnouncement = BuildKingBirdAnnouncement(canvasRect);
        BuildStatusUI(scene);
        GameplayFinalUI finalUI = BuildFinalOverlay(canvasRect);

        Sprite[] normalBirds = LoadNormalBirdSprites();
        Sprite king = LoadSprite($"{AssetRoot}/pajaros/PajaroRey.PNG");
        controller.Configure(popup, birdRoot, birdImage, birdTimer, normalBirds, king, finalUI);
        controller.ConfigureKingBirdAnnouncement(kingAnnouncement);
        controller.ConfigureIntervals(60f, 20f, 5f);

        GameObject marker = new GameObject(RevisionMarker);
        marker.transform.SetParent(canvas.transform, false);
        marker.SetActive(false);

        EnsureSiblingOrder(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, GameplayScenePath))
            throw new InvalidOperationException("No se pudo guardar Gameplay.unity.");
    }

    private static void BuildBirdLayer(
        RectTransform canvas,
        GameplayEventController controller,
        out GameObject birdRoot,
        out UnityEngine.UI.Image birdImage,
        out TMP_Text birdTimer)
    {
        GameObject layer = CreateUiObject("GameplayBirdLayer", canvas);
        Stretch(layer.GetComponent<RectTransform>());

        birdRoot = CreateUiObject("BirdButton", layer.transform);
        RectTransform birdRect = birdRoot.GetComponent<RectTransform>();
        // Parte despejada de la ventana: a la derecha del oso y lejos de la barra lateral.
        birdRect.anchorMin = birdRect.anchorMax = new Vector2(0.82f, 0.62f);
        birdRect.pivot = new Vector2(0.5f, 0.5f);
        birdRect.anchoredPosition = Vector2.zero;
        birdRect.sizeDelta = new Vector2(270f, 235f);

        birdImage = birdRoot.AddComponent<UnityEngine.UI.Image>();
        birdImage.preserveAspect = true;
        birdImage.raycastTarget = true;
        UnityEngine.UI.Button button = birdRoot.AddComponent<UnityEngine.UI.Button>();
        button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        UnityEventTools.AddPersistentListener(button.onClick, controller.PhotographVisibleBird);

        birdTimer = CreateText("BirdVisibleTimer", birdRoot.transform, "5 s", 28f, Color.white, TextAlignmentOptions.Center);
        SetBottomCenter(birdTimer.rectTransform, new Vector2(0f, -12f), new Vector2(110f, 42f));
        AddOutline(birdTimer.gameObject, DeepBlue, 2f);
        birdRoot.SetActive(false);
    }

    private static KingBirdAnnouncementUI BuildKingBirdAnnouncement(RectTransform canvas)
    {
        GameObject root = CreateUiObject("KingBirdAnnouncement", canvas);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.77f, 0.88f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(610f, 112f);

        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        UnityEngine.UI.Image glow = root.AddComponent<UnityEngine.UI.Image>();
        glow.color = new Color32(255, 221, 92, 235);
        glow.raycastTarget = false;
        UnityEngine.UI.Outline whiteBorder = root.AddComponent<UnityEngine.UI.Outline>();
        whiteBorder.effectColor = Color.white;
        whiteBorder.effectDistance = new Vector2(5f, -5f);

        TMP_Text message = CreateText(
            "KingBirdMessage",
            root.transform,
            "¡ES EL PÁJARO REY!",
            41f,
            DeepBlue,
            TextAlignmentOptions.Center);
        StretchInset(message.rectTransform, 12f);
        message.fontStyle = FontStyles.Bold;
        AddOutline(message.gameObject, Color.white, 2f);

        KingBirdAnnouncementUI announcement = root.AddComponent<KingBirdAnnouncementUI>();
        announcement.Configure(group, rootRect, glow, message);
        root.SetActive(false);
        return announcement;
    }

    private static void BuildStatusUI(Scene scene)
    {
        GameObject sidebar = FindNamed(scene, "LeftSidebar");
        GameObject mom = FindNamed(scene, "MomStatus");
        GameObject player = FindNamed(scene, "PlayerStatus");
        if (sidebar == null || mom == null || player == null)
            throw new InvalidOperationException("Gameplay necesita la barra lateral visual existente.");

        GameplayStatusUI old = sidebar.GetComponent<GameplayStatusUI>();
        if (old != null)
            UnityEngine.Object.DestroyImmediate(old);

        RemoveChildNamed(mom.transform, "StatusLogicLabels");
        RemoveChildNamed(player.transform, "StatusLogicLabels");
        RemoveChildNamed(player.transform, "SelfStatusBar");

        UnityEngine.UI.Image momFill = FindImage(mom.transform, "YellowFill");
        UnityEngine.UI.Image selfFill = FindImage(player.transform, "YellowFill");
        if (momFill == null || selfFill == null)
            throw new InvalidOperationException("No se encontraron las barras amarillas existentes.");

        ConfigureFill(momFill);
        ConfigureFill(selfFill);
        GameObject momLabels = CreateUiObject("StatusLogicLabels", mom.transform);
        Stretch(momLabels.GetComponent<RectTransform>());
        TMP_Text momValue = CreateBarValueLabel(momFill.transform.parent, "MotherValue");
        AddThresholdLabels(momLabels.transform, new Vector2(82f, -61f));

        GameObject playerLabels = CreateUiObject("StatusLogicLabels", player.transform);
        Stretch(playerLabels.GetComponent<RectTransform>());
        TMP_Text selfValue = CreateBarValueLabel(selfFill.transform.parent, "SelfValue");
        AddThresholdLabels(playerLabels.transform, new Vector2(82f, -36f));

        GameplayStatusUI status = sidebar.AddComponent<GameplayStatusUI>();
        status.Configure(
            momFill,
            selfFill,
            momValue,
            selfValue,
            FindNamed(scene, "MamaHappy"),
            FindNamed(scene, "MamaSerious"),
            FindNamed(scene, "MamaSad"));
    }

    private static GameplayFinalUI BuildFinalOverlay(RectTransform canvas)
    {
        GameObject overlay = CreateUiObject("GameplayLogicOverlay", canvas);
        Stretch(overlay.GetComponent<RectTransform>());
        GameplayFinalUI finalUI = overlay.AddComponent<GameplayFinalUI>();

        GameObject panel = CreateUiObject("FinalPanel", overlay.transform);
        Stretch(panel.GetComponent<RectTransform>());
        UnityEngine.UI.Image dim = panel.AddComponent<UnityEngine.UI.Image>();
        dim.color = new Color(0.05f, 0.10f, 0.18f, 0.92f);

        UnityEngine.UI.Image resultImage = CreateSpriteImage("FinalArtwork", panel.transform, null);
        SetCentered(resultImage.rectTransform, new Vector2(0f, 45f), new Vector2(1420f, 800f));
        resultImage.preserveAspect = true;

        TMP_Text details = CreateText("FinalConditions", panel.transform, string.Empty, 27f, Color.white, TextAlignmentOptions.Center);
        SetBottomCenter(details.rectTransform, new Vector2(0f, 146f), new Vector2(1250f, 86f));
        details.textWrappingMode = TextWrappingModes.Normal;
        AddOutline(details.gameObject, DeepBlue, 2f);

        UnityEngine.UI.Button restart = CreateButton("RestartButton", panel.transform, "Reiniciar");
        SetBottomCenter((RectTransform)restart.transform, new Vector2(-180f, 45f), new Vector2(290f, 76f));
        UnityEngine.UI.Button menu = CreateButton("MainMenuButton", panel.transform, "Menú Principal");
        SetBottomCenter((RectTransform)menu.transform, new Vector2(180f, 45f), new Vector2(320f, 76f));

        finalUI.Configure(
            panel,
            resultImage,
            LoadSprite($"{AssetRoot}/final/ganaste.PNG"),
            LoadSprite($"{AssetRoot}/final/perdiste.PNG"),
            details,
            restart,
            menu);
        panel.SetActive(false);
        return finalUI;
    }

    private static void BuildBirdPhotoScene()
    {
        Scene scene = EditorSceneManager.OpenScene(BirdScenePath, OpenSceneMode.Single);
        Canvas canvas = FindComponent<Canvas>(scene);
        if (canvas == null)
            throw new InvalidOperationException("BirdPhotoMinigame necesita Canvas.");

        RemoveNamed(scene, "Card");
        RemoveNamed(scene, "BirdPhotoRuntime");
        MinigamePlaceholderController placeholder = FindComponent<MinigamePlaceholderController>(scene);
        if (placeholder != null)
            UnityEngine.Object.DestroyImmediate(placeholder);

        GameObject root = CreateUiObject("BirdPhotoRuntime", canvas.transform);
        Stretch(root.GetComponent<RectTransform>());
        UnityEngine.UI.Image background = root.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color32(220, 239, 252, 255);

        BirdPhotoMinigameController controller = root.AddComponent<BirdPhotoMinigameController>();
        TMP_Text title = CreateText("Title", root.transform, "Fotografía de aves", 52f, DeepBlue, TextAlignmentOptions.Center);
        SetTopCenter(title.rectTransform, new Vector2(0f, -34f), new Vector2(800f, 78f));

        UnityEngine.UI.Image bird = CreateSpriteImage("PhotographedBird", root.transform, null);
        SetCentered(bird.rectTransform, new Vector2(0f, 70f), new Vector2(620f, 500f));
        bird.preserveAspect = true;

        TMP_Text timer = CreateText("PhotoTimer", root.transform, "Tiempo para la foto: 10", 38f, DeepBlue, TextAlignmentOptions.Center);
        SetTopCenter(timer.rectTransform, new Vector2(0f, -120f), new Vector2(600f, 62f));
        TMP_Text result = CreateText("PhotoResult", root.transform, "Encuadra al pájaro", 32f, DeepBlue, TextAlignmentOptions.Center);
        SetBottomCenter(result.rectTransform, new Vector2(0f, 220f), new Vector2(760f, 70f));

        GameObject decisions = CreateUiObject("PhotoDecisionButtons", root.transform);
        RectTransform decisionRect = decisions.GetComponent<RectTransform>();
        SetBottomCenter(decisionRect, new Vector2(0f, 96f), new Vector2(700f, 80f));
        UnityEngine.UI.Button success = CreateButton("TestSuccessButton", decisions.transform, "Éxito (prueba)");
        SetCentered(success.GetComponent<RectTransform>(), new Vector2(-180f, 0f), new Vector2(300f, 72f));
        UnityEngine.UI.Button failure = CreateButton("TestFailureButton", decisions.transform, "Fallo (prueba)");
        SetCentered(failure.GetComponent<RectTransform>(), new Vector2(180f, 0f), new Vector2(300f, 72f));
        UnityEventTools.AddPersistentListener(success.onClick, controller.TestSuccess);
        UnityEventTools.AddPersistentListener(failure.onClick, controller.TestFailure);

        UnityEngine.UI.Button continueButton = CreateButton("PhotoContinueButton", root.transform, "Continuar");
        SetBottomCenter((RectTransform)continueButton.transform, new Vector2(0f, 82f), new Vector2(300f, 76f));
        continueButton.gameObject.SetActive(false);
        controller.Configure(bird, timer, result, decisions, continueButton);

        EnsureEventSystem(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, BirdScenePath))
            throw new InvalidOperationException("No se pudo guardar BirdPhotoMinigame.unity.");
    }

    private static void ConfigureSpriteImports()
    {
        foreach (string file in Directory.GetFiles(AssetRoot, "*.PNG", SearchOption.AllDirectories))
        {
            string path = file.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }
    }

    private static Sprite[] LoadNormalBirdSprites()
    {
        List<Sprite> sprites = new List<Sprite>();
        for (int index = 1593; index <= 1601; index++)
            sprites.Add(LoadSprite($"{AssetRoot}/pajaros/IMG_{index}.PNG"));
        return sprites.ToArray();
    }

    private static void EnsureSiblingOrder(Scene scene)
    {
        GameObject bird = FindNamed(scene, "GameplayBirdLayer");
        GameObject foreground = FindNamed(scene, "GameplayForeground");
        GameObject kingAnnouncement = FindNamed(scene, "KingBirdAnnouncement");
        GameObject overlay = FindNamed(scene, "GameplayLogicOverlay");
        GameObject popup = FindNamed(scene, "MVPGameplayUI");
        if (bird != null && foreground != null)
            bird.transform.SetSiblingIndex(foreground.transform.GetSiblingIndex());
        if (kingAnnouncement != null && foreground != null)
            kingAnnouncement.transform.SetSiblingIndex(foreground.transform.GetSiblingIndex() + 1);
        popup?.transform.SetAsLastSibling();
        overlay?.transform.SetAsLastSibling();
    }

    private static TMP_Text CreateBarValueLabel(Transform parent, string name)
    {
        Transform previous = parent.Find(name);
        if (previous != null)
            UnityEngine.Object.DestroyImmediate(previous.gameObject);

        TMP_Text text = CreateText(name, parent, "10/15", 21f, DeepBlue, TextAlignmentOptions.Center);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.offsetMin = new Vector2(8f, 4f);
        text.rectTransform.offsetMax = new Vector2(-8f, -4f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 21f;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.fontStyle = FontStyles.Bold;
        text.transform.SetAsLastSibling();
        return text;
    }

    private static void AddThresholdLabels(Transform parent, Vector2 position)
    {
        TMP_Text text = CreateText("Thresholds", parent, "0      5      10      15", 15f, DeepBlue, TextAlignmentOptions.Center);
        SetCentered(text.rectTransform, position, new Vector2(250f, 24f));
    }

    private static void ConfigureFill(UnityEngine.UI.Image image)
    {
        image.type = UnityEngine.UI.Image.Type.Filled;
        image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        image.fillOrigin = 0;
        image.fillAmount = 10f / 15f;
    }

    private static UnityEngine.UI.Image FindImage(Transform root, string name)
    {
        Transform match = FindRecursive(root, name);
        return match != null ? match.GetComponent<UnityEngine.UI.Image>() : null;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException($"No se pudo cargar {path} como Sprite.");
        return sprite;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static UnityEngine.UI.Image CreateSpriteImage(string name, Transform parent, Sprite sprite)
    {
        GameObject gameObject = CreateUiObject(name, parent);
        UnityEngine.UI.Image image = gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject gameObject = CreateUiObject(name, parent);
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.font = TMP_Settings.defaultFontAsset != null
            ? TMP_Settings.defaultFontAsset
            : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return text;
    }

    private static UnityEngine.UI.Button CreateButton(string name, Transform parent, string label)
    {
        GameObject gameObject = CreateUiObject(name, parent);
        UnityEngine.UI.Image image = gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = Pink;
        UnityEngine.UI.Outline outline = gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(4f, -4f);
        UnityEngine.UI.Button button = gameObject.AddComponent<UnityEngine.UI.Button>();
        TMP_Text text = CreateText("Label", gameObject.transform, label, 28f, DeepBlue, TextAlignmentOptions.Center);
        StretchInset(text.rectTransform, 8f);
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static void AddOutline(GameObject gameObject, Color color, float distance)
    {
        UnityEngine.UI.Outline outline = gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StretchInset(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }

    private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
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

    private static void EnsureEventSystem(Scene scene)
    {
        if (FindComponent<EventSystem>(scene) != null)
            return;
        GameObject system = new GameObject("EventSystem");
        system.AddComponent<EventSystem>();
        system.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        SceneManager.MoveGameObjectToScene(system, scene);
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }

    private static GameObject FindNamed(Scene scene, string name)
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
        for (int index = 0; index < current.childCount; index++)
        {
            Transform result = FindRecursive(current.GetChild(index), name);
            if (result != null)
                return result;
        }
        return null;
    }

    private static void RemoveNamed(Scene scene, string name)
    {
        GameObject gameObject = FindNamed(scene, name);
        if (gameObject != null)
            UnityEngine.Object.DestroyImmediate(gameObject);
    }

    private static void RemoveChildNamed(Transform root, string name)
    {
        Transform child = root.Find(name);
        if (child != null)
            UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static void Require<T>(Scene scene, List<string> errors) where T : Component
    {
        if (FindComponent<T>(scene) == null)
            errors.Add($"{scene.name}: falta {typeof(T).Name}.");
    }

    private static void RequireNamed(Scene scene, string name, List<string> errors)
    {
        if (FindNamed(scene, name) == null)
            errors.Add($"{scene.name}: falta {name}.");
    }

    private static Color Hex(string rgb)
    {
        if (!ColorUtility.TryParseHtmlString("#" + rgb, out Color color))
            throw new InvalidOperationException($"Color inválido: {rgb}");
        return color;
    }
}
#endif
