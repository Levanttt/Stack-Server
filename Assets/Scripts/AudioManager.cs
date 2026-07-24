using UnityEngine;

[System.Serializable]
public class SoundFX
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-3f, 3f)] public float pitch = 1f; 
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM Source")]
    [Tooltip("Masukkan komponen AudioSource fisik di sini untuk memutar musik")]
    public AudioSource bgmSource; 

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else 
        {
            Destroy(gameObject); 
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;
        if (bgmSource.clip == clip) return; 

        bgmSource.clip = clip;
        bgmSource.loop = true; 
        bgmSource.Play();
    }

    public AudioSource PlaySFX(SoundFX sfx)
    {
        if (sfx == null || sfx.clip == null) return null;

        GameObject tempAudio = new GameObject("SFX_" + sfx.clip.name);
        AudioSource source = tempAudio.AddComponent<AudioSource>();
        
        source.clip = sfx.clip;
        source.volume = sfx.volume;
        source.pitch = sfx.pitch;
        source.spatialBlend = 0f; 
        
        source.Play();
        Destroy(tempAudio, sfx.clip.length); 
        
        return source; 
    }
}