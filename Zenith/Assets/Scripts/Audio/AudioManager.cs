using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [Header("Main Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource soundFXSource;

    [Header("SFX Library")]
    public List<SFXEntry> sfxClips = new List<SFXEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();
    }

    private void LoadVolumes()
    {
        float master = PlayerPrefs.GetFloat("masterVolume", 0.75f);
        float music = PlayerPrefs.GetFloat("musicVolume", 0.75f);
        float sfx = PlayerPrefs.GetFloat("soundFXVolume", 0.75f);

        audioMixer.SetFloat("masterVolume", Mathf.Log10(master) * 20f);
        audioMixer.SetFloat("musicVolume", Mathf.Log10(music) * 20f);
        audioMixer.SetFloat("soundFXVolume", Mathf.Log10(sfx) * 20f);
    }

    public void SetMaster(float value)
    {
        audioMixer.SetFloat("masterVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("masterVolume", value);
    }

    public void SetMusic(float value)
    {
        audioMixer.SetFloat("musicVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("musicVolume", value);
    }

    public void SetSFX(float value)
    {
        audioMixer.SetFloat("soundFXVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("soundFXVolume", value);
    }

    public void PlayMusic(AudioClip clip, float fadeTime = 1f)
    {
        StartCoroutine(FadeMusic(clip, fadeTime));
    }

    private IEnumerator FadeMusic(AudioClip newClip, float fadeTime)
    {
        if (musicSource.clip == newClip)
            yield break;

        float startVol = musicSource.volume;

        // Fade out old music
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0, t / fadeTime);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in new music
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0, startVol, t / fadeTime);
            yield return null;
        }
    }

    public void PlaySFX(string name)
    {
        SFXEntry entry = sfxClips.Find(x => x.name == name);
        if (entry != null)
            PlaySFXClip(entry.clip, entry.volume);
        else
            Debug.LogWarning($"SFX '{name}' not found in AudioManager.");
    }

    public void PlaySFXClip(AudioClip clip, float volume = 1f)
    {
        AudioSource src = Instantiate(soundFXSource, transform);
        src.clip = clip;
        src.volume = volume;
        src.outputAudioMixerGroup = soundFXSource.outputAudioMixerGroup;
        src.Play();
        Destroy(src.gameObject, clip.length + 0.05f);
    }
}

[System.Serializable]
public class SFXEntry
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
}