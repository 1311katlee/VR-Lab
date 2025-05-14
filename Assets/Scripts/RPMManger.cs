using UnityEngine;

public class RPMManager : MonoBehaviour
{
    public float currentRPM = 0f;

    public void SetRPM(float rpm)
    {
        currentRPM = Mathf.Clamp(rpm, 0f, 300f); // Optional: clamp here
        Debug.Log("RPMManager: RPM set to " + currentRPM);
    }

    public float GetRPM()
    {
        return currentRPM;
    }
}
