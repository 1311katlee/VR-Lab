using UnityEngine;
using TMPro;

public class NumpadInputManager : MonoBehaviour
{
    public TMP_InputField inputField;
    public DialToRPM dialToRPM;
    public float maxRPM = 300f;

    public void PressNumber(string number)
    {
        inputField.text += number;
    }

    public void PressDelete()
    {
        if (inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        }
    }

    public void PressEnter()
    {
        if (float.TryParse(inputField.text, out float rpm))
        {
            rpm = Mathf.Clamp(rpm, 0f, maxRPM);
            dialToRPM.currentRPM = rpm;
            Debug.Log("RPM set to: " + rpm);
        }
        else
        {
            Debug.LogWarning("Invalid input.");
        }

        inputField.text = "";
    }
}
