// pHJar.cs
// Updated with proper turbidity reduction and corrected water color gradient

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

    [Header("Initial Water Quality")]
    public float initialTurbidityNTU = 100f;
    public float minimumFinalNTU = 4f;

    [Header("Water Color Gradient")]
    [Tooltip("Color of clean water (low turbidity)")]
    public Color cleanWaterColor = new Color(0.85f, 0.9f, 1.0f, 0.25f);
    [Tooltip("Color of turbid water (high turbidity)")]
    public Color turbidWaterColor = new Color(0.5f, 0.45f, 0.35f, 0.95f);

    [Header("Reaction Params (tweak)")]
    public float growthRate = 0.6f;
    public float shearFactor = 0.01f;
    public float settlingThreshold = 0.5f;

    [Header("Visuals")]
    public ParticleSystem flocParticleSystem;
    public Renderer waterRenderer;
    public TMP_Text phText;

    [Header("Sludge Spawning")]
    public Transform sludgeSpawnPoint;
    public GameObject settledFlocPrefab;
    public float sludgeSpawnRadius = 0.1f;
    public int minSludgePiecesPerSpawn = 2;
    public int maxSludgePiecesPerSpawn = 5;
    public float baseSludgeScale = 0.015f;

    [Header("Particle Renderer Options (optional)")]
    public bool useMeshParticles = false;
    public Material particleMaterial;
    public float particleSize = 0.08f;
    public float particleLifetime = 4f;
    public float rateScale = 100f;
    public float emissionThreshold = 1f;

    [Header("References")]
    public RPMManager rpmManager;

    [Header("Auto Time Scale")]
    public float simulationTimeScale = 1f;

    // Internal state
    private float flocSize = 0f;
    private float turbidity = 1f;
    private float currentEfficiency = 0f;
    private float settledMass = 0f;

    // Jar Test Phase Tracking
    public enum JarTestPhase { Idle, RapidMix, SlowMix, Settling }
    private JarTestPhase currentPhase = JarTestPhase.Idle;
    private float phaseStartTime;

    // Chemistry
    [Header("Chemistry")]
    [Range(0f, 14f)] public float CurrentPH = 7f;

    void Awake()
    {
        if (flocParticleSystem == null)
            flocParticleSystem = GetComponentInChildren<ParticleSystem>();

        if (flocParticleSystem == null)
        {
            Debug.LogWarning($"[{name}] No ParticleSystem assigned or found in children.");
            return;
        }

        var main = flocParticleSystem.main;
        main.playOnAwake = false;
        main.prewarm = false;
        main.startLifetime = particleLifetime;
        main.startSize = particleSize;
        main.maxParticles = Mathf.Max(500, main.maxParticles);

        var emission = flocParticleSystem.emission;
        emission.enabled = false;

        var psRenderer = flocParticleSystem.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            if (useMeshParticles)
            {
                psRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Mesh tmpMesh = null;
                var mf = tmp.GetComponent<MeshFilter>();
                if (mf != null) tmpMesh = mf.sharedMesh;
                if (tmpMesh != null) psRenderer.mesh = tmpMesh;
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

        flocParticleSystem.Clear(true);
        flocParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Start()
    {
        UpdatePHText();
        UpdateVisuals(); // Initialize water color
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
        Debug.Log($"[pHJar] {name} started {newPhase} at RPM: {CurrentRPM:F1}");
    }

    void SimulateReaction(float dt)
    {
        if (flocParticleSystem == null) return;

        var main = flocParticleSystem.main;
        var emission = flocParticleSystem.emission;

        // Gaussian-like efficiency curve centered near pH 6.5
        float sigma = 2.5f;
        float optimalPH = 6.25f;
        currentEfficiency = Mathf.Exp(-Mathf.Pow((CurrentPH - optimalPH) / sigma, 2f));

        // Floc growth occurs while mixing phases are active
        if (currentPhase == JarTestPhase.RapidMix || currentPhase == JarTestPhase.SlowMix)
        {
            flocSize += growthRate * currentEfficiency * dt;
            flocSize = Mathf.Clamp01(flocSize);
        }

        // Shear reduces floc at extreme rpm
        if (CurrentRPM > rapidMixMaxRPM)
            flocSize = Mathf.Max(0f, flocSize - shearFactor * dt);

        // SLUDGE SETTLING during settling phase
        if (currentPhase == JarTestPhase.Settling && flocSize > 0.1f)
        {
            // Convert flocs to settled sludge during settling
            float settlingRate = flocSize * currentEfficiency * dt * 0.5f;
            SpawnSettledSludge(settlingRate);
            flocSize = Mathf.Max(0f, flocSize - settlingRate);
        }

        // TURBIDITY REDUCTION MODEL
        if (currentPhase == JarTestPhase.Settling)
        {
            float targetFraction = (minimumFinalNTU + (1f - currentEfficiency) * (initialTurbidityNTU - minimumFinalNTU)) / initialTurbidityNTU;
            turbidity = Mathf.Lerp(turbidity, targetFraction, dt * 0.1f);
        }
        else if (currentPhase == JarTestPhase.RapidMix || currentPhase == JarTestPhase.SlowMix)
        {
            float targetFraction = (minimumFinalNTU + (1f - currentEfficiency) * (initialTurbidityNTU - minimumFinalNTU)) / initialTurbidityNTU;
            turbidity = Mathf.Lerp(turbidity, targetFraction, dt * 0.02f);
        }

        turbidity = Mathf.Clamp01(turbidity);

        // Particle settings
        main.startLifetime = particleLifetime;
        main.startSize = particleSize;

        // Emission rate driven by floc activity
        float baseRate = 0f;
        float rate = baseRate + (currentEfficiency * rateScale * flocSize);
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);

        // Particle alpha reflects efficiency
        Color baseColor = Color.white;
        float alpha = Mathf.Lerp(0.2f, 1.0f, currentEfficiency);
        main.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        // Play / Stop logic
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
                flocParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                emission.enabled = false;
            }
        }

        // Debug with actual NTU
        float currentNTU = turbidity * initialTurbidityNTU;
        Debug.Log($"[pHJar] {name} Phase={currentPhase}, pH={CurrentPH:F1}, Eff={currentEfficiency:F2}, Floc={flocSize:F2}, NTU={currentNTU:F1}");
    }

    void SpawnSettledSludge(float amount)
    {
        if (amount <= 0f || settledFlocPrefab == null || sludgeSpawnPoint == null) return;
        
        settledMass += amount;
        
        // Spawn multiple small pieces instead of one large sphere
        if (settledMass > 0.03f)
        {
            // Spawn 2-5 small sludge pieces randomly distributed
            int piecesToSpawn = Random.Range(minSludgePiecesPerSpawn, maxSludgePiecesPerSpawn + 1);
            float massPerPiece = settledMass / piecesToSpawn;
            
            for (int i = 0; i < piecesToSpawn; i++)
            {
                // Random position within spawn radius (only X and Z, not Y)
                Vector2 randomCircle = Random.insideUnitCircle * sludgeSpawnRadius * 2f;
                Vector3 spawnOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
                Vector3 spawnPos = sludgeSpawnPoint.position + spawnOffset;
                
                // Random rotation
                Quaternion rot = Quaternion.Euler(
                    Random.Range(-10f, 10f),
                    Random.Range(0f, 360f),
                    Random.Range(-10f, 10f)
                );
                
                GameObject sludge = Instantiate(settledFlocPrefab, spawnPos, rot, sludgeSpawnPoint);
                
                // Varied sizes for visual interest
                float baseScale = baseSludgeScale + massPerPiece * 0.02f;
                float variation = Random.Range(0.8f, 1.2f);
                sludge.transform.localScale = Vector3.one * baseScale * variation;
            }
            
            settledMass = 0f;
        }
    }

    void UpdateVisuals()
    {
        if (waterRenderer != null)
        {
            Material mat = waterRenderer.material;
            
            // CORRECTED: Low turbidity (clean) = cleanWaterColor, High turbidity (dirty) = turbidWaterColor
            // turbidity ranges from 0 (cleanest) to 1 (dirtiest)
            Color currentColor = Color.Lerp(cleanWaterColor, turbidWaterColor, turbidity);
            mat.SetColor("_BaseColor", currentColor);
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
    public float GetTurbidityNTU() { return turbidity * initialTurbidityNTU; }

    public void ResetJar()
    {
        flocSize = 0f;
        turbidity = 1f;
        settledMass = 0f;
        
        if (flocParticleSystem != null)
        {
            var emission = flocParticleSystem.emission;
            emission.enabled = false;
            flocParticleSystem.Clear(true);
            flocParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        UpdatePHText();
        UpdateVisuals(); // Update water color on reset
    }
}