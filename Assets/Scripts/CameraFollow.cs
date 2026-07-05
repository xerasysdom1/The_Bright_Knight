using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] string targetTag = "Player";
    [SerializeField] Vector3 offset = new Vector3(0f, 5f, -8f);
    [SerializeField] Vector3 lookOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] float positionSmoothTime = 0.28f;
    [SerializeField] float rotationSmoothSpeed = 5.5f;
    [SerializeField] float facingSmoothTime = 0.22f;
    [SerializeField] float lookAheadDistance = 0.85f;
    [SerializeField] bool snapToTargetOnStart = true;
    [SerializeField] bool lookAtTarget = true;
    [SerializeField] bool useTargetFacingDirection = true;
    [SerializeField] float followDistance = 6f;
    [SerializeField] float followHeight = 3.8f;
    [SerializeField] float sideOffset = 0f;
    [SerializeField] bool clampToRoomBounds = true;
    [SerializeField] Vector2 xBounds = new Vector2(-10.5f, 10.5f);
    [SerializeField] Vector2 yBounds = new Vector2(1.4f, 5.5f);
    [SerializeField] Vector2 zBounds = new Vector2(-10.5f, 10.5f);

    Vector3 positionVelocity;
    Vector3 smoothedFacingDirection = Vector3.forward;
    Vector3 facingVelocity;

    void Awake()
    {
        FindTarget();
    }

    void Start()
    {
        if (target == null || !snapToTargetOnStart)
            return;

        smoothedFacingDirection = GetTargetFacingDirection();
        transform.position = GetTargetPosition();
        UpdateLookRotation(true);
    }

    void LateUpdate()
    {
        if (target == null && !FindTarget())
            return;

        UpdateSmoothedFacingDirection();

        Vector3 targetPosition = GetTargetPosition();
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, positionSmoothTime);
        if (clampToRoomBounds)
        {
            transform.position = ClampToRoom(transform.position);
        }

        UpdateLookRotation(false);
    }

    bool FindTarget()
    {
        if (target != null)
            return true;

        GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
        if (targetObject == null)
            return false;

        target = targetObject.transform;
        return true;
    }

    Vector3 GetTargetPosition()
    {
        Vector3 targetPosition;

        if (useTargetFacingDirection)
        {
            targetPosition = target.position
                - smoothedFacingDirection * followDistance
                + target.right * sideOffset
                + Vector3.up * followHeight;
        }
        else
        {
            targetPosition = target.position + offset;
        }

        return clampToRoomBounds ? ClampToRoom(targetPosition) : targetPosition;
    }

    void UpdateSmoothedFacingDirection()
    {
        if (!useTargetFacingDirection)
            return;

        Vector3 targetFacing = GetTargetFacingDirection();
        smoothedFacingDirection = Vector3.SmoothDamp(smoothedFacingDirection, targetFacing, ref facingVelocity, facingSmoothTime);

        if (smoothedFacingDirection.sqrMagnitude <= 0.001f)
        {
            smoothedFacingDirection = targetFacing;
        }
        else
        {
            smoothedFacingDirection.Normalize();
        }
    }

    Vector3 GetTargetFacingDirection()
    {
        Vector3 facingDirection = target.forward;
        facingDirection.y = 0f;

        if (facingDirection.sqrMagnitude <= 0.001f)
            facingDirection = Vector3.forward;

        return facingDirection.normalized;
    }

    Vector3 ClampToRoom(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, xBounds.x, xBounds.y);
        position.y = Mathf.Clamp(position.y, yBounds.x, yBounds.y);
        position.z = Mathf.Clamp(position.z, zBounds.x, zBounds.y);
        return position;
    }

    public void ConfigureRoomBounds(float roomWidth, float roomDepth, float roomHeight, float padding)
    {
        float halfWidth = (roomWidth * 0.5f) - padding;
        float halfDepth = (roomDepth * 0.5f) - padding;

        clampToRoomBounds = true;
        xBounds = new Vector2(-halfWidth, halfWidth);
        yBounds = new Vector2(1.4f, roomHeight - 0.5f);
        zBounds = new Vector2(-halfDepth, halfDepth);
    }

    void UpdateLookRotation(bool instant)
    {
        if (!lookAtTarget)
            return;

        Vector3 lookPosition = target.position + lookOffset + smoothedFacingDirection * lookAheadDistance;
        Vector3 lookDirection = lookPosition - transform.position;
        if (lookDirection.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = instant
            ? targetRotation
            : Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}
