using UnityEngine;

public class StirrerRotator : MonoBehaviour
{
    public RPMManager rpmManager;  // Reference to the RPMManager script

    void Update()
    {
        if (rpmManager != null)
        {
            float rpm = rpmManager.currentRPM;
            float degreesPerSecond = rpm * 6f; // 360 degrees per minute = 6 degrees per second per RPM
            transform.Rotate(transform.up, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
