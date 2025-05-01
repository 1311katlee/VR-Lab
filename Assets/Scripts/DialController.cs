using UnityEngine;

public class DialController : MonoBehaviour
{
    public float minRPM = 0f;
    public float maxRPM = 300f;
    public StirrerController stirrer; // Assign this in Inspector

    void Update()
    {
        float yRotation = transform.localEulerAngles.y;
        if (yRotation > 180f) yRotation -= 360f; // Normalize angle

        // Clamp to expected dial range (0 to 270 degrees)
        float clampedY = Mathf.Clamp(yRotation, 0f, 270f);
        float t = clampedY / 270f;

        float rpm = Mathf.Lerp(minRPM, maxRPM, t);
        //stirrer.SetRPM(rpm);
    }
}