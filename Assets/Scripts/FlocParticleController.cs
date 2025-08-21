using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FlocParticleController : MonoBehaviour 
{
    [Header("Stirrer Reference")]
    public Transform stirrer;
    
    [Header("Chemical Dosing")]
    public float alumDose = 0f; // mg/L - set this when alum is added via pipette
    public float minimumDoseForFlocculation = 5f; // mg/L minimum dose needed
    
    [Header("RPM Thresholds")]
    public float rapidMixMinRPM = 80f;
    public float rapidMixMaxRPM = 120f;
    public float slowMixMinRPM = 20f;
    public float slowMixMaxRPM = 50f;
    
    [Header("Phase Timing")]
    public float rapidMixDuration = 120f; // 2 minutes
    public float slowMixDuration = 1200f; // 20 minutes
    
    [Header("Particle Settings")]
    public float baseRate = 5f;
    public float rateMultiplier = 0.3f;
    public float rapidMixLifetime = 0.5f; // Short-lived during rapid mix
    public float slowMixLifetime = 3f; // Longer-lived flocs during slow mix
    public float settlingLifetime = 8f; // Even longer during settling
    
    private ParticleSystem ps;
    private ParticleSystem.EmissionModule em;
    private ParticleSystem.MainModule main;
    private float lastYAngle;
    private float rpm;
    
    // Phase tracking
    private enum JarTestPhase { Idle, RapidMix, SlowMix, Settling }
    private JarTestPhase currentPhase = JarTestPhase.Idle;
    private float phaseStartTime;
    private bool chemicalAdded = false;
    
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
        // Check if sufficient chemical has been added
        chemicalAdded = alumDose >= minimumDoseForFlocculation;
        
        if (!chemicalAdded)
        {
            currentPhase = JarTestPhase.Idle;
            return;
        }
        
        float phaseTime = Time.time - phaseStartTime;
        
        switch (currentPhase)
        {
            case JarTestPhase.Idle:
                // Start rapid mix when stirring at appropriate speed with chemical present
                if (rpm >= rapidMixMinRPM && rpm <= rapidMixMaxRPM)
                {
                    StartPhase(JarTestPhase.RapidMix);
                }
                break;
                
            case JarTestPhase.RapidMix:
                // Continue rapid mix for specified duration or if speed drops
                if (phaseTime >= rapidMixDuration || rpm < rapidMixMinRPM)
                {
                    if (rpm >= slowMixMinRPM && rpm <= slowMixMaxRPM)
                    {
                        StartPhase(JarTestPhase.SlowMix);
                    }
                    else if (rpm < slowMixMinRPM)
                    {
                        StartPhase(JarTestPhase.Settling);
                    }
                }
                break;
                
            case JarTestPhase.SlowMix:
                // Continue slow mix for specified duration or if stirring stops
                if (phaseTime >= slowMixDuration || rpm < slowMixMinRPM)
                {
                    StartPhase(JarTestPhase.Settling);
                }
                // Return to rapid mix if speed increases significantly
                else if (rpm > slowMixMaxRPM)
                {
                    StartPhase(JarTestPhase.RapidMix);
                }
                break;
                
            case JarTestPhase.Settling:
                // Return to appropriate mix phase if stirring resumes
                if (rpm >= rapidMixMinRPM && rpm <= rapidMixMaxRPM)
                {
                    StartPhase(JarTestPhase.RapidMix);
                }
                else if (rpm >= slowMixMinRPM && rpm <= slowMixMaxRPM)
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
        Debug.Log($"[JarTest] Started {newPhase} phase at RPM: {rpm:F1}");
    }
    
    void UpdateParticleSystem()
    {
        if (!chemicalAdded)
        {
            // No particles without chemical
            em.rateOverTime = 0;
            main.startLifetime = 1f;
            return;
        }
        
        float rate = 0f;
        float lifetime = 1f;
        
        switch (currentPhase)
        {
            case JarTestPhase.Idle:
                // Minimal particle activity
                rate = 0f;
                lifetime = 1f;
                break;
                
            case JarTestPhase.RapidMix:
                // Small, fast-moving particles representing initial coagulation
                if (rpm >= rapidMixMinRPM && rpm <= rapidMixMaxRPM)
                {
                    rate = baseRate + (rpm - rapidMixMinRPM) * rateMultiplier;
                    lifetime = rapidMixLifetime;
                }
                break;
                
            case JarTestPhase.SlowMix:
                // Larger flocs forming, moderate emission rate
                if (rpm >= slowMixMinRPM && rpm <= slowMixMaxRPM)
                {
                    rate = baseRate * 1.5f + rpm * rateMultiplier * 0.5f;
                    lifetime = slowMixLifetime;
                }
                break;
                
            case JarTestPhase.Settling:
                // Large, slowly settling flocs
                rate = baseRate * 0.3f; // Much lower emission
                lifetime = settlingLifetime;
                break;
        }
        
        // Apply dosage factor (more chemical = more flocs)
        float dosageFactor = Mathf.Clamp(alumDose / 20f, 0.1f, 2f);
        rate *= dosageFactor;
        
        em.rateOverTime = Mathf.Clamp(rate, 0f, 200f);
        main.startLifetime = lifetime;
        
        Debug.Log($"[JarTest] Phase: {currentPhase}, RPM: {rpm:F1}, " +
                 $"AlumDose: {alumDose:F1}mg/L, Rate: {em.rateOverTime.constant:F1}, " +
                 $"Lifetime: {main.startLifetime.constant:F1}, Alive: {ps.particleCount}");
    }
    
    // Call this method when alum is added via pipette
    public void AddAlumDose(float doseAmount)
    {
        alumDose += doseAmount;
        Debug.Log($"[JarTest] Added {doseAmount}mg/L alum. Total dose: {alumDose}mg/L");
        
        // Reset phase if this is first significant dose
        if (alumDose >= minimumDoseForFlocculation && currentPhase == JarTestPhase.Idle)
        {
            Debug.Log("[JarTest] Sufficient chemical added - ready for testing");
        }
    }
    
    // Reset the jar test
    public void ResetTest()
    {
        alumDose = 0f;
        currentPhase = JarTestPhase.Idle;
        chemicalAdded = false;
        em.rateOverTime = 0;
        Debug.Log("[JarTest] Test reset");
    }
}