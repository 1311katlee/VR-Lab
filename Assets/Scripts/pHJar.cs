// pHJar.cs
// Full script: no particles at start, uniform particle size, proper Play/Stop logic.

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
    public float settlingThreshold = 0.5f;

    [Header("Visuals")]
    public ParticleSystem flocParticleSystem;   // assign the ParticleSystem instance
    public Renderer waterRenderer;              // material used to tint water
    public TMP_Text phText;                     // UI text showing pH

    [Header("Particle Renderer Options (optional)")]
    public bool useMeshParticles = false;       // set true to force Mesh (sphere)
    public Material particleMaterial;          // optional: material applied to particles
    public float particleSize = 0.08f;         // uniform particle size
    public float particleLifetime = 4f;        // uniform lifetime
    public float rateScale = 100f;             // how strongly floc -> emission maps
    public float emissionThreshold = 1f;       // min rate to start the system

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

    // --- Awake: force particle systems to be cleared before first frame ---
    void Awake()
    {
        // if no explicit PS assigned, try to find one in children
        if (flocParticleSystem == null)
            flocParticleSystem = GetComponentInChildren<ParticleSystem>();

        if (flocParticleSystem == null)
        {
            Debug.LogWarning($"[{name}] No ParticleSystem assigned or found in children.");
            return;
        }

        // Main module handles playOnAwake / prewarm and lifetime / size defaults
        var main = flocParticleSystem.main;
        main.playOnAwake = false;
        main.prewarm = false;
        main.startLifetime = particleLifetime;
        main.startSize = particleSize;
        main.maxParticles = Mathf.Max(500, main.maxParticles);

        // Emission off
        var emission = flocParticleSystem.emission;
        emission.enabled = false;

        // Optionally force particle renderer to mesh + material
        var psRenderer = flocParticleSystem.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            if (useMeshParticles)
            {
                psRenderer.renderMode = ParticleSystemRenderMode.Mesh;

                // try to grab a sphere mesh by creating a temporary primitive
                GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Mesh tmpMesh = null;
                var mf = tmp.GetComponent<MeshFilter>();
                if (mf != null) tmpMesh = mf.sharedMesh;

                if (tmpMesh != null)
                {
                    psRenderer.mesh = tmpMesh;
                }

                // destroy the temp primitive (use DestroyImmediate in editor mode)
#if UNITY_EDITOR
                DestroyImmediate(tmp);
#else
                Destroy(tmp);
#endif
            }
            else
            {
                psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            if (particleMaterial != null)
                psRenderer.material = particleMaterial;
        }

        // Force clear any editor-cached particles and stop the system fully
        flocParticleSystem.Clear(true);
        flocParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        Debug.Log($"[{name}] Awake(): Cleared particle system and disabled emission.");
    }

    void Start()
    {
        UpdatePHText();
    }

    void Update()
    {
        // update RPM from RPMManager if present
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
        Debug.Log($"[pHJar] {name} started {newPhase} at RPM: {CurrentRPM:F1}");
    }

    void SimulateReaction(float dt)
    {
        if (flocParticleSystem == null) return;

        var main = flocParticleSystem.main;
        var emission = flocParticleSystem.emission;

        // Gaussian-like efficiency curve centered near 6.5-7.0
        float sigma = 2f;
        float efficiency = Mathf.Exp(-Mathf.Pow((CurrentPH - 7f) / sigma, 2f));

        // Floc growth occurs while mixing phases are active
        if (currentPhase == JarTestPhase.RapidMix || currentPhase == JarTestPhase.SlowMix)
        {
            flocSize += growthRate * efficiency * dt;
            flocSize = Mathf.Clamp01(flocSize);
        }

        // Shear reduces floc at extreme rpm
        if (CurrentRPM > rapidMixMaxRPM)
            flocSize = Mathf.Max(0f, flocSize - shearFactor * dt);

        // Turbidity responds to floc size, decays while settling
        if (currentPhase == JarTestPhase.Settling)
            turbidity = Mathf.Lerp(turbidity, 0f, dt * 0.05f);
        else
            turbidity = Mathf.Clamp01(flocSize);

        // --- Particle settings (uniform size) ---
        main.startLifetime = particleLifetime;
        main.startSize = particleSize;

        // emission rate driven by floc size and efficiency (no background emission)
        float baseRate = 0f;
        float rate = baseRate + (efficiency * rateScale * flocSize);
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);

        // color alpha still reflects efficiency so visibility changes, color kept by material
        Color baseColor = Color.white;
        float alpha = Mathf.Lerp(0.2f, 1.0f, efficiency);
        main.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        // --- Play / Stop logic ---
        if (rate > emissionThreshold)
        {
            if (!flocParticleSystem.isPlaying)
            {
                emission.enabled = true;
                flocParticleSystem.Play();
            }
        }
        else
        {
            if (flocParticleSystem.isPlaying)
            {
                // stop and clear to avoid leftover particles
                flocParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                emission.enabled = false;
            }
        }

        // debug: rate and alive count
        Debug.Log($"[pHJar] {name} Phase={currentPhase}, pH={CurrentPH:F1}, Eff={efficiency:F2}, Floc={flocSize:F2}, Rate={rate:F1}, Alive={flocParticleSystem.particleCount}");
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

    // Reset and clear this jar immediately
    public void ResetJar()
    {
        flocSize = 0f;
        turbidity = 0f;
        if (flocParticleSystem != null)
        {
            var emission = flocParticleSystem.emission;
            emission.enabled = false;
            flocParticleSystem.Clear(true);
            flocParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        UpdatePHText();
    }
}
