using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    AudioSource src;
    readonly System.Collections.Generic.Dictionary<string, AudioClip> toneCache = new System.Collections.Generic.Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
    }

    public void Play(string name)
    {
        var clip = Resources.Load<AudioClip>("Audio/" + name);
        if (clip != null)
        {
            src.PlayOneShot(clip);
            return;
        }
        // fallback: simple synthesized blips
        switch (name)
        {
            case "chop": src.PlayOneShot(Tone(120, 0.12f, 0.5f)); break;
            case "hit": src.PlayOneShot(Tone(90, 0.08f, 0.4f)); break;
            case "pickup": src.PlayOneShot(Tone(600, 0.08f, 0.3f)); break;
            case "sell": src.PlayOneShot(Tone(880, 0.15f, 0.4f)); break;
            case "buy": src.PlayOneShot(Tone(700, 0.12f, 0.4f)); break;
            case "error": src.PlayOneShot(Tone(150, 0.2f, 0.4f)); break;
            case "levelup": src.PlayOneShot(Tone(523, 0.1f, 0.4f)); break;
            case "door": src.PlayOneShot(Tone(160, 0.22f, 0.35f)); break;
        }
    }

    AudioClip Tone(float freq, float dur, float vol)
    {
        string key = $"{freq}_{dur}_{vol}";
        if (toneCache.TryGetValue(key, out var cached)) return cached;
        int sr = 22050;
        int len = Mathf.Max(1, Mathf.CeilToInt(sr * dur));
        var data = new float[len];
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / sr;
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * Mathf.Exp(-t * 20f) * vol;
        }
        var clip = AudioClip.Create("tone", data.Length, 1, sr, false);
        clip.SetData(data, 0);
        toneCache[key] = clip;
        return clip;
    }
}
