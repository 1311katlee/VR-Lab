// JarReaction.cs
// Alum-only jar test with realistic turbidity: starts cloudy, clears as flocs settle

using UnityEngine;
using TMPro;

[RequireComponent(typeof(MeshRenderer))]
public class JarReaction : MonoBehaviour
{
    [Header("State")]
    public float CurrentAlumMl = 0f;
    public float CurrentRPM = 0f;
    public float WaterVolumeMl = 1000f;

    [Header("Jar Test Parameters")]
    public float minimumAlumForFlocculation = 5f; 
    public float rapidMixMinRPM = 80f;
    public float rapidMixMaxRPM = 120f;
    public float slowMixMinRPM = 20f;
    public float slowMixMaxRPM = 50f;
    public float rapidMixDuration = 120f;    
    public float slowMixDuration = 1200f;    
    public float minimumRapidMixTime = 10f;  

    [Header("Reaction params (tweak)")]
    public float growthRate = 0.6f;         
    public float shearFactor = 0.01f;       
    public float coalescenceRate = 0.2f;    
    public float settlingThreshold = 0.5f;  

    [Header("Visuals")]
    public ParticleSystem flocParticleSystem; 
    public Renderer waterRenderer;            
    public Transform sludgeSpawnPoint;        
    public GameObject settledFlocPrefab;      
    public TMP_Text volumeText;               

    [Header("Particle Rendering (optional)")]
    public Mesh sphereMeshOverride;           

    [Header("References")]
    public RPMManager rpmManager;             

    [Header("Auto time scale")]
    public float simulationTimeScale = 1f;    

    // Internal state
    public float flocSize = 0f;    
    public float turbidity = 1f;   // start cloudy (100 NTU)
    private float settledMass = 0f;

    // Jar Test Phase Tracking
    public enum JarTestPhase { Idle, RapidMix, SlowMix, Settling }
    private JarTestPhase currentPhase = JarTestPhase.Idle;
    private float phaseStartTime;
    private bool hasSufficientChemical = false;
    private bool hasBeenMixedProperly = false; 
    private float totalRapidMixTime = 0f;      

    private const float UNIFORM_PARTICLE_SIZE = 0.03f;  
    private const float MIN_PLAY_RATE = 1f;             

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
                if (CurrentRPM >= rapidMixMinRPM && CurrentRPM <= rapidMixMaxRPM)
                {
                    totalRapidMixTime += dt;
                    if (!hasBeenMixedProperly && totalRapidMixTime >= minimumRapidMixTime)
                    {
                        hasBeenMixedProperly = true;
                        flocSize = CurrentAlumMl * 0.01f;
                        Debug.Log($"[JarTest] Rapid mix complete - reactions active");
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
    }

    void SimulateReaction(float dt)
    {
        if (!hasBeenMixedProperly)
        {
            flocSize = 0f;
            turbidity = 1f; // raw turbid water
            return;
        }

        float availableDose = Mathf.Max(0f, CurrentAlumMl);
        float doseFactor = availableDose / (WaterVolumeMl / 1000f);

        float growth = growthRate * doseFactor * (1f + coalescenceRate * flocSize) * dt;
        float shearLoss = shearFactor * Mathf.Max(0f, CurrentRPM) * flocSize * dt;

        float settlingLoss = 0f;
        if (flocSize > settlingThreshold)
        {
            float settlingMultiplier = (currentPhase == JarTestPhase.Settling) ? 3f : 1f;
            settlingLoss = (flocSize - settlingThreshold) * 0.1f * dt * settlingMultiplier;
            SpawnSettledIfNeeded(settlingLoss);
        }

        flocSize += growth - shearLoss - settlingLoss;
        flocSize = Mathf.Max(0f, flocSize);

        // --- New turbidity model: start cloudy (1), clear as flocs grow & settle
        float clearing = Mathf.Clamp01(flocSize / 30f);
        turbidity = Mathf.Lerp(turbidity, 1f - clearing, dt * 2f);
        turbidity = Mathf.Clamp(turbidity, 0.04f, 1f); // floor ~4 NTU
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
        if (waterRenderer != null)
        {
            Material mat = waterRenderer.material;
            Color clean = new Color(0.7f, 0.7f, 0.85f, 0.6f);
            Color cloudy = new Color(0.2f, 0.4f, 0.9f, 0.8f);
            mat.SetColor("_BaseColor", Color.Lerp(clean, cloudy, turbidity));
        }
    }

    void UpdateVolumeText()
    {
        if (volumeText != null)
            volumeText.text = CurrentAlumMl.ToString("F1") + " mL";
    }

    public void ResetJar()
    {
        CurrentAlumMl = 0f;
        flocSize = 0f;
        turbidity = 1f;
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
    }

    public float GetTurbidityNormalized() { return turbidity; }
    public float GetFlocSize() { return flocSize; }
    public JarTestPhase GetCurrentPhase() { return currentPhase; }
    public bool HasSufficientChemical() { return hasSufficientChemical; }
    public bool HasBeenMixedProperly() { return hasBeenMixedProperly; }
}
