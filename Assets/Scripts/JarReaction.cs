// JarReaction.cs
// Attach to each jar GameObject. Assign RPMManager, TMP text, ParticleSystem, Renderer, sludge spawn point, and prefab.

using UnityEngine;
using TMPro;

public class JarReaction : MonoBehaviour
{
    [Header("State")]
    public float CurrentAlumMl = 0f;         // amount of alum in this jar (ml)
    public float CurrentRPM = 0f;            // current stir speed from your stirrer
    public float WaterVolumeMl = 1000f;      // default jar volume (1000 ml)

    [Header("Reaction params (tweak)")]
    public float growthRate = 0.6f;          // how fast floc grows per dose unit
    public float shearFactor = 0.01f;        // loss per rpm unit
    public float coalescenceRate = 0.2f;     // how flocs merge to grow
    public float settlingThreshold = 0.5f;   // flocSize above which settling happens faster

    [Header("Visuals")]
    public ParticleSystem flocParticleSystem; // particle system for visible flocs
    public Renderer waterRenderer;            // set material for water (transparent shader)
    public Transform sludgeSpawnPoint;        // where settled sludge appears
    public GameObject settledFlocPrefab;      // prefab for settled floc
    public TMP_Text volumeText;               // UI text to display alum amount

    [Header("References")]
    public RPMManager rpmManager;             // assign in Inspector

    [Header("Auto time scale")]
    public float simulationTimeScale = 1f;    // >1 to speed up test

    // Internal state
    public float flocSize = 0f;    // abstract measure of floc mass/size
    public float turbidity = 0f;   // 0..1 for UI and color
    private float settledMass = 0f;

    void Start()
    {
        if (flocParticleSystem == null)
            Debug.LogWarning("Assign a particle system for floc visuals.");
        if (waterRenderer == null)
            Debug.LogWarning("Assign a water renderer (material must support color / alpha).");

        UpdateVolumeText();
    }

    void Update()
    {
        if (rpmManager != null)
            CurrentRPM = rpmManager.GetRPM();

        float dt = Time.deltaTime * simulationTimeScale;
        SimulateReaction(dt);
        UpdateVisuals();
    }

    // Called when alum is added
    public void ReceiveAlum(float ml)
    {
        CurrentAlumMl += ml;
        float seed = ml * 0.01f; // instant microfloc seed proportional to dose
        flocSize += seed;
        UpdateVolumeText();
    }

    void SimulateReaction(float dt)
    {
        float availableDose = Mathf.Max(0f, CurrentAlumMl);
        float doseFactor = availableDose / (WaterVolumeMl / 1000f);

        float growth = growthRate * doseFactor * (1f + coalescenceRate * flocSize) * dt;
        float shearLoss = shearFactor * CurrentRPM * flocSize * dt;

        float settlingLoss = 0f;
        if (flocSize > settlingThreshold)
        {
            settlingLoss = (flocSize - settlingThreshold) * 0.1f * dt;
            SpawnSettledIfNeeded(settlingLoss);
        }

        flocSize += growth - shearLoss - settlingLoss;
        flocSize = Mathf.Max(0f, flocSize);

        float alumConsumed = growth * 0.5f;
        CurrentAlumMl = Mathf.Max(0f, CurrentAlumMl - alumConsumed);

        turbidity = Mathf.Clamp01(flocSize / 2f);
    }

    void SpawnSettledIfNeeded(float settledDelta)
    {
        if (settledDelta <= 0f || settledFlocPrefab == null || sludgeSpawnPoint == null) return;
        settledMass += settledDelta;

        if (settledMass > 0.05f)
        {
            Vector3 pos = sludgeSpawnPoint.position;
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0, 360f), 0f);
            GameObject g = Instantiate(settledFlocPrefab, pos + Random.insideUnitSphere * 0.02f, rot, sludgeSpawnPoint);
            float s = Mathf.Clamp01(Mathf.Log10(1f + settledMass) + 0.2f);
            g.transform.localScale = Vector3.one * (0.02f + s * 0.08f);
            settledMass = 0f;
        }
    }

    void UpdateVisuals()
    {
        // Water color based on turbidity
        if (waterRenderer != null)
        {
            Material mat = waterRenderer.material;
            Color clean = new Color(0.7f, 0.7f, 0.85f, 0.6f);
            Color cloudy = new Color(0.2f, 0.4f, 0.9f, 0.8f);
            Color col = Color.Lerp(clean, cloudy, turbidity);
            mat.SetColor("_BaseColor", col);
        }

        // Floc particle emission
        if (flocParticleSystem != null)
        {
            var em = flocParticleSystem.emission;
            float rate = Mathf.Clamp(flocSize * 50f, 0f, 500f);
            em.rateOverTime = rate;
        }
    }

    void UpdateVolumeText()
    {
        if (volumeText != null)
            volumeText.text = CurrentAlumMl.ToString("F1") + " mL";
    }

    public float GetTurbidityNormalized() { return turbidity; }
    public float GetFlocSize() { return flocSize; }
}
