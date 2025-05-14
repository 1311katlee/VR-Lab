using UnityEngine;

public class ChemicalDoser : MonoBehaviour
{
    public float doseAmount = 0.5f; // Amount added per dose
    public float maxDose = 10f;

    private float[] beakerDoses = new float[6];

    public void DoseBeaker(int beakerIndex)
    {
        if (beakerIndex < 0 || beakerIndex >= beakerDoses.Length) return;

        beakerDoses[beakerIndex] = Mathf.Min(beakerDoses[beakerIndex] + doseAmount, maxDose);
        Debug.Log($"Beaker {beakerIndex + 1} dosed. Total dose: {beakerDoses[beakerIndex]}");
    }

    public float GetDose(int beakerIndex)
    {
        return beakerDoses[beakerIndex];
    }
}
