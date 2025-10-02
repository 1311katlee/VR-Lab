using UnityEngine;
using TMPro;

public class TurbidityDisplay : MonoBehaviour
{
    [Header("Target Jar")]
    [Tooltip("Assign the jar GameObject that has either JarReaction or pHJar component")]
    public GameObject targetJar;

    [Header("Display Settings")]
    public string prefix = "Turbidity: ";
    public string suffix = " NTU";
    public int decimalPlaces = 1;

    [Header("NTU Conversion (for normalized jars)")]
    [Tooltip("Initial turbidity of raw water in NTU (used only if jar reports normalized turbidity)")]
    public float initialTurbidityNTU = 100f;

    [Header("Text Component (Auto-detected if empty)")]
    public TextMeshPro textMesh;

    // Cached components
    private JarReaction alumJar;
    private pHJar phJar;

    void Start()
    {
        // Auto-detect TextMeshPro if not assigned
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
            if (textMesh == null)
            {
                Debug.LogError("[TurbidityDisplay] No TextMeshPro component found! Please add one or assign it in the inspector.");
                enabled = false;
                return;
            }
        }

        if (targetJar == null)
        {
            Debug.LogError("[TurbidityDisplay] No target jar assigned! Please assign a jar in the inspector.");
            enabled = false;
            return;
        }

        // Try to find JarReaction or pHJar
        alumJar = targetJar.GetComponent<JarReaction>();
        phJar = targetJar.GetComponent<pHJar>();

        if (alumJar == null && phJar == null)
        {
            Debug.LogError($"[TurbidityDisplay] Target jar '{targetJar.name}' has neither JarReaction nor pHJar component!");
            enabled = false;
            return;
        }

        string jarType = alumJar != null ? "JarReaction (alum)" : "pHJar";
        Debug.Log($"[TurbidityDisplay] Connected to {targetJar.name} ({jarType})");
    }

    void Update()
    {
        float displayValue = 0f;

        // If using new pHJar, call its NTU method directly
        if (phJar != null)
        {
            displayValue = phJar.GetTurbidityNTU();
        }
        else if (alumJar != null)
        {
            // JarReaction only reports normalized turbidity
            displayValue = alumJar.GetTurbidityNormalized() * initialTurbidityNTU;
        }

        string formatString = "F" + decimalPlaces;
        textMesh.text = prefix + displayValue.ToString(formatString) + suffix;
    }
}
