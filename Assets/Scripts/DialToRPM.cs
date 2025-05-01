using UnityEngine;

public class DialToRPM : MonoBehaviour
{
    public float minAngle = 0f;
    public float maxAngle = 270f;
    public float minRPM = 0f;
    public float maxRPM = 300f;

    [HideInInspector]
    public float currentRPM = 0f;

    void Update()
    {
        float rawAngle = transform.localEulerAngles.x;

        // Handle wrap-around
        if (rawAngle > 180f) rawAngle -= 360f;

        // Clamp angle to valid dial range
        float clampedAngle = Mathf.Clamp(rawAngle, minAngle, maxAngle);

        // Map angle to RPM
        float t = Mathf.InverseLerp(minAngle, maxAngle, clampedAngle);
        currentRPM = Mathf.Lerp(minRPM, maxRPM, t);

        Debug.Log("Dial Angle: " + rawAngle + " -> RPM: " + currentRPM);
    }
}