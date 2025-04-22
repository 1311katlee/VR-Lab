using UnityEngine;

public class DialController : MonoBehaviour
{
    public float sensitivity = 1.0f; // Controls how fast the dial turns
    public float minRPM = 0f;
    public float maxRPM = 300f;

    [HideInInspector]
    public float currentRPM = 0f;

    public bool isSelected = false;

    private float startYRotation;

    void Start()
    {
        startYRotation = transform.localEulerAngles.y;
    }

    void Update()
    {
        if (isSelected)
        {
            float input = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up, input * sensitivity);

            // Calculate the delta from the starting rotation
            float rawAngle = transform.localEulerAngles.y - startYRotation;
            if (rawAngle < 0) rawAngle += 360f;
            if (rawAngle > 180f) rawAngle -= 360f;

            float t = Mathf.InverseLerp(0f, 270f, Mathf.Clamp(rawAngle, 0f, 270f)); // Assuming max turn is 270°
            currentRPM = Mathf.Lerp(minRPM, maxRPM, t);
        }
    }
}
