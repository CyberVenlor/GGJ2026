using UnityEngine;

public class CameraLinearFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    public float positionGain = 6f;
    public bool useLocalOffset = false;
    public bool followX = true;
    public bool followY = true;
    public bool followZ = true;

    private void LateUpdate()
    {
        Transform followTarget = target != null ? target : transform.parent;
        if (followTarget == null)
        {
            return;
        }

        Vector3 desired = followTarget.position + (useLocalOffset ? followTarget.TransformVector(offset) : offset);
        Vector3 current = transform.position;

        if (!followX) desired.x = current.x;
        if (!followY) desired.y = current.y;
        if (!followZ) desired.z = current.z;

        // Linear control: v = K * error, x = x + v * dt
        Vector3 error = desired - current;
        Vector3 velocity = error * positionGain;
        transform.position = current + velocity * Time.deltaTime;
    }
}
