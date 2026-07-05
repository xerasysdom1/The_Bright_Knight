using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float turnSpeed = 12f;
    [SerializeField] bool useCameraRelativeMovement = true;
    [SerializeField] bool faceMoveDirection = true;
    [SerializeField] Transform movementCamera;
    [SerializeField] PlayerVisuals playerVisuals;
    [SerializeField] float jumpForce = 3f;
    [SerializeField] float groundCheckDistance = 0.35f;
    [SerializeField] LayerMask groundLayers = ~0;
    [SerializeField] float attackDuration = 0.55f;
    [SerializeField] float attackCooldown = 0.2f;
    [SerializeField] float parryMoveSpeedMultiplier = 0.45f;

    Rigidbody rb;
    Collider playerCollider;

    Vector2 moveInput;
    float currentMoveSpeed;
    Coroutine speedBoostRoutine;
    bool jumpQueued;
    bool isParrying;
    float attackEndTime;
    float nextAttackTime;

    public bool IsAttacking => Time.time < attackEndTime;
    public bool IsParrying => isParrying;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        if (playerCollider == null)
        {
            playerCollider = GetComponentInChildren<Collider>();
        }

        currentMoveSpeed = moveSpeed;

        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (movementCamera == null && Camera.main != null)
        {
            movementCamera = Camera.main.transform;
        }

        if (playerVisuals == null)
        {
            playerVisuals = GetComponentInChildren<PlayerVisuals>();
        }

        if (playerVisuals == null)
        {
            playerVisuals = gameObject.AddComponent<PlayerVisuals>();
        }
    }

    public void ActivateSpeedBoost(float boostAmount, float duration)
    {
        if (speedBoostRoutine != null)
        {
            StopCoroutine(speedBoostRoutine);
        }

        speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(boostAmount, duration));
    }

    private IEnumerator SpeedBoostRoutine(float boostAmount, float duration)
    {
        currentMoveSpeed = boostAmount;
        yield return new WaitForSeconds(duration);
        currentMoveSpeed = moveSpeed;
        speedBoostRoutine = null;
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumpQueued = true;
        }
    }

    void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            TryAttack();
        }
    }

    void OnParry(InputValue value)
    {
        if (value.isPressed)
        {
            SetParrying(!isParrying);
        }
    }

    void FixedUpdate()
    {
        Vector3 moveDirection = GetMoveDirection();
        float speedMultiplier = isParrying ? parryMoveSpeedMultiplier : 1f;
        Vector3 movement = moveDirection * currentMoveSpeed * speedMultiplier;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = movement.x;
        velocity.z = movement.z;

        if (jumpQueued)
        {
            if (IsGrounded())
            {
                velocity.y = jumpForce;
                playerVisuals.PlayJump();
            }

            jumpQueued = false;
        }

        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;
        playerVisuals.SetMoveAmount(moveInput.magnitude);

        if (faceMoveDirection && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            Quaternion rotation = Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(rotation);
        }
    }

    Vector3 GetMoveDirection()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        if (inputDirection.sqrMagnitude <= 0.001f)
            return Vector3.zero;

        if (!useCameraRelativeMovement)
            return inputDirection.normalized;

        if (movementCamera == null && Camera.main != null)
        {
            movementCamera = Camera.main.transform;
        }

        if (movementCamera == null)
            return inputDirection.normalized;

        Vector3 cameraForward = movementCamera.forward;
        Vector3 cameraRight = movementCamera.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        return ((cameraForward * moveInput.y) + (cameraRight * moveInput.x)).normalized;
    }

    void TryAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        SetParrying(false);
        attackEndTime = Time.time + attackDuration;
        nextAttackTime = attackEndTime + attackCooldown;
        playerVisuals.PlayAttack(attackDuration);
    }

    void SetParrying(bool shouldParry)
    {
        if (isParrying == shouldParry)
            return;

        isParrying = shouldParry;
        playerVisuals.SetParrying(isParrying);
    }

    bool IsGrounded()
    {
        if (playerCollider == null)
        {
            return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, groundLayers, QueryTriggerInteraction.Ignore);
        }

        Bounds bounds = playerCollider.bounds;
        float rayDistance = bounds.extents.y + groundCheckDistance;
        return Physics.Raycast(bounds.center, Vector3.down, rayDistance, groundLayers, QueryTriggerInteraction.Ignore);
    }
}
