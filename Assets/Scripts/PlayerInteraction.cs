using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 3f;
    public LayerMask interactableLayer;
    private GameObject currentDial;

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            currentDial = hit.collider.gameObject;

            // Scroll wheel input
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0 && currentDial != null)
            {
                currentDial.SendMessage("AdjustDial", scrollInput, SendMessageOptions.DontRequireReceiver);
            }
        }
        else
        {
            currentDial = null;
        }
    }
}
