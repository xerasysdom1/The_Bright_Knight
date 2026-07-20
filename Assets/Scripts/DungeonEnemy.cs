using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class DungeonEnemy : MonoBehaviour
{
    DungeonRunManager manager;
    Transform target;
    Rigidbody body;
    Renderer[] enemyRenderers;
    Transform healthFill;
    Transform healthBarRoot;
    Material bodyMaterial;
    Color baseColor;
    int maxHealth;
    int currentHealth;
    int attackDamage;
    int lightbulbValue;
    float moveSpeed;
    float nextAttackTime;
    float stunUntil;
    float roomHalfWidth;
    float roomHalfDepth;
    bool isDefeated;

    public void Configure(DungeonRunManager newManager, GameObject player, int levelIndex, int dropValue,
        float halfWidth, float halfDepth, Material shadowMaterial, Material eyeMaterial, Material metalMaterial)
    {
        manager = newManager;
        target = player != null ? player.transform : null;
        lightbulbValue = Mathf.Max(1, dropValue);
        roomHalfWidth = halfWidth;
        roomHalfDepth = halfDepth;
        maxHealth = 2 + levelIndex;
        currentHealth = maxHealth;
        attackDamage = levelIndex >= 3 ? 3 : 2;
        moveSpeed = 1.65f + levelIndex * 0.18f;
        baseColor = new Color(0.13f + levelIndex * 0.025f, 0.08f, 0.19f + levelIndex * 0.04f, 1f);
        bodyMaterial = new Material(shadowMaterial);
        SetMaterialColor(bodyMaterial, baseColor);

        BuildVisuals(bodyMaterial, eyeMaterial, metalMaterial, levelIndex);

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.9f, 0f);
        capsule.height = 1.8f;
        capsule.radius = 0.46f;

        body = GetComponent<Rigidbody>();
        body.mass = 1.4f;
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        enemyRenderers = GetComponentsInChildren<Renderer>();
        UpdateHealthBar();
    }

    void FixedUpdate()
    {
        if (isDefeated || target == null || Time.time < stunUntil || IsGameFinished())
        {
            StopMoving();
            return;
        }

        Vector3 toPlayer = target.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance <= 1.45f)
        {
            StopMoving();
            FaceDirection(toPlayer);

            if (Time.time >= nextAttackTime)
                AttackPlayer();

            return;
        }

        Vector3 direction = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector3.zero;
        direction = AvoidObstacles(direction);
        Vector3 velocity = body.linearVelocity;
        velocity.x = direction.x * moveSpeed;
        velocity.z = direction.z * moveSpeed;
        body.linearVelocity = velocity;
        FaceDirection(direction);

        Vector3 position = body.position;
        position.x = Mathf.Clamp(position.x, -roomHalfWidth, roomHalfWidth);
        position.z = Mathf.Clamp(position.z, -roomHalfDepth, roomHalfDepth);
        if (position != body.position)
            body.position = position;
    }

    Vector3 AvoidObstacles(Vector3 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude <= 0.001f)
            return desiredDirection;

        Vector3 origin = transform.position + Vector3.up * 0.65f;
        if (!Physics.SphereCast(origin, 0.3f, desiredDirection, out RaycastHit hit, 0.85f, ~0, QueryTriggerInteraction.Ignore))
            return desiredDirection;

        if (hit.transform == target || hit.transform.IsChildOf(transform))
            return desiredDirection;

        float side = transform.position.x + transform.position.z >= 0f ? 1f : -1f;
        Vector3 tangent = Vector3.Cross(Vector3.up, desiredDirection) * side;
        return (desiredDirection * 0.25f + tangent * 0.75f).normalized;
    }

    void AttackPlayer()
    {
        nextAttackTime = Time.time + 1.15f;
        PlayerMovement movement = target.GetComponent<PlayerMovement>();
        if (movement != null && movement.IsParrying)
        {
            GameAudio.PlayParry();
            int counterDamage = movement.ParryCounterDamage;
            TakeDamage(counterDamage, target.position);
            stunUntil = Time.time + 0.75f;
            return;
        }

        PlayerVitals vitals = target.GetComponent<PlayerVitals>();
        if (vitals != null)
            vitals.TakeDamage(attackDamage);
    }

    public void TakeDamage(int amount, Vector3 sourcePosition)
    {
        if (isDefeated || amount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        stunUntil = Time.time + 0.18f;
        UpdateHealthBar();
        StartCoroutine(HitFlash());

        Vector3 knockback = transform.position - sourcePosition;
        knockback.y = 0f;
        if (body != null && knockback.sqrMagnitude > 0.001f)
            body.AddForce(knockback.normalized * 3.2f, ForceMode.VelocityChange);

        if (currentHealth <= 0)
        {
            Defeat();
        }
        else
        {
            GameAudio.PlayEnemyHit();
        }
    }

    void Defeat()
    {
        if (isDefeated)
            return;

        isDefeated = true;
        GameAudio.PlayEnemyDown();
        manager?.NotifyEnemyDefeated(transform.position, lightbulbValue);
        CreateDefeatBurst();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    IEnumerator HitFlash()
    {
        SetMaterialColor(bodyMaterial, new Color(0.95f, 0.28f, 0.45f, 1f));
        yield return new WaitForSeconds(0.1f);

        if (!isDefeated)
            SetMaterialColor(bodyMaterial, baseColor);
    }

    void BuildVisuals(Material shadowMaterial, Material eyeMaterial, Material metalMaterial, int levelIndex)
    {
        CreatePrimitive("Shadow Body", PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), new Vector3(0.78f, 0.86f, 0.78f), shadowMaterial);
        CreatePrimitive("Shadow Head", PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0f), new Vector3(0.72f, 0.62f, 0.72f), shadowMaterial);
        CreatePrimitive("Left Horn", PrimitiveType.Cylinder, new Vector3(-0.36f, 2.02f, 0f), new Vector3(0.1f, 0.32f, 0.1f), metalMaterial)
            .transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        CreatePrimitive("Right Horn", PrimitiveType.Cylinder, new Vector3(0.36f, 2.02f, 0f), new Vector3(0.1f, 0.32f, 0.1f), metalMaterial)
            .transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        CreatePrimitive("Left Eye", PrimitiveType.Sphere, new Vector3(-0.2f, 1.69f, 0.32f), Vector3.one * 0.13f, eyeMaterial);
        CreatePrimitive("Right Eye", PrimitiveType.Sphere, new Vector3(0.2f, 1.69f, 0.32f), Vector3.one * 0.13f, eyeMaterial);

        if (levelIndex >= 2)
        {
            CreatePrimitive("Left Claw", PrimitiveType.Cube, new Vector3(-0.6f, 0.92f, 0.15f), new Vector3(0.18f, 0.65f, 0.18f), metalMaterial)
                .transform.localRotation = Quaternion.Euler(18f, 0f, -18f);
            CreatePrimitive("Right Claw", PrimitiveType.Cube, new Vector3(0.6f, 0.92f, 0.15f), new Vector3(0.18f, 0.65f, 0.18f), metalMaterial)
                .transform.localRotation = Quaternion.Euler(18f, 0f, 18f);
        }

        healthBarRoot = new GameObject("Enemy Health Bar").transform;
        healthBarRoot.SetParent(transform, false);
        healthBarRoot.localPosition = new Vector3(0f, 2.45f, 0f);
        CreatePrimitive("Health Background", PrimitiveType.Cube, Vector3.zero, new Vector3(1.25f, 0.11f, 0.045f), CreateFlatMaterial(new Color(0.04f, 0.025f, 0.04f, 1f)), healthBarRoot);
        healthFill = CreatePrimitive("Health Fill", PrimitiveType.Cube, new Vector3(0f, 0f, -0.03f), new Vector3(1.15f, 0.065f, 0.035f),
            CreateFlatMaterial(new Color(0.86f, 0.08f, 0.16f, 1f)), healthBarRoot).transform;
    }

    GameObject CreatePrimitive(string objectName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material material, Transform parent = null)
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = objectName;
        primitive.transform.SetParent(parent != null ? parent : transform, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localScale = localScale;
        primitive.GetComponent<Renderer>().sharedMaterial = material;

        Collider primitiveCollider = primitive.GetComponent<Collider>();
        if (primitiveCollider != null)
            Destroy(primitiveCollider);

        return primitive;
    }

    void LateUpdate()
    {
        if (healthBarRoot != null && Camera.main != null)
            healthBarRoot.rotation = Camera.main.transform.rotation;
    }

    void UpdateHealthBar()
    {
        if (healthFill == null)
            return;

        float healthRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        healthFill.localScale = new Vector3(1.15f * healthRatio, 0.065f, 0.035f);
        healthFill.localPosition = new Vector3(-0.575f * (1f - healthRatio), 0f, -0.03f);
    }

    void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
    }

    void StopMoving()
    {
        if (body == null)
            return;

        Vector3 velocity = body.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        body.linearVelocity = velocity;
    }

    bool IsGameFinished()
    {
        return GameManager.Instance != null && GameManager.Instance.IsGameFinished;
    }

    void CreateDefeatBurst()
    {
        GameObject burstObject = new GameObject("Shadow Defeat Burst");
        burstObject.transform.position = transform.position + Vector3.up;
        ParticleSystem particles = burstObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = 0.55f;
        main.startSpeed = 2.4f;
        main.startSize = 0.18f;
        main.startColor = new Color(0.55f, 0.18f, 0.85f, 1f);
        main.maxParticles = 18;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
        particles.Play();
    }

    Material CreateFlatMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        SetMaterialColor(material, color);
        return material;
    }

    void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }
}
