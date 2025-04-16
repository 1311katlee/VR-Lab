using UnityEngine;

public class StirrerController : MonoBehaviour
{
    public float rotationSpeed = 0f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    public void SetSpeed(float newSpeed)
    {
        rotationSpeed = newSpeed;
    }
}