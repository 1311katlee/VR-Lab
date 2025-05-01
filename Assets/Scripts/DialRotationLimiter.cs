using UnityEngine;

public class DialRotationLimiter : MonoBehaviour
{
    public bool lockY = true;
    public bool lockZ = true;

    void LateUpdate()
    {
        Vector3 angles = transform.localEulerAngles;
        if (lockY) angles.y = 0f;
        if (lockZ) angles.z = 0f;
        transform.localEulerAngles = angles;
    }
}