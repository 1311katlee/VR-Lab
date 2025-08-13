using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FlocParticleController : MonoBehaviour 
{
    public Transform stirrer;
    public float rpmThreshold = 100;
    public float baseRate = 10f;
    public float rateMultiplier = 0.5f;
    public float minLifetime = 1.5f;
    public float maxLifetime = 3.0f;
    
    private ParticleSystem ps;
    private ParticleSystem.EmissionModule em;
    private ParticleSystem.MainModule main;
    private float lastYAngle;
    private float rpm;
    private bool wasActive = false; // Track previous state
    
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        em = ps.emission;
        main = ps.main;
        
        // Start the particle system but with emission disabled
        em.enabled = false;
        em.rateOverTime = 0; // Explicitly set to 0
        lastYAngle = stirrer.localEulerAngles.y;
        
        // Ensure the particle system is playing (but not emitting)
        if (!ps.isPlaying)
            ps.Play();
    }
    
    void Update()
{
    // Calculate RPM (same as before)
    float currentY = stirrer.localEulerAngles.y;
    float deltaY = Mathf.DeltaAngle(lastYAngle, currentY);
    lastYAngle = currentY;
    float degreesPerSecond = deltaY / Time.deltaTime;
    rpm = Mathf.Abs(degreesPerSecond / 360f * 60f);
    
    // Always keep emission enabled, but control rate
    em.enabled = true;
    
    if (rpm >= rpmThreshold)
    {
        float rate = baseRate + rpm * rateMultiplier;
        em.rateOverTime = Mathf.Clamp(rate, baseRate, 500f);
        
        float t = Mathf.InverseLerp(rpmThreshold, rpmThreshold * 3f, rpm);
        main.startLifetime = Mathf.Lerp(maxLifetime, minLifetime, t);
    }
    else
    {
        em.rateOverTime = 0; // No particles when below threshold
    }
    
    Debug.Log($"[FlocParticles] RPM: {rpm:F1}, RateOverTime: {em.rateOverTime.constant}, Alive: {ps.particleCount}");
}
}