using UnityEngine;

public class StirrerController : MonoBehaviour
{
    public DialController dial;

    void Update()
    {
        float rpm = dial.currentRPM;
        float degreesPerSecond = rpm * 6f; // 1 RPM = 6 degrees/sec
        transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime);
    }
}