using UnityEngine;

public class RPMInputManager : MonoBehaviour
{
    public JarController[] jars; // Assign in inspector
    private int selectedJarIndex = 0;

    private string inputBuffer = "";

    void Update()
    {
        // Number key input
        for (KeyCode k = KeyCode.Alpha0; k <= KeyCode.Alpha9; k++)
        {
            if (Input.GetKeyDown(k))
            {
                inputBuffer += (k - KeyCode.Alpha0).ToString();
            }
        }

        // Enter to set RPM
        if (Input.GetKeyDown(KeyCode.Return) && inputBuffer.Length > 0)
        {
            if (int.TryParse(inputBuffer, out int rpm))
            {
                jars[selectedJarIndex].SetRPM(rpm);
            }
            inputBuffer = "";
        }

        // Tab to switch jar selection
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            selectedJarIndex = (selectedJarIndex + 1) % jars.Length;
            Debug.Log("Selected Jar: " + (selectedJarIndex + 1));
        }

        // Spacebar to start timer (example: 60 seconds)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jars[selectedJarIndex].StartMixing(60f); // you can modify duration as needed
        }
    }
}
