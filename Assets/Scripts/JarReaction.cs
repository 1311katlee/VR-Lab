// JarReaction.cs
// Alum-only jar test with gated behavior: no visible changes until proper mixing occurs

using UnityEngine;
using TMPro;

[RequireComponent(typeof(MeshRenderer))]
public class JarReaction : MonoBehaviour
{
    [Header("State")]
    public float CurrentAlumMl = 0f;         // alum in this jar (ml)
    public float CurrentRPM = 0f;            // current stir speed
    public float WaterVolumeMl = 1000f;      // jar volume (ml)

    [Header("Jar Test Parameters")]
    public float minimumAlumForFlocculation = 5f; // ml threshold
    public float rapidMixMinRPM = 80f;
    public float rapidMixMaxRPM = 120f;
    public float slowMixMinRPM = 20f;
    public float slowMixMaxRPM = 50f;
    public float rapidMixDuration = 120f;    // 2 minutes
    public float slowMixDuration = 1200f;    // 20 minutes
    public float minimumRapidMixTime = 10f;  // Must mix at least this long before reactions start

    [Header("Reaction params (tweak)")]
    public float growthRate = 0.6f;          // base growth rate (optimal pH assumed)
    public float shearFactor = 0.01f;        // break-up due to shear
    public float coalescenceRate = 0.2f;     // merging factor
    public float settlingThreshold = 0.5f;   // above this, settling accelerates

    [Header("Visuals")]
    public ParticleSystem flocParticleSystem; // particle system for flocs
    public Renderer waterRenderer;            // water material (transparent)
    public Transform sludgeSpawnPoint;        // where settled sludge appears
    public GameObject settledFlocPrefab;      // prefab for settled floc
    public TMP_Text volumeText;               // UI for alum volume

    [Header("Particle Rendering (optional)")]
    public Mesh sphereMeshOverride;           // assign a Sphere mesh if you want Mesh mode

    [Header("References")]
    public RPMManager rpmManager;             // assign in Inspector

    [Header("Auto time scale")]
    public float simulationTimeScale = 1f;    // >1 to speed up test

    // Internal state
    public float flocSize = 0f;    // abstract floc mass/size
    public float turbidity = 0f;   // 0..1 for visuals
    private float settledMass = 0f;

    // Jar Test Phase Tracking
    public enum JarTestPhase { Idle, RapidMix, SlowMix, Settling }
    private JarTestPhase currentPhase = JarTestPhase.Idle;
    private float phaseStartTime;
    private bool hasSufficientChemical = false;
    private bool hasBeenMixedProperly = false; // NEW: tracks if rapid mix occurred
    private float totalRapidMixTime = 0f;      // NEW: accumulates rapid mix time

    // --- Particle constants for this version ---
    private const float UNIFORM_PARTICLE_SIZE = 0.03f;  // Small
    private const float MIN_PLAY_RATE = 1f;              // threshold to start/stop system

