using UnityEngine;

public class DialController : MonoBehaviour
{
    public float sensitivity = 1.0f; // How fast RPM increases with scroll
    public float minRPM = 0f;
    public float maxRPM = 300f;

    [HideInInspector]
    public float currentRPM = 0f;

    private float startYRotation;

    void Start()
    {
        startYRotation = transform.localEulerAngles.y;
    }

    public void AdjustDial(float scrollInput)
    {
        // Update RPM based on scroll direction
        currentRPM += scrollInput * sensitivity;
        currentRPM = Mathf.Clamp(currentRPM, minRPM, maxRPM);

        // Rotate the dial based on RPM (e.g., map RPM to 270 degrees of dial)
        float t = Mathf.InverseLerp(minRPM, maxRPM, currentRPM);
        float targetRotation = Mathf.Lerp(0f, 270f, t);
        transform.localEulerAngles = new Vector3(0f, startYRotation + targetRotation, 0f);

        Debug.Log("Dial RPM: " + currentRPM);
    }
}
