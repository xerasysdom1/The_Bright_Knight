using UnityEngine;

[DisallowMultipleComponent]
public class GameAudio : MonoBehaviour
{
    static GameAudio instance;

    AudioSource ambienceSource;
    AudioSource effectsSource;
    AudioClip attackClip;
    AudioClip enemyHitClip;
    AudioClip enemyDownClip;
    AudioClip pickupClip;
    AudioClip lockedClip;
    AudioClip unlockedClip;
    AudioClip hurtClip;
    AudioClip parryClip;
    AudioClip victoryClip;

    public static void EnsureExists()
    {
        if (instance != null)
            return;

        GameAudio foundAudio = FindAnyObjectByType<GameAudio>();
        if (foundAudio != null)
        {
            instance = foundAudio;
            return;
        }

        new GameObject("Procedural Game Audio").AddComponent<GameAudio>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ambienceSource = gameObject.AddComponent<AudioSource>();
        effectsSource = gameObject.AddComponent<AudioSource>();

        ambienceSource.loop = true;
        ambienceSource.playOnAwake = false;
        ambienceSource.spatialBlend = 0f;
        ambienceSource.volume = 0.55f;

        effectsSource.playOnAwake = false;
        effectsSource.spatialBlend = 0f;
        effectsSource.volume = 0.8f;

        ambienceSource.clip = CreateAmbienceClip();
        attackClip = CreateToneClip("Sword Swing", 185f, 0.13f, 0.18f, -95f, true);
        enemyHitClip = CreateToneClip("Shadow Hit", 120f, 0.16f, 0.22f, -35f, true);
        enemyDownClip = CreateToneClip("Shadow Defeated", 150f, 0.32f, 0.24f, -105f, true);
        pickupClip = CreateToneClip("Lightbulb Collected", 620f, 0.22f, 0.22f, 420f, false);
        lockedClip = CreateToneClip("Door Locked", 105f, 0.2f, 0.2f, -25f, true);
        unlockedClip = CreateToneClip("Door Unsealed", 280f, 0.42f, 0.2f, 360f, false);
        hurtClip = CreateToneClip("Knight Hurt", 92f, 0.2f, 0.22f, -40f, true);
        parryClip = CreateToneClip("Knight Parry", 760f, 0.12f, 0.18f, 260f, false);
        victoryClip = CreateVictoryClip();
    }

    void Start()
    {
        if (ambienceSource.clip != null)
            ambienceSource.Play();
    }

    public static void PlayAttack() => PlayEffect(instance != null ? instance.attackClip : null, 0.65f);
    public static void PlayEnemyHit() => PlayEffect(instance != null ? instance.enemyHitClip : null, 0.72f);
    public static void PlayEnemyDown() => PlayEffect(instance != null ? instance.enemyDownClip : null, 0.85f);
    public static void PlayPickup() => PlayEffect(instance != null ? instance.pickupClip : null, 0.8f);
    public static void PlayLocked() => PlayEffect(instance != null ? instance.lockedClip : null, 0.7f);
    public static void PlayUnlocked() => PlayEffect(instance != null ? instance.unlockedClip : null, 0.85f);
    public static void PlayHurt() => PlayEffect(instance != null ? instance.hurtClip : null, 0.72f);
    public static void PlayParry() => PlayEffect(instance != null ? instance.parryClip : null, 0.8f);
    public static void PlayVictory() => PlayEffect(instance != null ? instance.victoryClip : null, 0.95f);

    static void PlayEffect(AudioClip clip, float volume)
    {
        EnsureExists();
        if (instance == null || instance.effectsSource == null || clip == null)
            return;

        instance.effectsSource.PlayOneShot(clip, volume);
    }

    AudioClip CreateAmbienceClip()
    {
        const int sampleRate = 22050;
        const float duration = 12f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float[] bassNotes = { 55f, 65.41f, 49f, 73.42f };
        float[] bellNotes = { 220f, 261.63f, 196f, 293.66f, 246.94f, 220f, 164.81f, 196f };

        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            int bassIndex = Mathf.FloorToInt(time / 3f) % bassNotes.Length;
            int bellIndex = Mathf.FloorToInt(time / 1.5f) % bellNotes.Length;
            float bass = Mathf.Sin(time * bassNotes[bassIndex] * Mathf.PI * 2f) * 0.035f;
            float fifth = Mathf.Sin(time * bassNotes[bassIndex] * 1.5f * Mathf.PI * 2f) * 0.014f;
            float bellEnvelope = Mathf.Exp(-(time % 1.5f) * 2.8f);
            float bell = Mathf.Sin(time * bellNotes[bellIndex] * Mathf.PI * 2f) * bellEnvelope * 0.018f;
            float pulse = Mathf.Sin(time * 0.17f * Mathf.PI * 2f) * 0.006f;
            samples[i] = bass + fifth + bell + pulse;
        }

        AudioClip clip = AudioClip.Create("The Bright Knight - Dungeon Ambience", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip CreateToneClip(string clipName, float startFrequency, float duration, float volume, float frequencySweep, bool addNoise)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(clipName.GetHashCode());

        for (int i = 0; i < sampleCount; i++)
        {
            float normalizedTime = (float)i / sampleCount;
            float time = (float)i / sampleRate;
            float frequency = Mathf.Max(35f, startFrequency + frequencySweep * normalizedTime);
            float envelope = Mathf.Pow(1f - normalizedTime, 1.7f);
            float wave = Mathf.Sin(time * frequency * Mathf.PI * 2f);
            if (addNoise)
                wave += ((float)random.NextDouble() * 2f - 1f) * 0.34f;

            samples[i] = wave * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip CreateVictoryClip()
    {
        const int sampleRate = 22050;
        const float duration = 1.6f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float[] notes = { 261.63f, 329.63f, 392f, 523.25f };

        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            int noteIndex = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(time / 0.4f));
            float noteTime = time % 0.4f;
            float envelope = Mathf.Clamp01(noteTime / 0.025f) * Mathf.Pow(1f - Mathf.Clamp01(noteTime / 0.4f), 0.6f);
            samples[i] = (Mathf.Sin(time * notes[noteIndex] * Mathf.PI * 2f) +
                Mathf.Sin(time * notes[noteIndex] * 2f * Mathf.PI * 2f) * 0.24f) * envelope * 0.16f;
        }

        AudioClip clip = AudioClip.Create("Dungeon Restored", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
