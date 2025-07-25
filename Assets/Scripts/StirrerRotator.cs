using UnityEngine;

public class StirrerRotator : MonoBehaviour
{
    private float currentRPM = 0f;

    public void SetRPM(float newRPM)
    {
        currentRPM = Mathf.Max(0f, newRPM);
    }

    void Update()
    {
        float degreesPerSecond = currentRPM * 6f;
        transform.Rotate(transform.up, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
