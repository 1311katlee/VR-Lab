using UnityEngine;
using TMPro;

public class RPMDisplayUI : MonoBehaviour
{
    public RPMManager rpmManager; // Drag your RPMManager object in the Inspector
    public TextMeshProUGUI rpmText;

    void Update()
    {
        if (rpmManager != null && rpmText != null)
        {
            float rpm = rpmManager.GetRPM();
            rpmText.text = $"Stirrer RPM: {rpm:F0}";
        }
    }
}
