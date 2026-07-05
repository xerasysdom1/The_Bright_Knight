using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] string targetTag = "Player";
    [SerializeField] Vector3 offset = new Vector3(0f, 5f, -8f);
    [SerializeField] Vector3 lookOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] float smoothSpeed = 8f;
    [SerializeField] bool snapToTargetOnStart = true;
    [SerializeField] bool lookAtTarget = true;

    void Awake()
    {
        FindTarget();
    }

    void Start()
    {
        if (target == null || !snapToTargetOnStart)
            return;

        transform.position = GetTargetPosition();
        UpdateLookRotation(true);
    }

    void LateUpdate()
    {
        if (target == null && !FindTarget())
            return;

        transform.position = Vector3.Lerp(transform.position, GetTargetPosition(), smoothSpeed * Time.deltaTime);
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
        return target.position + offset;
    }

    void UpdateLookRotation(bool instant)
    {
        if (!lookAtTarget)
            return;

        Vector3 lookPosition = target.position + lookOffset;
        Vector3 lookDirection = lookPosition - transform.position;
        if (lookDirection.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = instant
            ? targetRotation
            : Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }
}
