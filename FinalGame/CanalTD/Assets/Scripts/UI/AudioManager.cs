/*
 * File: AudioManager.cs
 *
 * Purpose:
 * Implements AudioManager for the ui layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for AudioManager within the ui system.
 * - Coordinate related objects, state changes, and cross-system communication.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify AudioManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip uiClickSound;
    public AudioClip towerShootSound;
    public AudioClip enemyDeathSound;
    public AudioClip buildSound;
    public AudioClip victorySound;
    public AudioClip cardPlaySound;

    [Header("Pitch Settings")]
    [Range(0.5f, 2f)] public float uiClickPitch = 1.25f;
    [Range(0.5f, 2f)] public float towerShootPitch = 1.0f;
    [Range(0.5f, 2f)] public float enemyDeathPitch = 0.9f;
    [Range(0.5f, 2f)] public float buildPitch = 1.1f;
    [Range(0.5f, 2f)] public float victoryPitch = 1.0f;
    [Range(0.5f, 2f)] public float cardPlayPitch = 1.0f;

    [Header("SFX Volume Settings")]
    [Range(0f, 1f)] public float uiClickVolume = 1f;
    [Range(0f, 1f)] public float towerShootVolume = 1f;
    [Range(0f, 1f)] public float enemyDeathVolume = 1f;
    [Range(0f, 1f)] public float buildVolume = 1f;
    [Range(0f, 1f)] public float victoryVolume = 1f;
    [Range(0f, 1f)] public float cardPlayVolume = 1f;

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    public bool musicEnabled = true;
    public bool sfxEnabled = true;

    private const string MusicEnabledKey = "Settings_MusicEnabled";
    private const string SfxEnabledKey = "Settings_SfxEnabled";
    private const string MasterVolumeKey = "Settings_MasterVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAudioSettings();
        ApplyAudioSettings();
        PlayBackgroundMusic();
    }

    private void LoadAudioSettings()
    {
        musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
    }

    public void ApplyAudioSettings()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = musicEnabled ? masterVolume * 0.45f : 0f;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = sfxEnabled ? masterVolume : 0f;
            sfxSource.pitch = 1f;
        }

        AudioListener.volume = masterVolume;
    }

    public void PlayBackgroundMusic()
    {
        if (bgmSource == null || backgroundMusic == null)
        {
            return;
        }

        bgmSource.clip = backgroundMusic;
        bgmSource.loop = true;

        if (musicEnabled && !bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (bgmSource == null)
        {
            return;
        }

        if (enabled)
        {
            bgmSource.volume = masterVolume * 0.45f;

            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }
        else
        {
            bgmSource.Pause();
        }
    }

    public void SetSfxEnabled(bool enabled)
    {
        sfxEnabled = enabled;
        PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (sfxSource != null)
        {
            sfxSource.volume = enabled ? masterVolume : 0f;
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.Save();

        ApplyAudioSettings();
    }

    public void PlaySfx(AudioClip clip, float pitch = 1f, float volume = 1f)
    {
        if (!sfxEnabled || sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, masterVolume * volume);
        sfxSource.pitch = 1f;
    }

    public void PlayUiClick()
    {
        PlaySfx(uiClickSound, uiClickPitch, uiClickVolume);
    }

    public void PlayTowerShoot()
    {
        PlaySfx(towerShootSound, towerShootPitch, towerShootVolume);
    }

    public void PlayEnemyDeath()
    {
        PlaySfx(enemyDeathSound, enemyDeathPitch, enemyDeathVolume);
    }

    public void PlayBuild()
    {
        PlaySfx(buildSound, buildPitch, buildVolume);
    }

    public void PlayVictory()
    {
        PlaySfx(victorySound, victoryPitch, victoryVolume);
    }

    public void PlayCardPlay()
    {
        PlaySfx(cardPlaySound, cardPlayPitch, cardPlayVolume);
    }
}