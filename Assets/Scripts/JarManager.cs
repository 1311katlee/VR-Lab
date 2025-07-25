using UnityEngine;

public class JarManager : MonoBehaviour
{
    public JarController[] jars;
    private int selectedJarIndex = 0;

    void Update()
    {
        // Select jar using number keys (1–6)
        if (Input.GetKeyDown(KeyCode.Keypad1)) SelectJar(0);
        if (Input.GetKeyDown(KeyCode.Keypad2)) SelectJar(1);
        if (Input.GetKeyDown(KeyCode.Keypad3)) SelectJar(2);
        if (Input.GetKeyDown(KeyCode.Keypad4)) SelectJar(3);
        if (Input.GetKeyDown(KeyCode.Keypad5)) SelectJar(4);
        if (Input.GetKeyDown(KeyCode.Keypad6)) SelectJar(5);

        // The global RPM adjustment and timer start remains wherever it was before
    }

    void SelectJar(int index)
    {
        selectedJarIndex = index;

        // Optional: Highlight the selected jar (use a method in JarController)
        for (int i = 0; i < jars.Length; i++)
        {
            jars[i].SetHighlight(i == selectedJarIndex);
        }

        Debug.Log($"Selected Jar: {index + 1}");
    }

    public JarController GetSelectedJar()
    {
        return jars[selectedJarIndex];
    }
}
