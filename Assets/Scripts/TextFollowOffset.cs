using UnityEngine;

public class TextFollowOffset : MonoBehaviour
{
    public Transform targetObject; // Assign pipette
    public Vector3 offset = new Vector3(0, 0.1f, 0); // Height above pipette

    void LateUpdate()
    {
        if (targetObject != null)
        {
            // Position the text in world space above the pipette
            transform.position = targetObject.position + offset;

            // Make the text face the main camera (billboard effect)
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

            // Keep consistent world scale (e.g., small in VR)
            //transform.localScale = Vector3.one * 0.01f;
        }
    }
}
