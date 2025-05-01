using UnityEngine;

public class StirrerController : MonoBehaviour
{
    public DialToRPM dialReference;  // Drag the dial GameObject here
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        if (dialReference != null)
        {
            float rpm = dialReference.currentRPM;
            float degreesPerSecond = rpm * 6f; // 360 degrees * RPM / 60
            transform.Rotate(rotationAxis, degreesPerSecond * Time.deltaTime);
        }
    }
}