using UnityEngine;
using TMPro;

public class AlumDoseController : MonoBehaviour
{
    public float alumDose = 0f;
    public float doseStep = 1f;
    public TextMeshProUGUI doseText; // <-- Assign the pH text here

    void Start()
    {
        UpdateDoseText();
    }

    public void IncreaseDose()
    {
        alumDose += doseStep;
        UpdateDoseText();
    }

    public void DecreaseDose()
    {
        alumDose = Mathf.Max(0, alumDose - doseStep);
        UpdateDoseText();
    }

    void UpdateDoseText()
    {
        if (doseText != null)
        {
            doseText.text = $"Alum Dose: {alumDose} mg/L";
        }
    }
}
