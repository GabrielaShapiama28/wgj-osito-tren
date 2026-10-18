#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace BearlyInTime.Editor
{
    public static class MvpSceneBuilder
    {
        private const string SceneFolder = "Assets/Game/Scenes";
        private static readonly Color PastelCream = Hex("FFF6E9");
        private static readonly Color PastelBlue = Hex("BDE3F2");
        private static readonly Color DeepBlue = Hex("2E6590");
        private static readonly Color Pink = Hex("F5A9BD");
        private static readonly Color DarkPink = Hex("E889A6");
        private static readonly Color White = Color.white;

        [InitializeOnLoadMethod]
        private static void QueueFirstBuildInOpenEditor()
        {
            if (Application.isBatchMode || File.Exists(ScenePath("MainMenu")))
                return;

            EditorApplication.delayCall += TryAutoBuild;
        }

        private static void TryAutoBuild()
        {
            if (File.Exists(ScenePath("MainMenu")))
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoBuild;
                return;
            }

            EditorSceneManager.SaveOpenScenes();
            BuildAll();
        }

        [MenuItem("Bearly in Time/Build MVP Scenes")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(SceneFolder);
            BuildMainMenu();
            BuildIntro();
            BuildFruitMinigame();
            BuildBirdPhotoMinigame();
            ExtendGameplay();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateMvp();
            EditorSceneManager.OpenScene(ScenePath("MainMenu"), OpenSceneMode.Single);
            Debug.Log("Bearly in Time MVP: cinco escenas creadas y registradas.");
        }

        public static void BuildFromCommandLine()
        {
            BuildAll();
        }

        private static void BuildMainMenu()
        {
            Scene scene = CreateEmptyScene("MainMenu", PastelCream);
            Canvas canvas = CreateCanvas();
            MainMenuController controller = canvas.gameObject.AddComponent<MainMenuController>();

            UnityEngine.UI.Image accent = CreateImage("TopAccent", canvas.transform, PastelBlue);
            SetRect(accent.rectTransform, new Vector2(0f, 0.78f), Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI title = CreateText("Title", canvas.transform, "Bearly in Time", 76, DeepBlue, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.1f, 0.73f), new Vector2(0.9f, 0.93f), Vector2.zero, Vector2.zero);
            AddWhiteOutline(title.gameObject, 3f);

            GameObject buttons = CreateUiObject("MenuButtons", canvas.transform);
            RectTransform buttonRect = buttons.GetComponent<RectTransform>();
            SetRect(buttonRect, new Vector2(0.34f, 0.13f), new Vector2(0.66f, 0.72f), Vector2.zero, Vector2.zero);
            UnityEngine.UI.VerticalLayoutGroup layout = buttons.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;

            CreateMenuButton("PlayButton", buttons.transform, "Jugar", controller.Play);
            CreateMenuButton("InstructionsButton", buttons.transform, "Instrucciones", controller.ShowInstructions);
            CreateMenuButton("CreditsButton", buttons.transform, "Créditos", controller.ShowCredits);
            CreateMenuButton("SettingsButton", buttons.transform, "Configuración", controller.ShowSettings);
            CreateMenuButton("QuitButton", buttons.transform, "Salir", controller.Quit);

            GameObject instructions = CreateInfoPanel(
                canvas.transform,
                "InstructionsPanel",
                "Instrucciones",
                "Viaja de 8:00 a 20:00. Decide qué favores aceptar y reserva tiempo para fotografiar al Pájaro Rey a las 17:00.",
                controller.ClosePanels
            );
            GameObject credits = CreateInfoPanel(
                canvas.transform,
                "CreditsPanel",
                "Créditos",
                "Bearly in Time\nMVP de game jam\nHecho con Unity 6",
                controller.ClosePanels
            );
            GameObject settings = CreateInfoPanel(
                canvas.transform,
                "SettingsPanel",
                "Configuración",
                "Panel preparado para opciones de volumen, pantalla y accesibilidad.",
                controller.ClosePanels
            );

            SetObjectReference(controller, "instructionsPanel", instructions);
            SetObjectReference(controller, "creditsPanel", credits);
            SetObjectReference(controller, "settingsPanel", settings);
            instructions.SetActive(false);
            credits.SetActive(false);
            settings.SetActive(false);

            EditorSceneManager.SaveScene(scene, ScenePath("MainMenu"));
        }

        private static void BuildIntro()
        {
            Scene scene = CreateEmptyScene("Intro", PastelBlue);
            Canvas canvas = CreateCanvas();
            SimpleSceneController controller = canvas.gameObject.AddComponent<SimpleSceneController>();
            CreateCenteredCard(
                canvas.transform,
                "Aquí va la intro",
                "Continuar",
                controller.ContinueToGameplay
            );
            EditorSceneManager.SaveScene(scene, ScenePath("Intro"));
        }

        private static void BuildFruitMinigame()
        {
            Scene scene = CreateEmptyScene("FruitMinigame", Hex("FFE6D5"));
            Canvas canvas = CreateCanvas();
            MinigamePlaceholderController controller = canvas.gameObject.AddComponent<MinigamePlaceholderController>();
            CreateCenteredCard(
                canvas.transform,
                "Minijuego de Mamá: Recoger frutas",
                "Finalizar minijuego",
                controller.CompleteMinigame
            );
            EditorSceneManager.SaveScene(scene, ScenePath("FruitMinigame"));
        }

        private static void BuildBirdPhotoMinigame()
        {
            Scene scene = CreateEmptyScene("BirdPhotoMinigame", Hex("DCEFFC"));
            Canvas canvas = CreateCanvas();
            MinigamePlaceholderController controller = canvas.gameObject.AddComponent<MinigamePlaceholderController>();
            CreateCenteredCard(
                canvas.transform,
                "Minijuego: Fotografiar pájaros",
                "Finalizar minijuego",
                controller.CompleteMinigame
            );
            EditorSceneManager.SaveScene(scene, ScenePath("BirdPhotoMinigame"));
        }

        private static void ExtendGameplay()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath("Gameplay"), OpenSceneMode.Single);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                canvas = CreateCanvas();

            Transform previous = canvas.transform.Find("MVPGameplayUI");
            if (previous != null)
                Object.DestroyImmediate(previous.gameObject);

            GameObject root = CreateUiObject("MVPGameplayUI", canvas.transform);
            SetRect(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameplayEventController eventController = root.AddComponent<GameplayEventController>();
            EventPopupUI popup = root.AddComponent<EventPopupUI>();

            TextMeshProUGUI placeholder = CreateText(
                "GameplayPlaceholder",
                root.transform,
                "Aquí va el gameplay",
                54,
                DeepBlue,
                TextAlignmentOptions.Center
            );
            SetRect(placeholder.rectTransform, new Vector2(0.2f, 0.37f), new Vector2(0.8f, 0.63f), Vector2.zero, Vector2.zero);
            AddWhiteOutline(placeholder.gameObject, 2f);

            GameObject popupRoot = CreateUiObject("EventPopup", root.transform);
            RectTransform popupRect = popupRoot.GetComponent<RectTransform>();
            SetRect(popupRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UnityEngine.UI.Image dimmer = popupRoot.AddComponent<UnityEngine.UI.Image>();
            dimmer.color = new Color(0.12f, 0.2f, 0.3f, 0.62f);

            UnityEngine.UI.Image dialog = CreateImage("Dialog", popupRoot.transform, PastelCream);
            SetRect(dialog.rectTransform, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), Vector2.zero, Vector2.zero);
            AddWhiteOutline(dialog.gameObject, 5f);

            TextMeshProUGUI popupTitle = CreateText("EventTitle", dialog.transform, "Evento", 46, DeepBlue, TextAlignmentOptions.Center);
            SetRect(popupTitle.rectTransform, new Vector2(0.08f, 0.7f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI description = CreateText("EventDescription", dialog.transform, "Descripción", 30, DeepBlue, TextAlignmentOptions.Center);
            description.textWrappingMode = TextWrappingModes.Normal;
            SetRect(description.rectTransform, new Vector2(0.08f, 0.27f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero);

            UnityEngine.UI.Button accept = CreateButton("AcceptButton", dialog.transform, "Aceptar");
            SetRect(accept.GetComponent<RectTransform>(), new Vector2(0.1f, 0.07f), new Vector2(0.46f, 0.24f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(accept.onClick, popup.Accept);

            UnityEngine.UI.Button reject = CreateButton("RejectButton", dialog.transform, "Rechazar");
            SetRect(reject.GetComponent<RectTransform>(), new Vector2(0.54f, 0.07f), new Vector2(0.9f, 0.24f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(reject.onClick, popup.Reject);

            SetObjectReference(popup, "popupRoot", popupRoot);
            SetObjectReference(popup, "titleText", popupTitle);
            SetObjectReference(popup, "descriptionText", description);
            SetObjectReference(eventController, "popup", popup);
            popupRoot.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Scene CreateEmptyScene(string name, Color cameraColor)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;

            GameObject cameraObject = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = cameraColor;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            return scene;
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster)
            );
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return canvas;
        }

        private static void CreateCenteredCard(Transform parent, string message, string buttonText, UnityEngine.Events.UnityAction action)
        {
            UnityEngine.UI.Image card = CreateImage("Card", parent, PastelCream);
            SetRect(card.rectTransform, new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.78f), Vector2.zero, Vector2.zero);
            AddWhiteOutline(card.gameObject, 5f);

            TextMeshProUGUI text = CreateText("Placeholder", card.transform, message, 50, DeepBlue, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.Normal;
            SetRect(text.rectTransform, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            UnityEngine.UI.Button button = CreateButton("ContinueButton", card.transform, buttonText);
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.27f, 0.08f), new Vector2(0.73f, 0.28f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static GameObject CreateInfoPanel(
            Transform parent,
            string name,
            string heading,
            string body,
            UnityEngine.Events.UnityAction closeAction)
        {
            UnityEngine.UI.Image overlay = CreateImage(name, parent, new Color(0.15f, 0.25f, 0.35f, 0.72f));
            SetRect(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UnityEngine.UI.Image card = CreateImage("Card", overlay.transform, PastelCream);
            SetRect(card.rectTransform, new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.82f), Vector2.zero, Vector2.zero);
            AddWhiteOutline(card.gameObject, 5f);

            TextMeshProUGUI title = CreateText("Heading", card.transform, heading, 52, DeepBlue, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI content = CreateText("Body", card.transform, body, 30, DeepBlue, TextAlignmentOptions.Center);
            content.textWrappingMode = TextWrappingModes.Normal;
            SetRect(content.rectTransform, new Vector2(0.1f, 0.27f), new Vector2(0.9f, 0.72f), Vector2.zero, Vector2.zero);

            UnityEngine.UI.Button close = CreateButton("BackButton", card.transform, "Volver");
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.3f, 0.07f), new Vector2(0.7f, 0.23f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(close.onClick, closeAction);
            return overlay.gameObject;
        }

        private static void CreateMenuButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            UnityEngine.UI.Button button = CreateButton(name, parent, label);
            UnityEngine.UI.LayoutElement layout = button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = 84f;
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static UnityEngine.UI.Button CreateButton(string name, Transform parent, string label)
        {
            UnityEngine.UI.Image background = CreateImage(name, parent, Pink);
            AddWhiteOutline(background.gameObject, 3f);
            UnityEngine.UI.Button button = background.gameObject.AddComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.ColorBlock colors = button.colors;
            colors.normalColor = Pink;
            colors.highlightedColor = Hex("FFC4D3");
            colors.pressedColor = DarkPink;
            colors.selectedColor = Pink;
            button.colors = colors;

            TextMeshProUGUI text = CreateText("Label", background.transform, label, 32, DeepBlue, TextAlignmentOptions.Center);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f));
            AddWhiteOutline(text.gameObject, 1.5f);
            return button;
        }

        private static UnityEngine.UI.Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = CreateUiObject(name, parent);
            UnityEngine.UI.Image image = gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string value,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject gameObject = CreateUiObject(name, parent);
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

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void AddWhiteOutline(GameObject gameObject, float distance)
        {
            UnityEngine.UI.Outline outline = gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = White;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath("MainMenu"), true),
                new EditorBuildSettingsScene(ScenePath("Intro"), true),
                new EditorBuildSettingsScene(ScenePath("Gameplay"), true),
                new EditorBuildSettingsScene(ScenePath("FruitMinigame"), true),
                new EditorBuildSettingsScene(ScenePath("BirdPhotoMinigame"), true)
            };
        }

        private static void ValidateMvp()
        {
            string[] sceneNames =
            {
                "MainMenu", "Intro", "Gameplay", "FruitMinigame", "BirdPhotoMinigame"
            };

            foreach (string sceneName in sceneNames)
            {
                string path = ScenePath(sceneName);
                if (!File.Exists(path))
                    throw new UnityEditor.Build.BuildFailedException($"Falta la escena {path}");

                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Require<Canvas>(sceneName, "Canvas");
                Require<EventSystem>(sceneName, "EventSystem");
            }

            EditorSceneManager.OpenScene(ScenePath("MainMenu"), OpenSceneMode.Single);
            Require<MainMenuController>("MainMenu", "MainMenuController");
            RequireNamed("MainMenu", "Title");
            RequireNamed("MainMenu", "PlayButton");
            RequireNamed("MainMenu", "InstructionsPanel");
            RequireNamed("MainMenu", "CreditsPanel");
            RequireNamed("MainMenu", "SettingsPanel");

            EditorSceneManager.OpenScene(ScenePath("Gameplay"), OpenSceneMode.Single);
            Require<GameClock>("Gameplay", "GameClock");
            Require<GameManager>("Gameplay", "GameManager");
            Require<HUDController>("Gameplay", "HUDController");
            Require<GameplayEventController>("Gameplay", "GameplayEventController");
            Require<EventPopupUI>("Gameplay", "EventPopupUI");
            RequireNamed("Gameplay", "GameplayPlaceholder");
            RequireNamed("Gameplay", "ClockText");

            if (EditorBuildSettings.scenes.Length != sceneNames.Length)
                throw new UnityEditor.Build.BuildFailedException("Build Settings no contiene exactamente las cinco escenas del MVP.");

            Debug.Log("Bearly in Time MVP: validación estructural superada.");
        }

        private static void Require<T>(string sceneName, string label) where T : Component
        {
            T component = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (component == null)
                throw new UnityEditor.Build.BuildFailedException($"{sceneName}: falta {label}.");
        }

        private static void RequireNamed(string sceneName, string objectName)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform item in transforms)
            {
                if (item.name == objectName)
                    return;
            }

            throw new UnityEditor.Build.BuildFailedException($"{sceneName}: falta el objeto {objectName}.");
        }

        private static string ScenePath(string sceneName) => $"{SceneFolder}/{sceneName}.unity";

        private static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out Color color);
            return color;
        }
    }
}
#endif
