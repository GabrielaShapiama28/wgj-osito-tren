using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SettingsPanelController : MonoBehaviour
{
    private const string MasterVolumeKey = "BearlyInTime.MasterVolume";
    private const string MusicVolumeKey = "BearlyInTime.MusicVolume";
    private const string SfxVolumeKey = "BearlyInTime.SfxVolume";
    private const string FullscreenKey = "BearlyInTime.Fullscreen";

    [SerializeField] private UnityEngine.UI.Slider masterVolumeSlider;
    [SerializeField] private UnityEngine.UI.Slider musicVolumeSlider;
    [SerializeField] private UnityEngine.UI.Slider sfxVolumeSlider;
    [SerializeField] private UnityEngine.UI.Toggle fullscreenToggle;
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text musicValueText;
    [SerializeField] private TMP_Text sfxValueText;

    private static readonly Dictionary<int, float> SourceBaseVolumes = new Dictionary<int, float>();
    private static bool sceneCallbackRegistered;

    public void Configure(
        UnityEngine.UI.Slider configuredMasterSlider,
        UnityEngine.UI.Slider configuredMusicSlider,
        UnityEngine.UI.Slider configuredSfxSlider,
        UnityEngine.UI.Toggle configuredFullscreenToggle,
        TMP_Text configuredMasterValue,
        TMP_Text configuredMusicValue,
        TMP_Text configuredSfxValue)
    {
        masterVolumeSlider = configuredMasterSlider;
        musicVolumeSlider = configuredMusicSlider;
        sfxVolumeSlider = configuredSfxSlider;
        fullscreenToggle = configuredFullscreenToggle;
        masterValueText = configuredMasterValue;
        musicValueText = configuredMusicValue;
        sfxValueText = configuredSfxValue;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedPreferencesOnStartup()
    {
        ApplyMasterVolume(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        if (!sceneCallbackRegistered)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneCallbackRegistered = true;
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCategoryVolumes();
    }

    private void Awake()
    {
        LoadPreferencesIntoControls();
    }

    private void OnEnable()
    {
        LoadPreferencesIntoControls();
    }

    public void SetMasterVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, clamped);
        PlayerPrefs.Save();
        ApplyMasterVolume(clamped);
        SetPercent(masterValueText, clamped);
    }

    public void SetMusicVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, clamped);
        PlayerPrefs.Save();
        ApplyCategoryVolumes();
        SetPercent(musicValueText, clamped);
    }

    public void SetSfxVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, clamped);
        PlayerPrefs.Save();
        ApplyCategoryVolumes();
        SetPercent(sfxValueText, clamped);
    }

    public void SetFullscreen(bool enabled)
    {
        PlayerPrefs.SetInt(FullscreenKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Screen.fullScreen = enabled;
    }

    private void LoadPreferencesIntoControls()
    {
        float master = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        float music = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        float sfx = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        masterVolumeSlider?.SetValueWithoutNotify(master);
        musicVolumeSlider?.SetValueWithoutNotify(music);
        sfxVolumeSlider?.SetValueWithoutNotify(sfx);
        fullscreenToggle?.SetIsOnWithoutNotify(fullscreen);

        SetPercent(masterValueText, master);
        SetPercent(musicValueText, music);
        SetPercent(sfxValueText, sfx);
        ApplyMasterVolume(master);
        ApplyCategoryVolumes();
    }

    private static void ApplyMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    private static void ApplyCategoryVolumes()
    {
        float music = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        float sfx = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (AudioSource source in sources)
        {
            if (source == null)
                continue;

            int id = source.GetInstanceID();
            if (!SourceBaseVolumes.TryGetValue(id, out float baseVolume))
            {
                baseVolume = source.volume;
                SourceBaseVolumes[id] = baseVolume;
            }

            source.volume = baseVolume * (IsMusic(source) ? music : sfx);
        }
    }

    private static bool IsMusic(AudioSource source)
    {
        string sourceName = source.name ?? string.Empty;
        return source.loop
            || sourceName.IndexOf("music", StringComparison.OrdinalIgnoreCase) >= 0
            || sourceName.IndexOf("musica", StringComparison.OrdinalIgnoreCase) >= 0
            || sourceName.IndexOf("música", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void SetPercent(TMP_Text label, float value)
    {
        if (label != null)
            label.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
    }
}
