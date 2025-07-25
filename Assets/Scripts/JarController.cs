using UnityEngine;

public class JarController : MonoBehaviour
{
    public StirrerRotator stirrer;
    public float rpm = 0f;
    public float timer = 0f;
    public bool isMixing = false;

    public Renderer jarRenderer; // Assign this to the visual part of the jar (like glass or base)
    public Color defaultColor = Color.gray;
    public Color highlightColor = Color.yellow;

    public void SetRPM(float newRPM)
    {
        rpm = newRPM;
        if (isMixing)
        {
            stirrer.SetRPM(rpm);
        }
    }

    public void StartMixing(float duration)
    {
        timer = duration;
        isMixing = true;
        stirrer.SetRPM(rpm);
    }

    public void SetHighlight(bool isSelected)
    {
        if (jarRenderer != null)
        {
            jarRenderer.material.color = isSelected ? highlightColor : defaultColor;
        }
    }

    void Update()
    {
        if (isMixing)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                isMixing = false;
                stirrer.SetRPM(0f);
            }
        }
    }
}
