using UnityEngine;
using TMPro;

public class TurbidityDisplay : MonoBehaviour
{
    [Header("Target Jar")]
    [Tooltip("Assign the jar GameObject that has either JarReaction or pHJar component")]
    public GameObject targetJar;
    
    [Header("Display Settings")]
    public string prefix = "Turbidity: ";
    public string suffix = "%";
    public int decimalPlaces = 1;
    
    [Header("Text Component (Auto-detected if empty)")]
    public TextMeshPro textMesh;
    
    private JarReaction alumJar;
    private MonoBehaviour pHJarComponent;
    
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
        
        // Validate target jar
        if (targetJar == null)
        {
            Debug.LogError("[TurbidityDisplay] No target jar assigned! Please assign a jar in the inspector.");
            enabled = false;
            return;
        }
        
        // Try to find JarReaction component (alum jar)
        alumJar = targetJar.GetComponent<JarReaction>();
        
        // Try to find pHJar component using reflection (since we don't have the exact class)
        if (alumJar == null)
        {
            pHJarComponent = targetJar.GetComponent("pHJar") as MonoBehaviour;
        }
        
        // Check if we found either component
        if (alumJar == null && pHJarComponent == null)
        {
            Debug.LogError($"[TurbidityDisplay] Target jar '{targetJar.name}' has neither JarReaction nor pHJar component!");
            enabled = false;
            return;
        }
        
        // Log success
        string jarType = alumJar != null ? "JarReaction (alum)" : "pHJar";
        Debug.Log($"[TurbidityDisplay] Connected to {targetJar.name} ({jarType})");
    }
    
    void Update()
    {
        float turbidity = GetTurbidity();
        
        // Convert to percentage (turbidity is 0-1, display as 0-100%)
        float turbidityPercent = turbidity * 100f;
        
        // Format the string
        string formatString = "F" + decimalPlaces;
        textMesh.text = prefix + turbidityPercent.ToString(formatString) + suffix;
    }
    
    float GetTurbidity()
    {
        // Get from alum jar
        if (alumJar != null)
        {
            return alumJar.GetTurbidityNormalized();
        }
        
        // Get from pH jar using reflection
        if (pHJarComponent != null)
        {
            // Try to get the turbidity field/property
            var turbidityField = pHJarComponent.GetType().GetField("turbidity");
            if (turbidityField != null)
            {
                return (float)turbidityField.GetValue(pHJarComponent);
            }
            
            // Try GetTurbidityNormalized method
            var method = pHJarComponent.GetType().GetMethod("GetTurbidityNormalized");
            if (method != null)
            {
                return (float)method.Invoke(pHJarComponent, null);
            }
            
            Debug.LogWarning("[TurbidityDisplay] Could not find turbidity value on pHJar component!");
        }
        
        return 0f;
    }
}