using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic Instance;
    
    private AudioSource audioSource;
    private AudioClip defaultClip;

    // This ensures the script automatically loads and runs when the game starts.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        // Check if one already exists to prevent duplicates when reloading scenes
        if (Object.FindFirstObjectByType<BackgroundMusic>() != null) return;

        GameObject go = new GameObject("BackgroundMusicManager");
        DontDestroyOnLoad(go);
        go.AddComponent<BackgroundMusic>();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        // Load the background music from the Resources folder
        defaultClip = Resources.Load<AudioClip>("Starting Out Waltz Vivace");

        if (defaultClip != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = defaultClip;
            
            // Make the music loop continuously
            audioSource.loop = true;
            
            // Automatically play when the game starts
            audioSource.playOnAwake = true;
            
            // Adjust the volume here if it's too loud (0.0 to 1.0)
            audioSource.volume = 0.5f; 
            
            audioSource.Play();
        }
    }

    public void PlayCookieTimeMusic()
    {
        if (audioSource == null) return;
        
        AudioClip cookieMusic = Resources.Load<AudioClip>("382251__alexprost__energetic-bigbeat-drum-loop");
        if (cookieMusic != null)
        {
            audioSource.clip = cookieMusic;
            audioSource.Play();
        }
    }

    public void PlayDefaultMusic()
    {
        if (audioSource == null || defaultClip == null) return;

        // Only switch back if we aren't already playing the default
        if (audioSource.clip != defaultClip)
        {
            audioSource.clip = defaultClip;
            audioSource.Play();
        }
    }
}
