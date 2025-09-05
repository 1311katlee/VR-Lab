// JarReaction.cs
// Version without alum, with pH added

using UnityEngine;
using TMPro;

public class pHJar : MonoBehaviour
{
    [Header("State")]
    public float CurrentRPM = 0f;            // stir speed from stirrer
    public float WaterVolumeMl = 1000f;      // jar volume (1000 ml)

    [Header("Jar Test Parameters")]
    public float rapidMixMinRPM = 80f;
    public float rapidMixMaxRPM = 120f;
    public float slowMixMinRPM = 20f;
    public float slowMixMaxRPM = 50f;
    public float rapidMixDuration = 120f;    // 2 minutes
    public float slowMixDuration = 1200f;    // 20 minutes

    [Header("Reaction params (tweak)")]
    public float growthRate = 0.6f;
    public float shearFactor = 0.01f;
    public float coalescenceRate = 0.2f;
    public float settlingThreshold = 0.5f;

    [Header("Visuals")]
    public ParticleSystem flocParticleSystem; // particles for flocs
    public Renderer waterRenderer;            // water material
    public TMP_Text phText;                   // UI text to display pH

    [Header("References")]
    public RPMManager rpmManager;             // assign in Inspector

    [Header("Auto time scale")]
    public float simulationTimeScale = 1f;

    // Internal state
    private float flocSize = 0f;
    private float turbidity = 0f;

    // Jar Test Phase Tracking
    public enum JarTestPhase { Idle, RapidMix, SlowMix, Settling }
    private JarTestPhase currentPhase = JarTestPhase.Idle;
    private float phaseStartTime;

    // Chemistry
    [Header("Chemistry")]
    [Range(0f, 14f)] public float CurrentPH = 7f;

    void Start()
    {
        UpdatePHText();
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

    void UpdateJarTestPhase()
    {
        float phaseTime = Time.time - phaseStartTime;

        switch (currentPhase)
        {
            case JarTestPhase.Idle:
                if (CurrentRPM >= rapidMixMinRPM && CurrentRPM <= rapidMixMaxRPM)
                    StartPhase(JarTestPhase.RapidMix);
                break;

            case JarTestPhase.RapidMix:
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
        float doseFactor = 1f; // since no alum, base factor is constant

        float phaseGrowthMultiplier = 1f;
        float phaseShearMultiplier = 1f;

        switch (currentPhase)
        {
            case JarTestPhase.Idle: phaseGrowthMultiplier = 0.1f; phaseShearMultiplier = 0f; break;
            case JarTestPhase.RapidMix: phaseGrowthMultiplier = 2f; phaseShearMultiplier = 2f; break;
            case JarTestPhase.SlowMix: phaseGrowthMultiplier = 1.5f; phaseShearMultiplier = 0.5f; break;
            case JarTestPhase.Settling: phaseGrowthMultiplier = 0.2f; phaseShearMultiplier = 0f; break;
        }

        // pH efficiency (best range ~6–8.5)
        float phEfficiency = Mathf.Clamp01(1f - Mathf.Abs(CurrentPH - 7.5f) / 5f);

        float growth = growthRate * doseFactor * (1f + coalescenceRate * flocSize) * dt * phaseGrowthMultiplier * phEfficiency;
        float shearLoss = shearFactor * CurrentRPM * flocSize * dt * phaseShearMultiplier;

        float settlingLoss = 0f;
        if (flocSize > settlingThreshold)
        {
            float settlingMultiplier = (currentPhase == JarTestPhase.Settling) ? 3f : 1f;
            settlingLoss = (flocSize - settlingThreshold) * 0.1f * dt * settlingMultiplier;
        }

        flocSize += growth - shearLoss - settlingLoss;
        flocSize = Mathf.Max(0f, flocSize);

        turbidity = Mathf.Clamp01(flocSize / 2f);
    }

    void UpdateVisuals()
    {
        // Water color based on turbidity
        if (waterRenderer != null)
        {
            Material mat = waterRenderer.material;
            Color clean = new Color(0.7f, 0.7f, 0.85f, 0.6f);
            Color cloudy = new Color(0.2f, 0.4f, 0.9f, 0.8f);
            mat.SetColor("_BaseColor", Color.Lerp(clean, cloudy, turbidity));
        }

        // Particle system
        if (flocParticleSystem != null)
        {
            var em = flocParticleSystem.emission;
            var main = flocParticleSystem.main;

            float baseRate = 0f;
            float lifetime = 1f;

            switch (currentPhase)
            {
                case JarTestPhase.Idle: baseRate = 0f; lifetime = 1f; break;
                case JarTestPhase.RapidMix: baseRate = flocSize * 30f; lifetime = 0.5f; break;
                case JarTestPhase.SlowMix: baseRate = flocSize * 40f; lifetime = 3f; break;
                case JarTestPhase.Settling: baseRate = flocSize * 15f; lifetime = 8f; break;
            }

            em.rateOverTime = Mathf.Clamp(baseRate, 0f, 500f);
            main.startLifetime = lifetime;
        }

        UpdatePHText();
    }

    void UpdatePHText()
    {
        if (phText != null)
            phText.text = "pH: " + CurrentPH.ToString("F1");
    }

    // --- pH controls ---
    public void SetPH(float value) { CurrentPH = Mathf.Clamp(value, 0f, 14f); }
    public float GetPH() { return CurrentPH; }
    public JarTestPhase GetCurrentPhase() { return currentPhase; }
    public float GetFlocSize() { return flocSize; }
    public float GetTurbidityNormalized() { return turbidity; }
}
