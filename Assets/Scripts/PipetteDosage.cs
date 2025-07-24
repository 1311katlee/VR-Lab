using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TMPro;

public class PipetteDosage : MonoBehaviour
{
    public float pipetteVolume = 0f;
    public float maxVolume = 10f;
    public float dosageRate = 1f; // mL per second
    public TMP_Text pipetteText;

    private bool isNearJar = false;
    private bool isNearAlumSource = false;
    private JarDosageReceiver currentJar;

    private XRBaseInteractor interactor;
    private bool isDispensing = false;

    private void Start()
    {
        UpdateText();
    }

    private void Update()
    {
        if (!isDispensing) return;

        float dosage = dosageRate * Time.deltaTime;

        // Refill when near alum source
        if (isNearAlumSource && pipetteVolume < maxVolume)
        {
            pipetteVolume += dosage;
            pipetteVolume = Mathf.Min(pipetteVolume, maxVolume);
            UpdateText();
        }

        // Dispense when near jar
        if (isNearJar && currentJar != null && pipetteVolume > 0f)
        {
            dosage = Mathf.Min(dosage, pipetteVolume);
            pipetteVolume -= dosage;
            currentJar.ReceiveAlum(dosage);
            UpdateText();
        }
    }

    private void UpdateText()
    {
        if (pipetteText != null)
        {
            pipetteText.text = pipetteVolume.ToString("F1") + " mL";
        }
    }

    public void BeginDispense(XRBaseInteractor interactor)
    {
        this.interactor = interactor;
        isDispensing = true;
    }

    public void EndDispense(XRBaseInteractor interactor)
    {
        isDispensing = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Jar"))
        {
            currentJar = other.GetComponent<JarDosageReceiver>();
            isNearJar = true;
        }
        else if (other.CompareTag("AlumJar")) // Make sure Alum bottle has this tag
        {
            isNearAlumSource = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Jar"))
        {
            isNearJar = false;
            currentJar = null;
        }
        else if (other.CompareTag("AlumJar"))
        {
            isNearAlumSource = false;
        }
    }
}
