using UnityEngine;

public class DialRotationLimiter : MonoBehaviour
{
    public float minAngle = 0f;
    public float maxAngle = 270f;
    public Vector3 rotationAxis = Vector3.right;

    public bool lockY = true;
    public bool lockZ = true;

    void LateUpdate()
    {
        Vector3 euler = transform.localEulerAngles;

        // Normalize angle to 0–360
        float angle = 0f;

        if (rotationAxis == Vector3.right)
        {
            angle = euler.x;
            if (angle > 180f) angle -= 360f;
            angle = Mathf.Clamp(angle, minAngle, maxAngle);
            euler.x = angle;
            if (lockY) euler.y = 0f;
            if (lockZ) euler.z = 0f;
        }
        else if (rotationAxis == Vector3.up)
        {
            angle = euler.y;
            if (angle > 180f) angle -= 360f;
            angle = Mathf.Clamp(angle, minAngle, maxAngle);
            euler.y = angle;
            if (lockY) euler.x = 0f;
            if (lockZ) euler.z = 0f;
        }
        else if (rotationAxis == Vector3.forward)
        {
            angle = euler.z;
            if (angle > 180f) angle -= 360f;
            angle = Mathf.Clamp(angle, minAngle, maxAngle);
            euler.z = angle;
            if (lockY) euler.x = 0f;
            if (lockZ) euler.y = 0f;
        }

        transform.localEulerAngles = euler;
    }
}
