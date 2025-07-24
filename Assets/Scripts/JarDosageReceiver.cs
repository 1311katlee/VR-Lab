using UnityEngine;
using TMPro;

public class JarDosageReceiver : MonoBehaviour
{
    public float receivedAlumVolume = 0f;
    public TMP_Text volumeText;

    public void ReceiveAlum(float amount)
    {
        receivedAlumVolume += amount;
        UpdateText();
    }

    private void UpdateText()
    {
        if (volumeText != null)
        {
            volumeText.text = receivedAlumVolume.ToString("F1") + " mL";
        }
    }
}
