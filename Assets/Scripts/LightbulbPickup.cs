using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class LightbulbPickup : MonoBehaviour
{
    DungeonRunManager manager;
    int value;
    float baseY;
    float phase;
    bool collected;

    public void Configure(DungeonRunManager newManager, int lightbulbValue)
    {
        manager = newManager;
        value = Mathf.Max(1, lightbulbValue);
        baseY = transform.localPosition.y;
        phase = Mathf.Abs(transform.position.x * 0.37f + transform.position.z * 0.19f);

        SphereCollider trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.75f;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, 95f * Time.deltaTime, Space.World);
        Vector3 position = transform.localPosition;
        position.y = baseY + Mathf.Sin(Time.time * 2.6f + phase) * 0.16f;
        transform.localPosition = position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player"))
            return;

        LightbulbWallet wallet = other.GetComponent<LightbulbWallet>();
        if (wallet == null)
            wallet = other.GetComponentInParent<LightbulbWallet>();
        if (wallet == null)
            return;

        collected = true;
        wallet.AddLightbulbs(value);
        manager?.NotifyLightbulbCollected(value);
        GameAudio.PlayPickup();
        CreatePickupBurst();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void CreatePickupBurst()
    {
        GameObject burstObject = new GameObject("Light Pickup Burst");
        burstObject.transform.position = transform.position;
        ParticleSystem particles = burstObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.25f;
        main.loop = false;
        main.startLifetime = 0.45f;
        main.startSpeed = 1.8f;
        main.startSize = 0.13f;
        main.startColor = new Color(1f, 0.82f, 0.2f, 1f);
        main.maxParticles = 14;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
        particles.Play();
    }
}
