using UnityEngine;

public class DialController : MonoBehaviour
{
    public float sensitivity = 1.0f;
    public float minRPM = 0f;
    public float maxRPM = 300f;

    [HideInInspector]
    public float currentRPM = 0f;

    public bool isSelected = false;

    private float startYRotation;

    void Start()
    {
        startYRotation = transform.localEulerAngles.y;
    }

    void Update()
    {
        if (isSelected)
        {
            float input = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up, input * sensitivity);

            float rawAngle = transform.localEulerAngles.y - startYRotation;
            if (rawAngle < 0) rawAngle += 360f;
            if (rawAngle > 180f) rawAngle -= 360f;

            float t = Mathf.InverseLerp(0f, 270f, Mathf.Clamp(rawAngle, 0f, 270f));
            currentRPM = Mathf.Lerp(minRPM, maxRPM, t);
        }
    }

    // 👇 This is what SendMessage is calling
    public void Interact()
    {
        isSelected = !isSelected;
        Debug.Log("Dial selected: " + isSelected);
    }
}
