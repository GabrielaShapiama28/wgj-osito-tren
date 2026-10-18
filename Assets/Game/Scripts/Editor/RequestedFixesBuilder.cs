#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RequestedFixesBuilder
{
    private const string MainMenuPath = "Assets/Game/Scenes/MainMenu.unity";
    private const string GameplayPath = "Assets/Game/Scenes/Gameplay.unity";
    private const string BirdPhotoPath = "Assets/Game/Scenes/BirdPhotoMinigame.unity";

    [MenuItem("Bearly in Time/Apply Menu and Gameplay Fixes")]
    public static void BuildAndValidate()
    {
        BearlyInTime.Editor.MvpSceneBuilder.BuildMainMenuOnly();
        GameplayLogicBuilder.BuildGameplaySystems();
        BirdPhotoIntegrationBuilder.BuildIntegration();
        Validate();
        Debug.Log("[Bearly in Time] Menú, HUD, Pájaro Rey, rechazo y fotografía corregidos y conectados.");
    }

    [MenuItem("Bearly in Time/Validate Menu and Gameplay Fixes")]
    public static void Validate()
    {
        List<string> errors = new List<string>();
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene menu = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            Require<MainMenuController>(menu, errors);
            Require<SettingsPanelController>(menu, errors);
            RequireNamed(menu, "InstructionsPanel", errors);
            RequireNamed(menu, "CreditsPanel", errors);
            RequireNamed(menu, "SettingsPanel", errors);
            RequireNamed(menu, "MasterVolumeSlider", errors);
            RequireNamed(menu, "MusicVolumeSlider", errors);
            RequireNamed(menu, "SfxVolumeSlider", errors);
            RequireNamed(menu, "FullscreenToggle", errors);
            RequireBackButton(menu, "InstructionsPanel", errors);
            RequireBackButton(menu, "CreditsPanel", errors);
            RequireBackButton(menu, "SettingsPanel", errors);

            Scene gameplay = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
            GameplayEventController events = FindComponent<GameplayEventController>(gameplay);
            Require<GameplayStatusUI>(gameplay, errors);
            Require<KingBirdAnnouncementUI>(gameplay, errors);
            if (events == null)
                errors.Add("Gameplay: falta GameplayEventController.");

            GameObject bird = FindNamed(gameplay, "BirdButton");
            if (bird == null)
            {
                errors.Add("Gameplay: falta BirdButton.");
            }
            else
            {
                RectTransform rect = bird.GetComponent<RectTransform>();
                if (rect == null || rect.anchorMin.x < 0.75f)
                    errors.Add("Gameplay: el pájaro no está colocado en el lado derecho de la ventana.");
            }

            if (FindNamed(gameplay, "SelfLabel") != null)
                errors.Add("Gameplay: el texto Relación Yo todavía está visible.");
            ValidateCounter(gameplay, "MotherValue", errors);
            ValidateCounter(gameplay, "SelfValue", errors);

            Scene birdPhoto = EditorSceneManager.OpenScene(BirdPhotoPath, OpenSceneMode.Single);
            Require<PointerController>(birdPhoto, errors);
            RequireNamed(birdPhoto, "BirdVisual", errors);
            RequireNamed(birdPhoto, "PhotoTimerText", errors);
            ValidateMotherRejection(errors);
        }
        finally
        {
            if (previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", errors));

        Debug.Log("[Bearly in Time] Validación superada: paneles, preferencias, HUD, aviso del Rey y fotografía conectados.");
    }

    private static void ValidateMotherRejection(List<string> errors)
    {
        GameObject probe = new GameObject("MotherRejectionValidationProbe");
        try
        {
            probe.AddComponent<GameClock>();
            GameManager manager = probe.AddComponent<GameManager>();
            manager.RejectGameplayEvent("validation-mother-favor", GameplayEventType.MotherFavor);
            manager.RejectGameplayEvent("validation-mother-favor", GameplayEventType.MotherFavor);
            if (manager.MotherRelationship != 8 || manager.SelfRelationship != 12 || manager.BearEnergy != 10)
                errors.Add("Gameplay: rechazar no aplica exactamente Mamá -2, Yo +2 y Energía sin cambios una sola vez.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probe);
        }
    }

    private static void RequireBackButton(Scene scene, string panelName, List<string> errors)
    {
        GameObject panel = FindNamed(scene, panelName);
        Transform back = panel != null ? FindRecursive(panel.transform, "BackButton") : null;
        UnityEngine.UI.Button button = back != null ? back.GetComponent<UnityEngine.UI.Button>() : null;
        if (button == null || button.onClick.GetPersistentEventCount() == 0)
            errors.Add($"MainMenu: {panelName} no tiene un botón Volver conectado.");
    }

    private static void ValidateCounter(Scene scene, string name, List<string> errors)
    {
        GameObject counter = FindNamed(scene, name);
        TMP_Text text = counter != null ? counter.GetComponent<TMP_Text>() : null;
        RectTransform rect = counter != null ? counter.GetComponent<RectTransform>() : null;
        if (text == null || rect == null)
        {
            errors.Add($"Gameplay: falta el contador {name}.");
            return;
        }

        if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one)
            errors.Add($"Gameplay: {name} no está encuadrado dentro de su barra.");
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

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int index = 0; index < root.childCount; index++)
        {
            Transform match = FindRecursive(root.GetChild(index), name);
            if (match != null)
                return match;
        }
        return null;
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
}
#endif
