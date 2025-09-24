// pHJar.cs
// Version without alum, with realistic pH behavior

using UnityEngine;
using TMPro;

[RequireComponent(typeof(MeshRenderer))]
public class pHJar : MonoBehaviour
{
    [Header("State")]
    public float CurrentRPM = 0f;
    public float WaterVolumeMl = 1000f;

    [Header("Jar Test Parameters")]
    public float rapidMixMinRPM = 80f;
    public float rapidMixMaxRPM = 120f;
    public float slowMixMinRPM = 20f;
    public float slowMixMaxRPM = 50f;
    public float rapidMixDuration = 120f;
    public float slowMixDuration = 1200f;

    [Header("Reaction Params (tweak)")]
    public float growthRate = 0.6f;
    public float shearFactor = 0.01f;
    public float coalescenceRate = 0.2f;
    public float settlingThreshold = 0.5f;

    [Header("Visuals")]
    public ParticleSystem flocParticleSystem;
    public Renderer waterRenderer;
    public TMP_Text phText;

    [Header("References")]
    public RPMManager rpmManager;

    [Header("Auto Time Scale")]
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
        if (flocParticleSystem == null) return;

        var main = flocParticleSystem.main;
        var emission = flocParticleSystem.emission;

        // Gaussian-like efficiency curve, peak near pH 7
        float efficiency = Mathf.Exp(-Mathf.Pow((CurrentPH - 7f) / 2f, 2f));

        // Floc growth (depends on mixing and efficiency)
        if (currentPhase == JarTestPhase.RapidMix || currentPhase == JarTestPhase.SlowMix)
        {
            flocSize += growthRate * efficiency * dt;
            flocSize = Mathf.Clamp01(flocSize);
        }

        // Shear effect at very high RPM
        if (CurrentRPM > rapidMixMaxRPM)
            flocSize = Mathf.Max(0f, flocSize - shearFactor * dt);

        // Turbidity is proportional to floc size but decreases when settling
        if (currentPhase == JarTestPhase.Settling)
            turbidity = Mathf.Lerp(turbidity, 0f, dt * 0.05f);
        else
            turbidity = Mathf.Clamp01(flocSize);

        // --- Particle Size ---
        float size = Mathf.Lerp(0.2f, 1.0f, efficiency);
        main.startSize = size;

        // --- Particle Lifetime ---
        float lifetime = Mathf.Lerp(2f, 6f, efficiency);
        main.startLifetime = lifetime;

        // --- Emission Rate ---
        float baseRate = 5f;
        float rate = baseRate + (efficiency * 50f * flocSize);
        emission.rateOverTime = rate;

        // --- Particle Color ---
        Color baseColor = Color.white;
        float alpha = Mathf.Lerp(0.2f, 1.0f, efficiency);
        main.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        // Debug
        Debug.Log($"[pHJar] Phase={currentPhase}, pH={CurrentPH}, Eff={efficiency:F2}, Floc={flocSize:F2}, Turb={turbidity:F2}, Rate={rate:F1}");
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

        UpdatePHText();
    }

    void UpdatePHText()
    {
        if (phText != null)
            phText.text = CurrentPH.ToString("F1");
    }

    // Public API
    public void SetPH(float value) { CurrentPH = Mathf.Clamp(value, 0f, 14f); }
    public float GetPH() { return CurrentPH; }
    public JarTestPhase GetCurrentPhase() { return currentPhase; }
    public float GetFlocSize() { return flocSize; }
    public float GetTurbidityNormalized() { return turbidity; }
}
