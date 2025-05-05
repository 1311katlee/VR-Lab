using UnityEngine;

public class DialToRPM : MonoBehaviour
{
    public float minAngle = 0f;
    public float maxAngle = 270f;
    public float minRPM = 0f;
    public float maxRPM = 300f;
    public float snapIncrement = 50f; // RPM steps
    public float snapThreshold = 5f;  // How close it must be to snap

    [HideInInspector]
    public float currentRPM = 0f;

    void Update()
    {
        float rawAngle = transform.localEulerAngles.x;

        // Normalize angle
        if (rawAngle > 180f) rawAngle -= 360f;
        float clampedAngle = Mathf.Clamp(rawAngle, minAngle, maxAngle);

        // Convert angle to raw RPM
        float t = Mathf.InverseLerp(minAngle, maxAngle, clampedAngle);
        float targetRPM = Mathf.Lerp(minRPM, maxRPM, t);

        // Snap logic
        float snappedRPM = Mathf.Round(targetRPM / snapIncrement) * snapIncrement;

        // If close enough to a snap point, use snapped; else use raw RPM
        if (Mathf.Abs(snappedRPM - targetRPM) <= snapThreshold)
            currentRPM = snappedRPM;
        else
            currentRPM = targetRPM;

        Debug.Log("Final RPM: " + currentRPM);
    }
}