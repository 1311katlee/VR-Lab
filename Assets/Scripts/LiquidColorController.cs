using UnityEngine;

public class LiquidController : MonoBehaviour
{
    public Material liquidMaterial; // Assign the liquid material
    public float minHeight = 0.1f;  // Minimum scale on Y
    public float maxHeight = 1f;    // Maximum scale on Y

    public float minRPM = 0f;
    public float maxRPM = 300f;

    public float rpmThreshold = 100f;
    public Color lowTurbidityColor = new Color(0.6f, 0.85f, 1f);  // Light blue
    public Color highTurbidityColor = new Color(0.4f, 0.4f, 0.4f); // Muddy gray

    public RPMManager rpmManager; // Assign this in the Inspector

    void Start()
    {
        if (rpmManager == null)
        {
            Debug.LogError("RPMManager reference is missing on LiquidController!");
        }
        if (liquidMaterial == null)
        {
            Debug.LogError("Liquid Material is not assigned!");
        }
    }

    void Update()
    {
        if (rpmManager != null)
        {
            float rpm = rpmManager.GetRPM();

            // Height change based on RPM
            float heightT = Mathf.InverseLerp(minRPM, maxRPM, rpm);
            float newHeight = Mathf.Lerp(minHeight, maxHeight, 1 - heightT);

            Vector3 newScale = transform.localScale;
            newScale.y = newHeight;
            transform.localScale = newScale;

            // Color change based on threshold
            if (liquidMaterial != null)
            {
                Color newColor = (rpm > rpmThreshold) ? lowTurbidityColor : highTurbidityColor;
                liquidMaterial.color = newColor;
            }
        }
    }
}
