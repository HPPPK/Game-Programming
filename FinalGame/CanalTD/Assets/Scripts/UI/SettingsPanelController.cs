/*
 * File: SettingsPanelController.cs
 *
 * Purpose:
 * Implements SettingsPanelController for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for SettingsPanelController within the ui system.
 * - Update the owning object state and react to gameplay events during play.
 * - Present readable feedback so players can understand turns, actions, and results.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Player input, button clicks, pointer events, or scene transition requests.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Updates visible UI, indicators, prompts, and player-facing status messages.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify SettingsPanelController in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
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

    /// <summary>
    /// Finds and stores settings panel controller references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        LoadSettings();
        BindListenersOnce();
    }

    /// <summary>
    /// Sets up settings panel controller when this scene object starts running.
    /// </summary>
    private void Start()
    {
        CloseSettings();
    }

    /// <summary>
    /// Removes settings panel controller listeners and temporary references before destruction.
    /// </summary>
    private void OnDestroy()
    {
        UnbindListeners();
    }

    /// <summary>
    /// Checks the current state to decide whether open is true.
    /// </summary>
    public bool IsOpen()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }

    /// <summary>
    /// Handles open settings for UI display, input, or player feedback.
    /// </summary>
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

    /// <summary>
    /// Handles close settings for UI display, input, or player feedback.
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Handles toggle settings for UI display, input, or player feedback.
    /// </summary>
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

    /// <summary>
    /// Loads settings from saved settings, room data, or scene references.
    /// </summary>
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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicEnabled(musicEnabled);
            AudioManager.Instance.SetSfxEnabled(sfxEnabled);
            AudioManager.Instance.SetMasterVolume(masterVolume);
        }
        else
        {
            AudioListener.volume = masterVolume;
        }
    }

    /// <summary>
    /// Saves settings so it persists after the current UI or scene update.
    /// </summary>
    public void SaveSettings()
    {
        bool musicEnabled = musicToggle == null || musicToggle.isOn;
        bool sfxEnabled = sfxToggle == null || sfxToggle.isOn;
        bool fullscreen = fullscreenToggle != null && fullscreenToggle.isOn;
        float masterVolume = masterVolumeSlider != null ? masterVolumeSlider.value : 1f;

        PlayerPrefs.SetInt(MusicEnabledKey, musicEnabled ? 1 : 0);
        PlayerPrefs.SetInt(SfxEnabledKey, sfxEnabled ? 1 : 0);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Responds to on music toggle changed and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnMusicToggleChanged(bool enabled)
    {
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicEnabled(enabled);
        }
    }

    /// <summary>
    /// Responds to on SFX toggle changed and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnSfxToggleChanged(bool enabled)
    {
        PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxEnabled(enabled);
        }
    }

    /// <summary>
    /// Responds to on fullscreen toggle changed and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnFullscreenToggleChanged(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Responds to on master volume changed and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnMasterVolumeChanged(float volume)
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, volume);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }
        else
        {
            AudioListener.volume = volume;
        }
    }

    /// <summary>
    /// Handles bind listeners once for UI display, input, or player feedback.
    /// </summary>
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

    /// <summary>
    /// Handles unbind listeners for UI display, input, or player feedback.
    /// </summary>
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
