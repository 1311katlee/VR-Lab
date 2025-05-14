using UnityEngine;
using TMPro;

public class NumpadInputManager : MonoBehaviour
{
    public TMP_InputField inputField;
    public RPMManager rpmManager; // assign in Inspector
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
            rpmManager.SetRPM(rpm);
            Debug.Log("RPM set to: " + rpm);
        }
        else
        {
            Debug.LogWarning("Invalid input.");
        }

        inputField.text = "";
    }

    // Optional: hook this up to a separate submit button if needed
    public void OnNumpadSubmit(string input)
    {
        if (float.TryParse(input, out float rpmValue))
        {
            rpmManager.SetRPM(rpmValue);
        }
    }
}
