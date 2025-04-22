using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 3f;
    public LayerMask interactableLayer;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
            {
                Debug.Log("Hit Interactable: " + hit.transform.name);

                // Example: Call Interact method on that object
                hit.transform.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}