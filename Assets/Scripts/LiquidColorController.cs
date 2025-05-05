using UnityEngine;

public class LiquidController : MonoBehaviour
{
    public Transform stirrer; // Assign the stirrer pivot
    public Material liquidMaterial; // Optional: assign the liquid material
    public float minHeight = 0.1f; // Minimum scale on Y
    public float maxHeight = 1f;   // Max scale on Y

    public float minRPM = 0f;
    public float maxRPM = 300f;  // Maximum RPM (no clamp here, we're allowing it to go higher than 26)

    public float rpmThreshold = 26f;  // Set the RPM threshold for color change
    public Color lowTurbidityColor = new Color(0.6f, 0.85f, 1f); // Light blue
    public Color highTurbidityColor = new Color(0.4f, 0.4f, 0.4f); // Muddy gray

    private DialToRPM rpmSource;

    void Start()
    {
        rpmSource = stirrer.GetComponent<DialToRPM>();
        if (liquidMaterial == null)
        {
            Debug.LogError("Liquid Material is not assigned!");
        }
    }

    void Update()
    {
        if (rpmSource != null)
        {
            float rpm = rpmSource.currentRPM;

            // Log RPM value to debug
            Debug.Log("Current RPM: " + rpm);

            // Simulate height change based on RPM, scaling height from minHeight to maxHeight
            float heightT = Mathf.InverseLerp(minRPM, maxRPM, rpm);
            Debug.Log("HeightT: " + heightT);  // Debugging output

            // Calculate new height (higher RPM = lower level)
            float newHeight = Mathf.Lerp(minHeight, maxHeight, 1 - heightT);
            Vector3 newScale = transform.localScale;
            newScale.y = newHeight;
            transform.localScale = newScale;

            // Change liquid color when RPM crosses the threshold
            if (liquidMaterial != null)
            {
                Color newColor;

                // Log the comparison result of RPM and the threshold
                Debug.Log("RPM Threshold: " + rpmThreshold);
                Debug.Log("Is RPM above threshold? " + (rpm > rpmThreshold)); // Debug the condition

                if (rpm > rpmThreshold) // If RPM exceeds threshold
                {
                    newColor = lowTurbidityColor; // Set color to low turbidity color
                    Debug.Log("Color Change: Low Turbidity");  // Debugging output
                }
                else
                {
                    newColor = highTurbidityColor; // Otherwise, set it to high turbidity color
                    Debug.Log("Color Change: High Turbidity");  // Debugging output
                }

                liquidMaterial.color = newColor;
                Debug.Log("New Color: " + newColor);  // Debugging output
            }
        }
        else
        {
            Debug.LogError("RPM Source (DialToRPM) not found on stirrer.");
        }
    }
}
