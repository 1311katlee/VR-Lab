using UnityEngine;
using TMPro;

public class RPMDisplayUI : MonoBehaviour
{
    public DialToRPM dial; // Assign your dial GameObject here
    public TextMeshProUGUI rpmText;

    void Update()
    {
        if (dial != null && rpmText != null)
        {
            rpmText.text = $"Stirrer RPM: {dial.currentRPM:F0}";
        }
    }
}
