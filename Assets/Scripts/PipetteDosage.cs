using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PipetteDosage : MonoBehaviour
{
    public TextMeshPro text;
    public float dosageRate = 1f;
    public float maxDosage = 10f;
    public InputActionProperty triggerAction; // The right-hand trigger input

    private float currentDosage = 0f;
    private bool isInAlum = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("AlumJar")) // Tag your alum jar as "Alum"
        {
            isInAlum = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("AlumJar"))
        {
            isInAlum = false;
        }
    }

    void Update()
    {
        float triggerValue = triggerAction.action.ReadValue<float>();
        if (isInAlum && triggerValue > 0.1f)
        {
            currentDosage += dosageRate * Time.deltaTime;
            currentDosage = Mathf.Min(currentDosage, maxDosage);
            text.text = $"{currentDosage:F1} mL";
        }
    }
}
