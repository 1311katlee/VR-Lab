using UnityEngine;

public class test : MonoBehaviour
{
    public ParticleSystem flocParticles;
    public float rpm;

    void Update()
    {
        var emission = flocParticles.emission; // Get actual reference to emission module

        if (rpm > 100f)
        {
            emission.enabled = true;
            emission.rateOverTime = 50f; // Make it something clearly visible
        }
        else
        {
            emission.rateOverTime = 0f; // Turn off particles
            emission.enabled = false;
        }
    }
}
