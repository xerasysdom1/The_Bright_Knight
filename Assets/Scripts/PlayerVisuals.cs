using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] RuntimeAnimatorController animatorController;
    [SerializeField] Material knightMaterial;
    [SerializeField] string idleStateName = "Idle";
    [SerializeField] string walkStateName = "Walk";
    [SerializeField] string attackStateName = "Attack";
    [SerializeField] float walkThreshold = 0.05f;
    [SerializeField] float animationFadeTime = 0.12f;
    [SerializeField] float minWalkAnimationSpeed = 0.85f;
    [SerializeField] float maxWalkAnimationSpeed = 1.25f;
    [SerializeField] float parryBlendSpeed = 12f;
    [SerializeField] Vector3 leftUpperArmParryOffset = new Vector3(0f, 8f, 28f);
    [SerializeField] Vector3 leftForearmParryOffset = new Vector3(0f, -6f, 24f);
    [SerializeField] Vector3 leftHandParryOffset = new Vector3(0f, 0f, 10f);

    string currentStateName;
    float attackLockEndTime;
    float parryBlend;
    bool isParrying;
    Transform leftUpperArm;
    Transform leftForearm;
    Transform leftHand;

    void Awake()
    {
        EnsureAnimator();
        FindParryBones();
        ApplyKnightMaterial();
    }

    void Start()
    {
        PlayState(idleStateName, true);
    }

    public void SetMoveAmount(float moveAmount)
    {
        EnsureAnimator();

        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        if (Time.time < attackLockEndTime)
            return;

        float normalizedMoveAmount = Mathf.Clamp01(moveAmount);
        bool isWalking = normalizedMoveAmount > walkThreshold;
        PlayState(isWalking ? walkStateName : idleStateName, false);

        animator.speed = isWalking
            ? Mathf.Lerp(minWalkAnimationSpeed, maxWalkAnimationSpeed, normalizedMoveAmount)
            : 1f;
    }

    public void PlayAttack(float duration)
    {
        EnsureAnimator();

        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        attackLockEndTime = Time.time + duration;
        animator.speed = 1f;
        PlayState(attackStateName, false, true);
    }

    public void PlayJump()
    {
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    public void SetParrying(bool shouldParry)
    {
        isParrying = shouldParry;
    }

    void LateUpdate()
    {
        UpdateParryPose();
    }

    void EnsureAnimator()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        animator.applyRootMotion = false;

        if (animatorController == null)
        {
            animatorController = Resources.Load<RuntimeAnimatorController>("KnightController");
        }

        if (animatorController != null && animator.runtimeAnimatorController != animatorController)
        {
            animator.runtimeAnimatorController = animatorController;
        }

#if UNITY_EDITOR
        if (animator.avatar == null)
        {
            animator.avatar = LoadKnightAvatar();
        }
#endif
    }

    void PlayState(string stateName, bool instant, bool forceRestart = false)
    {
        if (animator == null || animator.runtimeAnimatorController == null || (!forceRestart && currentStateName == stateName))
            return;

        if (instant)
        {
            animator.Play(stateName, 0, 0f);
        }
        else
        {
            animator.CrossFadeInFixedTime(stateName, animationFadeTime);
        }

        currentStateName = stateName;
    }

    void FindParryBones()
    {
        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform childTransform in childTransforms)
        {
            if (childTransform.name == "Bip001 L UpperArm")
            {
                leftUpperArm = childTransform;
            }
            else if (childTransform.name == "Bip001 L Forearm")
            {
                leftForearm = childTransform;
            }
            else if (childTransform.name == "Bip001 L Hand")
            {
                leftHand = childTransform;
            }
        }

    }

    void UpdateParryPose()
    {
        parryBlend = Mathf.MoveTowards(parryBlend, isParrying ? 1f : 0f, parryBlendSpeed * Time.deltaTime);
        if (parryBlend <= 0f)
            return;

        ApplyParryRotation(leftUpperArm, leftUpperArmParryOffset);
        ApplyParryRotation(leftForearm, leftForearmParryOffset);
        ApplyParryRotation(leftHand, leftHandParryOffset);
    }

    void ApplyParryRotation(Transform bone, Vector3 offset)
    {
        if (bone == null)
            return;

        Quaternion currentRotation = bone.localRotation;
        Quaternion targetRotation = currentRotation * Quaternion.Euler(offset);
        bone.localRotation = Quaternion.Slerp(currentRotation, targetRotation, parryBlend);
    }

    void ApplyKnightMaterial()
    {
        if (knightMaterial == null)
        {
            knightMaterial = Resources.Load<Material>("KnightDemoMaterial");
        }

        if (knightMaterial == null)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer childRenderer in renderers)
        {
            Material[] materials = childRenderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = knightMaterial;
            }

            childRenderer.sharedMaterials = materials;
        }
    }

#if UNITY_EDITOR
    Avatar LoadKnightAvatar()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Toon_RTS_demo/models/ToonRTS_demo_Knight.FBX");
        foreach (Object asset in assets)
        {
            if (asset is Avatar avatar)
                return avatar;
        }

        return null;
    }
#endif
}
