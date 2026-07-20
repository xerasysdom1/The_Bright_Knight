using UnityEngine;

public class DungeonHazard : MonoBehaviour
{
    [SerializeField] int damage = 2;
    [SerializeField] float damageInterval = 0.9f;

    float nextDamageTime;

    void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    void TryDamage(Collider other)
    {
        if (Time.time < nextDamageTime || !other.CompareTag("Player"))
            return;

        PlayerVitals vitals = other.GetComponent<PlayerVitals>();
        if (vitals == null)
            vitals = other.GetComponentInParent<PlayerVitals>();
        if (vitals == null)
            return;

        nextDamageTime = Time.time + damageInterval;
        vitals.TakeDamage(damage);

        Rigidbody body = other.attachedRigidbody;
        if (body != null)
        {
            Vector3 away = other.transform.position - transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.001f)
                body.AddForce((away.normalized + Vector3.up * 0.25f) * 2.4f, ForceMode.VelocityChange);
        }
    }
}