    void Awake()
    {
        if (flocParticleSystem != null)
        {
            var psMain = flocParticleSystem.main;
            psMain.playOnAwake = false;
            psMain.prewarm = false;
            psMain.startSize = UNIFORM_PARTICLE_SIZE;
            psMain.startLifetime = 2f;

            var emission = flocParticleSystem.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            var psRenderer = flocParticleSystem.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                if (sphereMeshOverride != null)
                {
                    psRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                    psRenderer.mesh = sphereMeshOverride;
                }
                else
                {
                    psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                    psRenderer.mesh = null;
                }
                psRenderer.alignment = ParticleSystemRenderSpace.View;
                psRenderer.normalDirection = 1f;
            }

            flocParticleSystem.Clear(true);
            flocParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void Start()
    {
        UpdateVolumeText();
    }

    void Update()
    {
        if (rpmManager != null)
            CurrentRPM = rpmManager.GetRPM();

        float dt = Time.deltaTime * simulationTimeScale;

        UpdateJarTestPhase(dt);
        SimulateReaction(dt);
        UpdateVisuals();
    }

    public void ReceiveAlum(float ml)
    {
        if (ml <= 0f) return;

        float previousAlum = CurrentAlumMl;
        CurrentAlumMl += ml;
        
        // Only add initial seed AFTER proper mixing occurs
        // For now, just log the addition
        Debug.Log($"[JarTest] Added {ml:F1}mL alum. Total: {CurrentAlumMl:F1}mL (no reaction until mixed)");

        if (previousAlum < minimumAlumForFlocculation && CurrentAlumMl >= minimumAlumForFlocculation)
        {
            hasSufficientChemical = true;
            Debug.Log($"[JarTest] Sufficient chemical added ({CurrentAlumMl:F1}mL) - BEGIN RAPID MIX to start reaction");
        }

        UpdateVolumeText();
    }

    void UpdateJarTestPhase(float dt)
    {
        hasSufficientChemical = CurrentAlumMl >= minimumAlumForFlocculation;

        if (!hasSufficientChemical)
        {
            if (currentPhase != JarTestPhase.Idle)
            {
                currentPhase = JarTestPhase.Idle;
                hasBeenMixedProperly = false;
                totalRapidMixTime = 0f;
                Debug.Log("[JarTest] Insufficient chemical - returning to Idle phase");
            }
            return;
        }

        float phaseTime = Time.time - phaseStartTime;

        switch (currentPhase)
        {
            case JarTestPhase.Idle:
                if (CurrentRPM >= rapidMixMinRPM && CurrentRPM <= rapidMixMaxRPM)
                    StartPhase(JarTestPhase.RapidMix);
                break;

            case JarTestPhase.RapidMix:
                // Accumulate rapid mix time
                if (CurrentRPM >= rapidMixMinRPM && CurrentRPM <= rapidMixMaxRPM)
                {
                    totalRapidMixTime += dt;
                    
                    // Check if we've mixed long enough to activate reactions
                    if (!hasBeenMixedProperly && totalRapidMixTime >= minimumRapidMixTime)
                    {
                        hasBeenMixedProperly = true;
                        // Initialize floc with the alum that was added
                        flocSize = CurrentAlumMl * 0.01f;
                        Debug.Log($"[JarTest] Rapid mix complete ({totalRapidMixTime:F1}s) - REACTIONS ACTIVATED");
                    }
                }
                
                if (phaseTime >= rapidMixDuration || CurrentRPM < rapidMixMinRPM)
                {
                    if (CurrentRPM >= slowMixMinRPM && CurrentRPM <= slowMixMaxRPM)
                        StartPhase(JarTestPhase.SlowMix);
                    else if (CurrentRPM < slowMixMinRPM)
                        StartPhase(JarTestPhase.Settling);
                }
                break;

            case JarTestPhase.SlowMix:
                if (phaseTime >= slowMixDuration || CurrentRPM < slowMixMinRPM)
                    StartPhase(JarTestPhase.Settling);
                else if (CurrentRPM > slowMixMaxRPM)
                    StartPhase(JarTestPhase.RapidMix);
                break;

            case JarTestPhase.Settling:
                if (CurrentRPM >= rapidMixMinRPM && CurrentRPM <= rapidMixMaxRPM)
                    StartPhase(JarTestPhase.RapidMix);
                else if (CurrentRPM >= slowMixMinRPM && CurrentRPM <= slowMixMaxRPM)
                    StartPhase(JarTestPhase.SlowMix);
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
        // GATE: No reactions until proper rapid mixing has occurred
        if (!hasBeenMixedProperly)
        {
            flocSize = 0f;
            turbidity = 0f;
            return;
        }

        if (!hasSufficientChemical)
        {
            flocSize = Mathf.Max(0f, flocSize - flocSize * 0.1f * dt);
            turbidity = Mathf.Clamp01(flocSize / 2f);
            return;
        }

        float availableDose = Mathf.Max(0f, CurrentAlumMl);
        float doseFactor = availableDose / (WaterVolumeMl / 1000f);

        // Phase multipliers
        float phaseGrowthMultiplier = 1f;
        float phaseShearMultiplier = 1f;

        switch (currentPhase)
        {
            case JarTestPhase.Idle:
                phaseGrowthMultiplier = 0.1f;
                phaseShearMultiplier = 0f;
                break;

            case JarTestPhase.RapidMix:
                phaseGrowthMultiplier = 2f;
                phaseShearMultiplier = 2f;
                break;

            case JarTestPhase.SlowMix:
                phaseGrowthMultiplier = 1.5f;
                phaseShearMultiplier = 0.5f;
                break;

            case JarTestPhase.Settling:
                phaseGrowthMultiplier = 0.2f;
                phaseShearMultiplier = 0f;
                break;
        }

        float growth = growthRate * doseFactor * (1f + coalescenceRate * flocSize) * dt * phaseGrowthMultiplier;
        float shearLoss = shearFactor * Mathf.Max(0f, CurrentRPM) * flocSize * dt * phaseShearMultiplier;

        float settlingLoss = 0f;
        if (flocSize > settlingThreshold)
        {
            float settlingMultiplier = (currentPhase == JarTestPhase.Settling) ? 3f : 1f;
            settlingLoss = (flocSize - settlingThreshold) * 0.1f * dt * settlingMultiplier;
            
            // GATE: Only spawn sludge if reactions are active
            SpawnSettledIfNeeded(settlingLoss);
        }

        flocSize += growth - shearLoss - settlingLoss;
        flocSize = Mathf.Max(0f, flocSize);

        if (currentPhase == JarTestPhase.RapidMix && CurrentRPM > 100f)
        {
            float alumConsumed = growth * 0.5f;
            CurrentAlumMl = Mathf.Max(0f, CurrentAlumMl - alumConsumed);
        }

        // Scale turbidity based on expected floc size range (0-30)
        // This way water clears as flocs settle out
        turbidity = Mathf.Clamp01(flocSize / 30f);
        UpdateVolumeText();
        
        // Debug output
        Debug.Log($"[JarReaction] {gameObject.name} Phase={currentPhase}, FlocSize={flocSize:F2}, " +
                 $"Turbidity={turbidity:F2}, Growth={growth:F2}, ShearLoss={shearLoss:F2}, " +
                 $"SettlingLoss={settlingLoss:F2}, Alum={CurrentAlumMl:F1}mL");
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
        // GATE: Water only changes color after proper mixing
        if (waterRenderer != null)
        {
            Material mat = waterRenderer.material;
            Color clean = new Color(0.7f, 0.7f, 0.85f, 0.6f);
            Color cloudy = new Color(0.2f, 0.4f, 0.9f, 0.8f);
            
            // Only show turbidity if reactions have started
            // During settling, water clears up as turbidity decreases (correct behavior)
            float visualTurbidity = hasBeenMixedProperly ? turbidity : 0f;
            mat.SetColor("_BaseColor", Color.Lerp(clean, cloudy, visualTurbidity));
        }

        // GATE: Particles only appear after proper mixing
        if (flocParticleSystem != null)
        {
            var em = flocParticleSystem.emission;
            var main = flocParticleSystem.main;

            main.startSize = UNIFORM_PARTICLE_SIZE;

            float targetRate = 0f;
            float lifetime = 2f;

            // Only emit particles if reactions are active
            if (hasSufficientChemical && hasBeenMixedProperly)
            {
                switch (currentPhase)
                {
                    case JarTestPhase.Idle:
                        targetRate = 0f;
                        lifetime = 1f;
                        break;

                    case JarTestPhase.RapidMix:
                        targetRate = flocSize * 30f + Mathf.Max(0f, CurrentRPM - rapidMixMinRPM) * 2f;
                        lifetime = 0.6f;
                        break;

                    case JarTestPhase.SlowMix:
                        targetRate = flocSize * 40f + Mathf.Max(0f, CurrentRPM) * 0.5f;
                        lifetime = 3f;
                        break;

                    case JarTestPhase.Settling:
                        targetRate = flocSize * 15f;
                        lifetime = 8f;
                        break;
                }
            }

            targetRate = Mathf.Clamp(targetRate, 0f, 500f);
            em.rateOverTime = targetRate;
            main.startLifetime = lifetime;

            Color baseColor = Color.white;
            float alpha = hasBeenMixedProperly ? Mathf.Lerp(0.25f, 1f, turbidity) : 0f;
            main.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            if (targetRate > MIN_PLAY_RATE)
            {
                if (!flocParticleSystem.isPlaying)
                {
                    em.enabled = true;
                    flocParticleSystem.Play();
                }
            }
            else
            {
                if (flocParticleSystem.isPlaying)
                {
                    flocParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    em.enabled = false;
                }
            }
        }

        UpdateVolumeText();
    }

    void UpdateVolumeText()
    {
        if (volumeText != null)
            volumeText.text = CurrentAlumMl.ToString("F1") + " mL";
    }

    // Public method to reset jar for new test
    public void ResetJar()
    {
        CurrentAlumMl = 0f;
        flocSize = 0f;
        turbidity = 0f;
        settledMass = 0f;
        currentPhase = JarTestPhase.Idle;
        hasBeenMixedProperly = false;
        totalRapidMixTime = 0f;
        
        if (flocParticleSystem != null)
        {
            flocParticleSystem.Clear(true);
            flocParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        
        UpdateVolumeText();
        Debug.Log("[JarTest] Jar reset - ready for new test");
    }

    public float GetTurbidityNormalized() { return turbidity; }
    public float GetFlocSize() { return flocSize; }
    public JarTestPhase GetCurrentPhase() { return currentPhase; }
    public bool HasSufficientChemical() { return hasSufficientChemical; }
    public bool HasBeenMixedProperly() { return hasBeenMixedProperly; }
}