#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GameplayPlayModeSmokeRunner
{
    // Prueba integral de navegación y reglas críticas del MVP.
    private const string RequestPath = "Temp/RunGameplaySmokeTest.request";
    private const string GameplayPath = "Assets/Game/Scenes/Gameplay.unity";
    private static int step;
    private static double stepStarted;
    private static float clockAfterNormalPhoto;
    private static float clockBeforeNormalPhoto;
    private static float clockBeforeFailedPhoto;
    private static int batchExitCode;

    static GameplayPlayModeSmokeRunner()
    {
        EditorApplication.delayCall += TryStartRequestedTest;
    }

    [MenuItem("Bearly in Time/Run Gameplay Play Mode Smoke Test")]
    public static void RequestTestFromMenu()
    {
        Directory.CreateDirectory("Temp");
        File.WriteAllText(RequestPath, "requested");
        TryStartRequestedTest();
    }

    private static void TryStartRequestedTest()
    {
        if (!File.Exists(RequestPath)
            || EditorApplication.isCompiling
            || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(RequestPath);
        EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
        batchExitCode = 0;
        step = 0;
        stepStarted = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
            return;

        try
        {
            if (EditorApplication.timeSinceStartup - stepStarted > 12d)
                throw new TimeoutException($"Timeout en paso {step}, escena {SceneManager.GetActiveScene().name}.");

            switch (step)
            {
                case 0:
                    InScene("Gameplay", () =>
                    {
                        GameManager manager = Need<GameManager>();
                        Need<GameplayEventController>();
                        GameplayEventController events = Need<GameplayEventController>();
                        if (Mathf.Abs(events.NormalBirdIntervalSeconds - 20f) > 0.01f)
                            throw new InvalidOperationException("El intervalo de pájaros no está configurado en 20 segundos.");
                        manager.AdvanceGameplayIntervals(21f);
                        Next();
                    });
                    break;
                case 1:
                    InScene("Gameplay", () =>
                    {
                        GameObject bird = NeedNamed("BirdButton");
                        if (!bird.activeInHierarchy)
                            return;
                        clockBeforeNormalPhoto = GameManager.Instance.Clock.CurrentTotalMinutes;
                        Need<GameplayEventController>().PhotographVisibleBird();
                        Next();
                    });
                    break;
                case 2:
                    InScene("BirdPhotoMinigame", () =>
                    {
                        PointerController photo = Need<PointerController>();
                        UnityEngine.UI.Image bird = NeedNamed("BirdVisual").GetComponent<UnityEngine.UI.Image>();
                        if (bird.sprite != GameManager.Instance.ActiveBirdSprite)
                            throw new InvalidOperationException("BirdPhotoMinigame no mostró el pájaro seleccionado.");
                        if (photo.SelectedBirdSprite != GameManager.Instance.ActiveBirdSprite)
                            throw new InvalidOperationException("BirdPhotoMinigame no conservó la selección inmutable del pájaro.");
                        if (NeedNamed("PhotoTimerText").GetComponent<TMPro.TMP_Text>() == null)
                            throw new InvalidOperationException("Falta el temporizador de fotografía.");
                        photo.TestFinish(true);
                        photo.ContinueToGameplay();
                        Next();
                    });
                    break;
                case 3:
                    InScene("Gameplay", () =>
                    {
                        GameManager manager = Need<GameManager>();
                        if (manager.BearEnergy != 12)
                            throw new InvalidOperationException($"Energía tras foto normal: {manager.BearEnergy}, se esperaba 12.");
                        if (Mathf.Abs(manager.Clock.CurrentTotalMinutes - (clockBeforeNormalPhoto + 10f)) > 1.5f)
                            throw new InvalidOperationException("La fotografía normal no consumió exactamente 10 minutos.");
                        clockAfterNormalPhoto = manager.Clock.CurrentTotalMinutes;
                        manager.AdvanceGameplayIntervals(61f);
                        Next();
                    });
                    break;
                case 4:
                    InScene("Gameplay", () =>
                    {
                        EventPopupUI popup = Need<EventPopupUI>();
                        if (!popup.IsOpen)
                            return;
                        Need<GameplayEventController>().RejectCurrentEvent();
                        GameManager manager = Need<GameManager>();
                        if (manager.MotherRelationship != 8 || manager.SelfRelationship != 12)
                            throw new InvalidOperationException("El rechazo no aplicó Mamá -2 y Yo +2.");
                        manager.AdvanceGameplayIntervals(61f);
                        Next();
                    });
                    break;
                case 5:
                    InScene("Gameplay", () =>
                    {
                        if (!Need<EventPopupUI>().IsOpen)
                            return;
                        Need<GameplayEventController>().AcceptCurrentEvent();
                        Next();
                    });
                    break;
                case 6:
                    InScene("FruitMinigame", () =>
                    {
                        FruitMinigameController fruit = Need<FruitMinigameController>();
                        ValidateFruitTargets(fruit);
                        typeof(FruitMinigameController)
                            .GetMethod("FinishRound", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?.Invoke(fruit, new object[] { true });
                        GameObject basketTrigger = NeedNamed("BasketCollisionTrigger");
                        if (basketTrigger.GetComponent<FruitBasketCatcher>() == null)
                            throw new InvalidOperationException("La zona de captura no pertenece a la canasta.");
                        if (NeedNamed("BearBody").GetComponent<Collider2D>() != null)
                            throw new InvalidOperationException("El cuerpo del oso no debe atrapar frutas.");
                        NeedNamed("ContinueButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                        Next();
                    });
                    break;
                case 7:
                    InScene("Gameplay", () =>
                    {
                        GameManager manager = Need<GameManager>();
                        if (manager.MotherRelationship != 10 || manager.BearEnergy != 10)
                            throw new InvalidOperationException("El favor exitoso no aplicó Mamá +2 y Energía -2.");
                        float expected = Mathf.Min(20f * 60f, clockAfterNormalPhoto + 120f);
                        if (Mathf.Abs(manager.Clock.CurrentTotalMinutes - expected) > 1.5f)
                            throw new InvalidOperationException("FruitMinigame no consumió exactamente dos horas.");
                        manager.Clock.AdvanceTime(17f * 60f - manager.Clock.CurrentTotalMinutes);
                        Next();
                    });
                    break;
                case 8:
                    InScene("Gameplay", () =>
                    {
                        GameObject bird = NeedNamed("BirdButton");
                        if (!bird.activeInHierarchy)
                            return;
                        UnityEngine.UI.Image image = bird.GetComponent<UnityEngine.UI.Image>();
                        if (image.sprite == null || !image.sprite.name.Contains("PajaroRey"))
                            throw new InvalidOperationException("A las 17:00 no apareció el sprite del Pájaro Rey.");
                        Need<GameplayEventController>().PhotographVisibleBird();
                        Next();
                    });
                    break;
                case 9:
                    InScene("BirdPhotoMinigame", () =>
                    {
                        PointerController photo = Need<PointerController>();
                        photo.TestFinish(true);
                        photo.ContinueToGameplay();
                        Next();
                    });
                    break;
                case 10:
                    InScene("Gameplay", () =>
                    {
                        GameManager manager = Need<GameManager>();
                        if (!manager.KingBirdPhotographed)
                            throw new InvalidOperationException("El resultado del Pájaro Rey no quedó guardado.");
                        manager.Clock.AdvanceTime(20f * 60f - manager.Clock.CurrentTotalMinutes);
                        Next();
                    });
                    break;
                case 11:
                    InScene("Gameplay", () =>
                    {
                        GameManager manager = Need<GameManager>();
                        GameObject final = NeedNamed("FinalPanel");
                        if (!final.activeInHierarchy)
                            return;
                        if (!manager.DidWin)
                            throw new InvalidOperationException("Las condiciones ganadoras no evaluaron GANASTE.");
                        NeedNamed("RestartButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                        Next();
                    });
                    break;
                case 12:
                    InScene("Gameplay", () =>
                    {
                        GameManager manager = Need<GameManager>();
                        if (manager.MotherRelationship != 10
                            || manager.BearEnergy != 10
                            || manager.SelfRelationship != 10)
                            throw new InvalidOperationException("Reiniciar no restauró las barras a 10.");
                        manager.AdvanceGameplayIntervals(21f);
                        Next();
                    });
                    break;
                case 13:
                    InScene("Gameplay", () =>
                    {
                        GameObject bird = NeedNamed("BirdButton");
                        if (!bird.activeInHierarchy)
                            return;
                        clockBeforeFailedPhoto = GameManager.Instance.Clock.CurrentTotalMinutes;
                        Need<GameplayEventController>().PhotographVisibleBird();
                        Next();
                    });
                    break;
                case 14:
                    InScene("BirdPhotoMinigame", () =>
                    {
                        PointerController photo = Need<PointerController>();
                        photo.TestFinish(false);
                        if (!NeedNamed("Lose").activeInHierarchy)
                            throw new InvalidOperationException("La derrota no mostró el pop-up original Lose.");
                        photo.ContinueToGameplay();
                        Next();
                    });
                    break;
                case 15:
                    InScene("Gameplay", () =>
                    {
                        GameManager manager = Need<GameManager>();
                        if (manager.BearEnergy != 10 || manager.LastMinigameSucceeded)
                            throw new InvalidOperationException("Una fotografía fallida entregó recompensa.");
                        if (Mathf.Abs(manager.Clock.CurrentTotalMinutes - (clockBeforeFailedPhoto + 10f)) > 1.5f)
                            throw new InvalidOperationException("La fotografía fallida no consumió exactamente 10 minutos.");
                        manager.IgnoreBirdEvent("king-bird-17", GameplayEventType.KingBird);
                        manager.Clock.AdvanceTime(20f * 60f - manager.Clock.CurrentTotalMinutes);
                        Next();
                    });
                    break;
                case 16:
                    InScene("Gameplay", () =>
                    {
                        GameManager manager = Need<GameManager>();
                        if (!NeedNamed("FinalPanel").activeInHierarchy)
                            return;
                        if (manager.DidWin)
                            throw new InvalidOperationException("El final de derrota evaluó incorrectamente GANASTE.");
                        UnityEngine.UI.Image art = NeedNamed("FinalArtwork").GetComponent<UnityEngine.UI.Image>();
                        if (art.sprite == null || !art.sprite.name.Contains("perdiste"))
                            throw new InvalidOperationException("El final de derrota no usa perdiste.PNG.");
                        Pass();
                    });
                    break;
            }
        }
        catch (Exception exception)
        {
            batchExitCode = 1;
            Debug.LogError($"[Bearly in Time Smoke Test] FALLÓ: {exception}");
            Stop();
        }
    }

    private static void ValidateFruitTargets(FruitMinigameController fruit)
    {
        Type type = typeof(FruitMinigameController);
        int red = ReadInt(type, fruit, "redTarget");
        int green = ReadInt(type, fruit, "greenTarget");
        int lemon = ReadInt(type, fruit, "lemonTarget");
        if (red < 1 || green < 1 || lemon < 1 || red + green + lemon != 10)
            throw new InvalidOperationException($"Objetivos de frutas inválidos: {red}+{green}+{lemon}.");
    }

    private static int ReadInt(Type type, object instance, string field)
    {
        return (int)(type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance) ?? -1);
    }

    private static void InScene(string sceneName, Action action)
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, sceneName, StringComparison.Ordinal))
            return;
        action();
    }

    private static T Need<T>() where T : UnityEngine.Object
    {
        T value = UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (value == null)
            throw new InvalidOperationException($"No se encontró {typeof(T).Name}.");
        return value;
    }

    private static GameObject NeedNamed(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform found = FindRecursive(root.transform, name);
            if (found != null)
                return found.gameObject;
        }
        throw new InvalidOperationException($"No se encontró {name}.");
    }

    private static Transform FindRecursive(Transform current, string name)
    {
        if (current.name == name)
            return current;
        for (int index = 0; index < current.childCount; index++)
        {
            Transform found = FindRecursive(current.GetChild(index), name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static void Next()
    {
        step++;
        stepStarted = EditorApplication.timeSinceStartup;
    }

    private static void Pass()
    {
        Debug.Log("[Bearly in Time Smoke Test] SUPERADO: QTE de fotografía, pájaro seleccionado, victoria/derrota, 10 minutos, pájaro normal, Mamá, FruitMinigame, Pájaro Rey, barras, tiempo, reinicio y finales.");
        Stop();
    }

    private static void Stop()
    {
        EditorApplication.update -= Tick;
        if (Application.isBatchMode)
        {
            EditorApplication.playModeStateChanged -= ExitBatchModeAfterPlay;
            EditorApplication.playModeStateChanged += ExitBatchModeAfterPlay;
        }
        EditorApplication.isPlaying = false;
    }

    private static void ExitBatchModeAfterPlay(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;

        EditorApplication.playModeStateChanged -= ExitBatchModeAfterPlay;
        EditorApplication.Exit(batchExitCode);
    }
}
#endif
