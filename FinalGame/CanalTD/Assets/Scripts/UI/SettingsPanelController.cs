/*
 * File: SettingsPanelController.cs
 *
 * Purpose:
 * Controls the global settings panel and stores player preferences.
 */
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingsPanel;

    [Header("Controls")]
    public Toggle musicToggle;
    public Toggle sfxToggle;
    public Toggle fullscreenToggle;
    public Slider masterVolumeSlider;

    private const string MusicEnabledKey = "Settings_MusicEnabled";
    private const string SfxEnabledKey = "Settings_SfxEnabled";
    private const string FullscreenKey = "Settings_Fullscreen";
    private const string MasterVolumeKey = "Settings_MasterVolume";

    private bool listenersBound;

    private void Awake()
    {
        LoadSettings();
        BindListenersOnce();
    }

    private void Start()
    {
        CloseSettings();
    }

    private void OnDestroy()
    {
        UnbindListeners();
    }

    // Returns whether the settings panel is currently visible.
    public bool IsOpen()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }

    // Opens the settings panel and refreshes controls from PlayerPrefs.
    public void OpenSettings()
    {
        LoadSettings();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("SettingsPanelController settingsPanel is not assigned.");
        }
    }

    // Closes the settings panel without changing saved settings.
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Toggles settings visibility.
    public void ToggleSettings()
    {
        if (IsOpen())
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }

    // Reads saved settings and applies them to Unity and the UI controls.
    public void LoadSettings()
    {
        bool musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        bool sfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(musicEnabled);
        }

        if (sfxToggle != null)
        {
            sfxToggle.SetIsOnWithoutNotify(sfxEnabled);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        }

        Screen.fullScreen = fullscreen;
        AudioListener.volume = masterVolume;
    }

    // Saves the current UI settings to PlayerPrefs.
    public void SaveSettings()
    {
        PlayerPrefs.SetInt(MusicEnabledKey, musicToggle == null || musicToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt(SfxEnabledKey, sfxToggle == null || sfxToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt(FullscreenKey, fullscreenToggle != null && fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolumeSlider != null ? masterVolumeSlider.value : 1f);
        PlayerPrefs.Save();
    }

    // Handles the Music toggle changing.
    public void OnMusicToggleChanged(bool enabled)
    {
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Handles the SFX toggle changing.
    public void OnSfxToggleChanged(bool enabled)
    {
        PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Handles the fullscreen toggle changing.
    public void OnFullscreenToggleChanged(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Handles master volume slider changes.
    public void OnMasterVolumeChanged(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(MasterVolumeKey, volume);
        PlayerPrefs.Save();
    }

    // Registers UI callbacks once so Inspector and code do not stack duplicate listeners.
    private void BindListenersOnce()
    {
        if (listenersBound)
        {
            return;
        }

        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }

        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.RemoveListener(OnSfxToggleChanged);
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggleChanged);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        listenersBound = true;
    }

    // Removes callbacks when this object is destroyed.
    private void UnbindListeners()
    {
        if (!listenersBound)
        {
            return;
        }

        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);
        }

        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.RemoveListener(OnSfxToggleChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggleChanged);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        }

        listenersBound = false;
    }
}
