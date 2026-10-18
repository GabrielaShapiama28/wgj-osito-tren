#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BirdPhotoIntegrationBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/BirdPhotoMinigame.unity";
    private const string RevisionMarker = "BirdPhotoIntegrationRevision_1";
    private const string SessionKey = "BearlyInTime.BirdPhotoIntegrationBuilt1";

    [InitializeOnLoadMethod]
    private static void QueueAutomaticBuild()
    {
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
            BuildIntegration();
            SessionState.SetBool(SessionKey, true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Bearly in Time/Rebuild Bird Photo Integration")]
    public static void BuildIntegration()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Canvas canvas = FindComponent<Canvas>(scene);
            PointerController pointer = FindComponent<PointerController>(scene);
            GameObject birdObject = FindNamed(scene, "BirdVisual");
            GameObject win = FindNamed(scene, "Win");
            GameObject lose = FindNamed(scene, "Lose");

            if (canvas == null || pointer == null || birdObject == null || win == null || lose == null)
                throw new InvalidOperationException("FotoMiniTest no contiene su Canvas, Pointer, BirdVisual o pop-ups originales.");
            if (win.GetComponent<WinPopupAnimation>() == null || lose.GetComponent<WinPopupAnimation>() == null)
                throw new InvalidOperationException("Los pop-ups originales necesitan WinPopupAnimation.");

            RemoveNamed(scene, "PhotoTimerText");
            RemoveNamed(scene, RevisionMarker);

            GameObject timerObject = new GameObject(
                "PhotoTimerText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            timerObject.transform.SetParent(canvas.transform, false);
            RectTransform timerRect = timerObject.GetComponent<RectTransform>();
            timerRect.anchorMin = timerRect.anchorMax = new Vector2(0.5f, 1f);
            timerRect.pivot = new Vector2(0.5f, 1f);
            timerRect.anchoredPosition = new Vector2(0f, -24f);
            timerRect.sizeDelta = new Vector2(420f, 64f);

            TextMeshProUGUI timer = timerObject.GetComponent<TextMeshProUGUI>();
            timer.text = "Tiempo: 10";
            timer.fontSize = 34f;
            timer.fontStyle = FontStyles.Bold;
            timer.alignment = TextAlignmentOptions.Center;
            timer.color = Color.white;
            timer.raycastTarget = false;

            UnityEngine.UI.Outline outline = timerObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.08f, 0.13f, 0.20f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            UnityEngine.UI.Image birdImage = birdObject.GetComponent<UnityEngine.UI.Image>();
            if (birdImage == null)
                throw new InvalidOperationException("BirdVisual perdió su componente Image original.");
            pointer.ConfigureIntegration(10f, timer, birdImage);

            GameObject marker = new GameObject(RevisionMarker);
            marker.transform.SetParent(canvas.transform, false);
            marker.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("No se pudo guardar BirdPhotoMinigame.unity.");
        }
        finally
        {
            if (previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        AssetDatabase.SaveAssets();
        SessionState.SetBool(SessionKey, true);
        Debug.Log("[Bearly in Time] FotoMiniTest original integrado en BirdPhotoMinigame.");
    }

    [MenuItem("Bearly in Time/Validate Bird Photo Integration")]
    public static void ValidateIntegration()
    {
        List<string> errors = new List<string>();
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require<PointerController>(scene, errors);
            Require<WinPopupAnimation>(scene, errors);
            RequireNamed(scene, "Pointer", errors);
            RequireNamed(scene, "Safe Zone", errors);
            RequireNamed(scene, "BirdVisual", errors);
            RequireNamed(scene, "Win", errors);
            RequireNamed(scene, "Lose", errors);
            RequireNamed(scene, "PhotoTimerText", errors);
            RequireNamed(scene, RevisionMarker, errors);
        }
        finally
        {
            if (previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", errors));

        Debug.Log("[Bearly in Time] BirdPhotoMinigame validado: diseño original, QTE, pop-ups e integración presentes.");
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

    private static void RemoveNamed(Scene scene, string name)
    {
        GameObject existing = FindNamed(scene, name);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
    }

    private static void Require<T>(Scene scene, List<string> errors) where T : Component
    {
        if (FindComponent<T>(scene) == null)
            errors.Add($"Falta {typeof(T).Name} en {scene.name}.");
    }

    private static void RequireNamed(Scene scene, string name, List<string> errors)
    {
        if (FindNamed(scene, name) == null)
            errors.Add($"Falta el objeto {name} en {scene.name}.");
    }
}
#endif
