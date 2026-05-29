using UnityEngine;
using UnityEngine.SceneManagement;

public class RunnerAudioManager : MonoBehaviour
{
    private const string MusicVolumeKey = "HoboRushMusicVolume";
    private const string SfxVolumeKey = "HoboRushSfxVolume";
    private const string RuntimeMilestonePath = "HoboRushAudio/Runner_Milestone";

    [SerializeField, Range(0f, 1f)] private float masterVolume = 0.82f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.28f;
    [SerializeField, Range(0f, 1f)] private float movementVolume = 0.58f;
    [SerializeField, Range(0f, 1f)] private float warningVolume = 0.38f;
    [SerializeField, Range(0f, 1f)] private float uiVolume = 0.56f;
    [SerializeField, Range(0f, 1f)] private float gameOverVolume = 0.78f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.1f;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioClip[] jumpClips;
    [SerializeField] private AudioClip[] landClips;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip[] slideClips;
    [SerializeField] private AudioClip[] obstacleWarningClips;
    [SerializeField] private AudioClip[] birdWarningClips;
    [SerializeField] private AudioClip[] uiClickClips;
    [SerializeField] private AudioClip[] milestoneClips;
    [SerializeField] private AudioClip[] impactClips;
    [SerializeField] private AudioClip[] gameOverClips;
    [SerializeField] private int sourcePoolSize = 8;
    [SerializeField] private Vector2 pitchJitter = new Vector2(0.94f, 1.06f);

    private AudioSource[] sources;
    private AudioSource musicSource;
    private AudioClip runtimeMilestoneClip;
    private int sourceIndex;
    private float lastWarningTime;
    private float lastFootstepTime;

    public static RunnerAudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySavedVolumeSettings();
        NormalizeAudioListeners();
        BuildSourcePool();
        StartMusic();
    }

    public void PlayJump()
    {
        PlayRandom(jumpClips, movementVolume, 1.02f);
    }

    public void PlayLand()
    {
        PlayRandom(landClips, movementVolume * 0.75f, 0.96f);
    }

    public void PlayFootstep()
    {
        if (Time.unscaledTime - lastFootstepTime < 0.09f)
        {
            return;
        }

        lastFootstepTime = Time.unscaledTime;
        PlayRandom(footstepClips, footstepVolume, 1f);
    }

    public void PlaySlide()
    {
        PlayRandom(slideClips, movementVolume * 0.7f, 0.92f);
    }

    public void PlayObstacleWarning(bool bird)
    {
        if (Time.unscaledTime - lastWarningTime < 0.18f)
        {
            return;
        }

        lastWarningTime = Time.unscaledTime;
        PlayRandom(bird ? birdWarningClips : obstacleWarningClips, warningVolume, bird ? 1.08f : 0.95f);
    }

    public void PlayUiClick()
    {
        PlayRandom(uiClickClips, uiVolume, 1f);
    }

    public void PlayMilestone()
    {
        if (runtimeMilestoneClip == null)
        {
            runtimeMilestoneClip = Resources.Load<AudioClip>(RuntimeMilestonePath);
        }

        if (runtimeMilestoneClip != null)
        {
            PlayClip(runtimeMilestoneClip, uiVolume * 0.95f, 1.02f);
            return;
        }

        PlayRandom(milestoneClips, uiVolume * 0.9f, 1.04f);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }

    public void PlayGameOver()
    {
        PlayRandom(impactClips, gameOverVolume, 0.82f);
        PlayRandom(gameOverClips, gameOverVolume, 0.96f);
    }

    private void BuildSourcePool()
    {
        int count = Mathf.Max(2, sourcePoolSize);
        sources = new AudioSource[count];
        for (int i = 0; i < count; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            sources[i] = source;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        NormalizeAudioListeners();
        ApplySavedVolumeSettings();
        ApplyMusicVolume();
        StartMusic();
    }

    private void NormalizeAudioListeners()
    {
        AudioListener[] ownListeners = GetComponents<AudioListener>();
        for (int i = 0; i < ownListeners.Length; i++)
        {
            ownListeners[i].enabled = false;
        }

        AudioListener[] activeListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (activeListeners.Length == 0)
        {
            if (Camera.main != null)
            {
                AudioListener cameraListener = Camera.main.GetComponent<AudioListener>();
                if (cameraListener == null)
                {
                    cameraListener = Camera.main.gameObject.AddComponent<AudioListener>();
                }

                cameraListener.enabled = true;
                return;
            }

            AudioListener fallback = gameObject.GetComponent<AudioListener>();
            if (fallback == null)
            {
                fallback = gameObject.AddComponent<AudioListener>();
            }

            fallback.enabled = true;
            return;
        }

        if (activeListeners.Length <= 1)
        {
            return;
        }

        AudioListener preferred = null;
        if (Camera.main != null)
        {
            preferred = Camera.main.GetComponent<AudioListener>();
        }

        if (preferred == null)
        {
            preferred = activeListeners[0];
        }

        for (int i = 0; i < activeListeners.Length; i++)
        {
            activeListeners[i].enabled = activeListeners[i] == preferred;
        }
    }

    private void StartMusic()
    {
        if (musicSource != null)
        {
            ApplyMusicVolume();
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
            return;
        }

        if (musicClip == null)
        {
            musicClip = Resources.Load<AudioClip>("HoboRushAudio/Runner_Music");
        }

        if (musicClip == null)
        {
            return;
        }

        float resolvedMusicVolume = musicVolume > 0f ? musicVolume : 0.1f;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicVolume = resolvedMusicVolume;
        ApplyMusicVolume();
        musicSource.Play();
    }

    private void ApplyMusicVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(musicVolume * masterVolume);
        }
    }

    private void PlayRandom(AudioClip[] clips, float volume, float pitch)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
        {
            return;
        }

        AudioSource source = NextSource();
        source.pitch = Mathf.Clamp(pitch * Random.Range(pitchJitter.x, pitchJitter.y), 0.6f, 1.4f);
        source.volume = Mathf.Clamp01(volume * masterVolume * sfxVolume);
        source.PlayOneShot(clip);
    }

    private void PlayClip(AudioClip clip, float volume, float pitch)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = NextSource();
        source.pitch = Mathf.Clamp(pitch * Random.Range(pitchJitter.x, pitchJitter.y), 0.6f, 1.4f);
        source.volume = Mathf.Clamp01(volume * masterVolume * sfxVolume);
        source.PlayOneShot(clip);
    }

    private void ApplySavedVolumeSettings()
    {
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume));
    }

    private AudioSource NextSource()
    {
        sourceIndex = (sourceIndex + 1) % sources.Length;
        return sources[sourceIndex];
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
