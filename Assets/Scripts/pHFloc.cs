using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class pHFloc : MonoBehaviour
{
    [Header("Stirrer Reference")]
    public Transform stirrer;

    [Header("pH Control")]
    public pHJar jarReaction;   // assign the JarReaction script from this jar
    public float optimalMinPH = 6f;   // effective range for flocculation
    public float optimalMaxPH = 8f;
    
    [Header("RPM Thresholds")]
    public float rapidMixMinRPM = 80f;
    public float rapidMixMaxRPM = 120f;
    public float slowMixMinRPM = 20f;
    public float slowMixMaxRPM = 50f;
    
    [Header("Phase Timing")]
    public float rapidMixDuration = 120f;
    public float slowMixDuration = 1200f;
    
    [Header("Particle Settings")]
    public float baseRate = 5f;
    public float rateMultiplier = 0.3f;
    public float rapidMixLifetime = 0.5f;
    public float slowMixLifetime = 3f;
    public float settlingLifetime = 8f;
    
    private ParticleSystem ps;
    private ParticleSystem.EmissionModule em;
    private ParticleSystem.MainModule main;
    private float lastYAngle;
    private float rpm;

    // Phase tracking
    private enum JarTestPhase { Idle, RapidMix, SlowMix, Settling }
    private JarTestPhase currentPhase = JarTestPhase.Idle;
    private float phaseStartTime;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        em = ps.emission;
        main = ps.main;

        em.enabled = true;
        em.rateOverTime = 0;
        lastYAngle = stirrer.localEulerAngles.y;

        if (!ps.isPlaying)
            ps.Play();
    }

    void Update()
    {
        CalculateRPM();
        UpdateTestPhase();
        UpdateParticleSystem();
    }

    void CalculateRPM()
    {
        float currentY = stirrer.localEulerAngles.y;
        float deltaY = Mathf.DeltaAngle(lastYAngle, currentY);
        lastYAngle = currentY;

        float degreesPerSecond = deltaY / Time.deltaTime;
        rpm = Mathf.Abs(degreesPerSecond / 360f * 60f);
    }

    void UpdateTestPhase()
    {
        float pH = GetJarPH();
        bool phEffective = (pH >= optimalMinPH && pH <= optimalMaxPH);

        if (!phEffective)
        {
            currentPhase = JarTestPhase.Idle;
            return;
        }

        float phaseTime = Time.time - phaseStartTime;

        switch (currentPhase)
        {
            case JarTestPhase.Idle:
                if (rpm >= rapidMixMinRPM && rpm <= rapidMixMaxRPM)
                    StartPhase(JarTestPhase.RapidMix);
                break;

            case JarTestPhase.RapidMix:
                if (phaseTime >= rapidMixDuration || rpm < rapidMixMinRPM)
                {
                    if (rpm >= slowMixMinRPM && rpm <= slowMixMaxRPM)
                        StartPhase(JarTestPhase.SlowMix);
                    else if (rpm < slowMixMinRPM)
                        StartPhase(JarTestPhase.Settling);
                }
                break;

            case JarTestPhase.SlowMix:
                if (phaseTime >= slowMixDuration || rpm < slowMixMinRPM)
                    StartPhase(JarTestPhase.Settling);
                else if (rpm > slowMixMaxRPM)
                    StartPhase(JarTestPhase.RapidMix);
                break;

            case JarTestPhase.Settling:
                if (rpm >= rapidMixMinRPM && rpm <= rapidMixMaxRPM)
                    StartPhase(JarTestPhase.RapidMix);
                else if (rpm >= slowMixMinRPM && rpm <= slowMixMaxRPM)
                    StartPhase(JarTestPhase.SlowMix);
                break;
        }
    }

    void StartPhase(JarTestPhase newPhase)
    {
        currentPhase = newPhase;
        phaseStartTime = Time.time;
        Debug.Log($"[JarTestPH] Started {newPhase} phase at RPM: {rpm:F1}");
    }

    void UpdateParticleSystem()
    {
        float pH = GetJarPH();
        bool phEffective = (pH >= optimalMinPH && pH <= optimalMaxPH);

        if (!phEffective)
        {
            em.rateOverTime = 0;
            main.startLifetime = 1f;
            return;
        }

        float rate = 0f;
        float lifetime = 1f;

        switch (currentPhase)
        {
            case JarTestPhase.Idle:
                rate = 0f;
                lifetime = 1f;
                break;

            case JarTestPhase.RapidMix:
                if (rpm >= rapidMixMinRPM && rpm <= rapidMixMaxRPM)
                {
                    rate = baseRate + (rpm - rapidMixMinRPM) * rateMultiplier;
                    lifetime = rapidMixLifetime;
                }
                break;

            case JarTestPhase.SlowMix:
                if (rpm >= slowMixMinRPM && rpm <= slowMixMaxRPM)
                {
                    rate = baseRate * 1.5f + rpm * rateMultiplier * 0.5f;
                    lifetime = slowMixLifetime;
                }
                break;

            case JarTestPhase.Settling:
                rate = baseRate * 0.3f;
                lifetime = settlingLifetime;
                break;
        }

        // Factor in how close pH is to the optimal range center
        float centerPH = (optimalMinPH + optimalMaxPH) / 2f;
        float phEffectiveness = Mathf.Clamp01(1f - Mathf.Abs(pH - centerPH) / 2f);
        rate *= phEffectiveness;

        em.rateOverTime = Mathf.Clamp(rate, 0f, 200f);
        main.startLifetime = lifetime;

        Debug.Log($"[JarTestPH] Phase: {currentPhase}, RPM: {rpm:F1}, pH: {pH:F1}, " +
                  $"Rate: {em.rateOverTime.constant:F1}, Lifetime: {main.startLifetime.constant:F1}, Alive: {ps.particleCount}");
    }

    float GetJarPH()
    {
        return (jarReaction != null) ? jarReaction.CurrentPH : 7f; // default neutral
    }
}
