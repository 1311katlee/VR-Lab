// JarReaction.cs
// Enhanced with realistic jar test phases and particle behavior

using UnityEngine;
using TMPro;

public class JarReaction : MonoBehaviour
{
    [Header("State")]
    public float CurrentAlumMl = 0f;         // amount of alum in this jar (ml)
    public float CurrentRPM = 0f;            // current stir speed from your stirrer
    public float WaterVolumeMl = 1000f;      // default jar volume (1000 ml)

    [Header("Jar Test Parameters")]
    public float minimumAlumForFlocculation = 5f; // ml minimum needed
    public float rapidMixMinRPM = 80f;
    public float rapidMixMaxRPM = 120f;
    public float slowMixMinRPM = 20f;
    public float slowMixMaxRPM = 50f;
    public float rapidMixDuration = 120f;    // 2 minutes
    public float slowMixDuration = 1200f;    // 20 minutes

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

    // Jar Test Phase Tracking
    public enum JarTestPhase { Idle, RapidMix, SlowMix, Settling }
    private JarTestPhase currentPhase = JarTestPhase.Idle;
    private float phaseStartTime;
    private bool hasSufficientChemical = false;

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
        
        UpdateJarTestPhase();
        SimulateReaction(dt);
        UpdateVisuals();
    }

    // Called when alum is added via pipette
    public void ReceiveAlum(float ml)
    {
        if (ml <= 0f) return;
        
        float previousAlum = CurrentAlumMl;
        CurrentAlumMl += ml;
        flocSize += ml * 0.01f;
        
        Debug.Log($"[JarTest] Added {ml:F1}mL alum. Total: {CurrentAlumMl:F1}mL");
        
        // Check if we just crossed the threshold
        if (previousAlum < minimumAlumForFlocculation && CurrentAlumMl >= minimumAlumForFlocculation)
        {
            hasSufficientChemical = true;
            Debug.Log($"[JarTest] Sufficient chemical added ({CurrentAlumMl:F1}mL) - ready for testing");
        }
        
        UpdateVolumeText();
    }

    void UpdateJarTestPhase()
    {
        hasSufficientChemical = CurrentAlumMl >= minimumAlumForFlocculation;
        
        if (!hasSufficientChemical)
        {
            if (currentPhase != JarTestPhase.Idle)
            {
                currentPhase = JarTestPhase.Idle;
                Debug.Log("[JarTest] Insufficient chemical - returning to Idle phase");
            }
            return;
        }
        
        float phaseTime = Time.time - phaseStartTime;
        
        switch (currentPhase)
        {
            case JarTestPhase.Idle:
                // Start rapid mix when stirring at appropriate speed
                if (CurrentRPM >= rapidMixMinRPM && CurrentRPM <= rapidMixMaxRPM)
                {
                    StartPhase(JarTestPhase.RapidMix);
                }
                break;
                
            case JarTestPhase.RapidMix:
                // Continue rapid mix for specified duration or if speed changes
                if (phaseTime >= rapidMixDuration || CurrentRPM < rapidMixMinRPM)
                {
                    if (CurrentRPM >= slowMixMinRPM && CurrentRPM <= slowMixMaxRPM)
                    {
                        StartPhase(JarTestPhase.SlowMix);
                    }
                    else if (CurrentRPM < slowMixMinRPM)
                    {
                        StartPhase(JarTestPhase.Settling);
                    }
                }
                break;
                
            case JarTestPhase.SlowMix:
                // Continue slow mix for specified duration or if stirring stops
                if (phaseTime >= slowMixDuration || CurrentRPM < slowMixMinRPM)
                {
                    StartPhase(JarTestPhase.Settling);
                }
                // Return to rapid mix if speed increases significantly
                else if (CurrentRPM > slowMixMaxRPM)
                {
                    StartPhase(JarTestPhase.RapidMix);
                }
                break;
                
            case JarTestPhase.Settling:
                // Return to appropriate mix phase if stirring resumes
                if (CurrentRPM >= rapidMixMinRPM && CurrentRPM <= rapidMixMaxRPM)
                {
                    StartPhase(JarTestPhase.RapidMix);
                }
                else if (CurrentRPM >= slowMixMinRPM && CurrentRPM <= slowMixMaxRPM)
                {
                    StartPhase(JarTestPhase.SlowMix);
                }
                break;
        }
    }

    void StartPhase(JarTestPhase newPhase)
    {
        currentPhase = newPhase;
        phaseStartTime = Time.time;
        Debug.Log($"[JarTest] Started {newPhase} phase at RPM: {CurrentRPM:F1}");
    }

    void SimulateReaction(float dt)
    {
        if (!hasSufficientChemical)
        {
            // Minimal reaction without sufficient chemical
            flocSize = Mathf.Max(0f, flocSize - flocSize * 0.1f * dt); // Slow decay
            turbidity = Mathf.Clamp01(flocSize / 2f);
            return;
        }

        float availableDose = Mathf.Max(0f, CurrentAlumMl);
        float doseFactor = availableDose / (WaterVolumeMl / 1000f);

        // Phase-specific reaction rates
        float phaseGrowthMultiplier = 1f;
        float phaseShearMultiplier = 1f;

        switch (currentPhase)
        {
            case JarTestPhase.Idle:
                phaseGrowthMultiplier = 0.1f; // Very slow growth when not mixing
                phaseShearMultiplier = 0f;    // No shear when not mixing
                break;
                
            case JarTestPhase.RapidMix:
                phaseGrowthMultiplier = 2f;   // Enhanced growth during rapid mix
                phaseShearMultiplier = 2f;    // Higher shear breaks up large flocs
                break;
                
            case JarTestPhase.SlowMix:
                phaseGrowthMultiplier = 1.5f; // Good growth conditions
                phaseShearMultiplier = 0.5f;  // Lower shear allows floc formation
                break;
                
            case JarTestPhase.Settling:
                phaseGrowthMultiplier = 0.2f; // Minimal growth when settling
                phaseShearMultiplier = 0f;    // No shear during settling
                break;
        }

        float growth = growthRate * doseFactor * (1f + coalescenceRate * flocSize) * dt * phaseGrowthMultiplier;
        float shearLoss = shearFactor * CurrentRPM * flocSize * dt * phaseShearMultiplier;

        float settlingLoss = 0f;
        if (flocSize > settlingThreshold)
        {
            // Enhanced settling during settling phase
            float settlingMultiplier = (currentPhase == JarTestPhase.Settling) ? 3f : 1f;
            settlingLoss = (flocSize - settlingThreshold) * 0.1f * dt * settlingMultiplier;
            SpawnSettledIfNeeded(settlingLoss);
        }

        flocSize += growth - shearLoss - settlingLoss;
        flocSize = Mathf.Max(0f, flocSize);

        // Chemical consumption during rapid mix
        if (currentPhase == JarTestPhase.RapidMix && CurrentRPM > 100f)
        {
            float alumConsumed = growth * 0.5f;
            CurrentAlumMl = Mathf.Max(0f, CurrentAlumMl - alumConsumed);
        }

        turbidity = Mathf.Clamp01(flocSize / 2f);
        UpdateVolumeText();
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

        // Phase-specific particle behavior
        if (flocParticleSystem != null)
        {
            var em = flocParticleSystem.emission;
            var main = flocParticleSystem.main;
            
            if (!hasSufficientChemical)
            {
                // No particles without sufficient chemical
                em.rateOverTime = 0f;
                return;
            }

            float baseRate = 0f;
            float lifetime = 1f;

            switch (currentPhase)
            {
                case JarTestPhase.Idle:
                    baseRate = 0f; // No particles when idle
                    lifetime = 1f;
                    break;
                    
                case JarTestPhase.RapidMix:
                    // Small, fast particles during rapid mix
                    baseRate = flocSize * 30f + (CurrentRPM - rapidMixMinRPM) * 2f;
                    lifetime = 0.5f;
                    break;
                    
                case JarTestPhase.SlowMix:
                    // Medium flocs during slow mix
                    baseRate = flocSize * 40f + CurrentRPM * 1f;
                    lifetime = 3f;
                    break;
                    
                case JarTestPhase.Settling:
                    // Large, slow-settling particles
                    baseRate = flocSize * 15f;
                    lifetime = 8f;
                    break;
            }

            em.rateOverTime = Mathf.Clamp(baseRate, 0f, 500f);
            main.startLifetime = lifetime;

            Debug.Log($"[JarTest] Phase: {currentPhase}, RPM: {CurrentRPM:F1}, " +
                     $"Alum: {CurrentAlumMl:F1}mL, FlocSize: {flocSize:F2}, " +
                     $"ParticleRate: {em.rateOverTime.constant:F1}, Alive: {flocParticleSystem.particleCount}");
        }
        
        UpdateVolumeText();
    }

    void UpdateVolumeText()
    {
        if (volumeText != null)
            volumeText.text = CurrentAlumMl.ToString("F1") + " mL";
    }

    public float GetTurbidityNormalized() { return turbidity; }
    public float GetFlocSize() { return flocSize; }
    public JarTestPhase GetCurrentPhase() { return currentPhase; }
    public bool HasSufficientChemical() { return hasSufficientChemical; }
}