using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(AudioSource))]
public class AmbientMusicManager : MonoBehaviour
{
    public enum MusicMode { Happy, Scary, Chase }

    [Header("References")]
    [Tooltip("Your Player’s Transform")]
    [SerializeField] private Transform player;
    [Tooltip("Optional: manually assign enemies; leave empty to auto-find all in scene")]
    [SerializeField] private List<Enemy> enemies;

    [Header("Detection Settings")]
    [Tooltip("Radius within which we switch to Scary (if not chasing)")]
    [SerializeField] private float secondaryDetectionRadius = 10f;

    [Header("Music Clips (1+ per list)")]
    public List<AudioClip> happyClips = new List<AudioClip>();
    public List<AudioClip> scaryClips = new List<AudioClip>();
    public List<AudioClip> chaseClips = new List<AudioClip>();

    [Header("Random SFX Clips (1+ for one-shots)")]
    [Tooltip("Will play one at random every interval")]
    public List<AudioClip> randomClips = new List<AudioClip>();
    [Tooltip("Min & Max seconds between random SFX")]
    [SerializeField] private Vector2 randomIntervalRange = new Vector2(60f, 120f);

    [Header("Cricket Ambient")]
    [Tooltip("Looped quiet cricket ambient noise")]
    public AudioClip cricketClip;
    [Range(0f, 1f)]
    [Tooltip("Volume for cricket ambient")]
    public float cricketVolume = 0.1f;

    [Header("Playback Settings")]
    [Tooltip("Minimum seconds before allowing a non-Chase mode switch")]
    [SerializeField] private float minPlayTime = 60f;
    [Tooltip("Seconds to cross-fade between Happy⇄Scary")]
    [SerializeField] private float crossfadeDuration = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    // Background music sources (A/B for crossfade)
    private AudioSource sourceA, sourceB;
    private AudioSource activeSource, fadeSource;

    // One-shot SFX source
    private AudioSource sfxSource;

    // Cricket ambient source
    private AudioSource cricketSource;

    private MusicMode currentMode, requestedMode;
    private float lastSwitchTime;
    private bool isTransitioning = false;

    void Awake()
    {
        if (enemies == null || enemies.Count == 0)
            enemies = FindObjectsOfType<Enemy>().ToList();

        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        foreach (var src in new[] { sourceA, sourceB })
        {
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
        }
        activeSource = sourceA;
        fadeSource = sourceB;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        cricketSource = gameObject.AddComponent<AudioSource>();
        cricketSource.loop = true;
        cricketSource.playOnAwake = false;
        cricketSource.clip = cricketClip;
        cricketSource.volume = cricketVolume;
    }

    void Start()
    {
        lastSwitchTime = Time.time - minPlayTime;
        currentMode = MusicMode.Happy;
        requestedMode = MusicMode.Happy;
        PlayRandomClip(currentMode);

        if (cricketClip != null)
            cricketSource.Play();

        StartCoroutine(RandomSFXLoop());
    }

    void Update()
    {
        bool anyChasing = enemies.Any(e => e.CurrentState == Enemy.State.Chasing);

        // Exit chase immediately when no enemies are chasing
        if (currentMode == MusicMode.Chase && !anyChasing)
        {
            StopAllCoroutines();
            sourceA.Stop();
            sourceB.Stop();

            var mode = DetermineMode();
            PlayRandomClip(mode);
            currentMode = mode;
            lastSwitchTime = Time.time;

            StartCoroutine(RandomSFXLoop());
        }

        // Immediate chase override
        if (anyChasing)
        {
            if (currentMode != MusicMode.Chase)
            {
                StopAllCoroutines();
                sourceA.Stop();
                sourceB.Stop();

                activeSource = sourceA;
                activeSource.clip = GetRandomClip(MusicMode.Chase);
                activeSource.volume = volume;
                activeSource.loop = true;
                activeSource.Play();

                currentMode = MusicMode.Chase;
                lastSwitchTime = Time.time;
                isTransitioning = false;

                StartCoroutine(RandomSFXLoop());
            }
            return;
        }

        // Happy/Scary logic
        requestedMode = DetermineMode();
        if (!isTransitioning
            && requestedMode != currentMode
            && Time.time - lastSwitchTime >= minPlayTime)
        {
            StartCoroutine(SwitchMode(requestedMode));
        }
    }

    private MusicMode DetermineMode()
    {
        foreach (var e in enemies)
            if (Vector3.Distance(player.position, e.transform.position) <= secondaryDetectionRadius)
                return MusicMode.Scary;
        return MusicMode.Happy;
    }

    private IEnumerator SwitchMode(MusicMode newMode)
    {
        isTransitioning = true;
        fadeSource.clip = GetRandomClip(newMode);
        fadeSource.time = 0f;
        fadeSource.Play();

        float t = 0f;
        while (t < crossfadeDuration)
        {
            t += Time.deltaTime;
            float f = t / crossfadeDuration;
            activeSource.volume = Mathf.Lerp(volume, 0f, f);
            fadeSource.volume = Mathf.Lerp(0f, volume, f);
            yield return null;
        }

        activeSource.Stop();
        var tmp = activeSource;
        activeSource = fadeSource;
        fadeSource = tmp;

        currentMode = newMode;
        lastSwitchTime = Time.time;
        isTransitioning = false;
    }

    private void PlayRandomClip(MusicMode mode)
    {
        activeSource.clip = GetRandomClip(mode);
        activeSource.time = 0f;
        activeSource.volume = volume;
        activeSource.loop = true;
        activeSource.Play();
    }

    private AudioClip GetRandomClip(MusicMode mode)
    {
        var list = mode == MusicMode.Scary ? scaryClips
                 : mode == MusicMode.Chase ? chaseClips
                 : happyClips;
        return (list != null && list.Count > 0)
             ? list[Random.Range(0, list.Count)]
             : null;
    }

    private IEnumerator RandomSFXLoop()
    {
        while (true)
        {
            float delay = Random.Range(randomIntervalRange.x, randomIntervalRange.y);
            yield return new WaitForSeconds(delay);

            if (randomClips.Count > 0)
            {
                var clip = randomClips[Random.Range(0, randomClips.Count)];
                sfxSource.PlayOneShot(clip);
            }
        }
    }
}
