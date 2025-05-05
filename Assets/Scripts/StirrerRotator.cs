using UnityEngine;

public class StirrerRotator : MonoBehaviour
{
    public DialToRPM dialReference;  // Drag the dial GameObject here
    //public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        if (dialReference != null)
        {
            float rpm = dialReference.currentRPM;
            float degreesPerSecond = rpm * 6f; // 360 degrees * RPM / 60
            transform.Rotate(transform.up, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}