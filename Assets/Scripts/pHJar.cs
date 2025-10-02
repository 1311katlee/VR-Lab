using UnityEngine;
using TMPro;

public enum JarTestPhase
{
    Mixing,
    Settling,
    Completed
}

public class pHJar : MonoBehaviour
{
    [Header("pH Settings")]
    public float CurrentPH = 7.0f; // adjustable in inspector
    public float RawWaterNTU = 100f; // starting turbidity in NTU
    public float MinNTU = 4f;       // best possible turbidity after treatment

    [Header("Reaction Parameters")]
    [Tooltip("Smaller sigma = sharper efficiency curve around neutral")]
    public float sigma = 1.0f;

    [Header("Simulation Timing")]
    public float mixingDuration = 5f;
    public float settlingDuration = 15f;

    [Header("Visuals")]
    public ParticleSystem flocParticleSystem;
    public TextMeshPro phText;

    [Header("Debug")]
    public bool debugLog = true;

    // Internal state
    private float turbidity = 1f; // normalized (0–1)
    private float flocSize = 0f;
    private float efficiency = 0f;
    private float phaseTimer = 0f;
    private JarTestPhase currentPhase = JarTestPhase.Mixing;

    void Start()
    {
        ResetJar();
    }

    void Update()
    {
        SimulateReaction(Time.deltaTime);
        UpdatePHText();
    }

    public void SimulateReaction(float dt)
    {
        phaseTimer += dt;

        switch (currentPhase)
        {
            case JarTestPhase.Mixing:
                flocSize = Mathf.MoveTowards(flocSize, 1f, dt * 0.5f);
                if (phaseTimer >= mixingDuration)
                {
                    currentPhase = JarTestPhase.Settling;
                    phaseTimer = 0f;
                    if (flocParticleSystem != null)
                    {
                        var emission = flocParticleSystem.emission;
                        emission.enabled = true;
                        flocParticleSystem.Play();
                    }
                }
                break;

            case JarTestPhase.Settling:
                // Gaussian-like efficiency curve centered at ~7.0
                efficiency = Mathf.Exp(-Mathf.Pow((CurrentPH - 7f) / sigma, 2f));
                efficiency = Mathf.Clamp01(efficiency);

                // Compute NTU target from efficiency
                float targetNTU = Mathf.Lerp(RawWaterNTU, MinNTU, efficiency);
                float targetNormalized = Mathf.InverseLerp(RawWaterNTU, MinNTU, targetNTU);

                // Smoothly settle turbidity toward target
                turbidity = Mathf.Lerp(turbidity, targetNormalized, dt * 0.5f);

                if (phaseTimer >= settlingDuration)
                {
                    currentPhase = JarTestPhase.Completed;
                    if (flocParticleSystem != null)
                    {
                        var emission = flocParticleSystem.emission;
                        emission.enabled = false;
                        flocParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
                break;

            case JarTestPhase.Completed:
                // Hold final turbidity
                break;
        }

        if (debugLog)
        {
            Debug.Log($"[pHJar] pH={CurrentPH:F1}, Eff={efficiency:F2}, Floc={flocSize:F2}, NTU={GetTurbidityNTU():F1}");
        }
    }

    public float GetTurbidityNormalized()
    {
        return turbidity; // still 0–1 internally
    }

    public float GetTurbidityNTU()
    {
        return Mathf.Lerp(RawWaterNTU, MinNTU, 1f - turbidity);
    }

    public void ResetJar()
    {
        turbidity = 1f; // raw water
        flocSize = 0f;
        phaseTimer = 0f;
        currentPhase = JarTestPhase.Mixing;

        if (flocParticleSystem != null)
        {
            var emission = flocParticleSystem.emission;
            emission.enabled = false;
            flocParticleSystem.Clear(true);
            flocParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        UpdatePHText();
    }

    private void UpdatePHText()
    {
        if (phText != null)
        {
            phText.text = $"pH {CurrentPH:F1}\n{GetTurbidityNTU():F1} NTU";
        }
    }
}
