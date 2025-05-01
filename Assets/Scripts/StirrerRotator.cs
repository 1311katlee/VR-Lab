using UnityEngine;

public class StirrerRotator : MonoBehaviour
{
    public float rpm = 0f; // Rotations per minute

    void Update()
    {
        float degreesPerSecond = rpm * 6f; // 360 degrees / 60 seconds
        transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime);
    }
}