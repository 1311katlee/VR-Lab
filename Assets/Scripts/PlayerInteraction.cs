using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 3f;
    public LayerMask interactableLayer;

    private GameObject currentTarget;
    private DialController currentDial;

    void Update()
    {
        // Raycast forward
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            // Check if new object is being looked at
            if (hit.transform.gameObject != currentTarget)
            {
                // Unhighlight previous
                if (currentTarget != null)
                {
                    var oldHighlight = currentTarget.GetComponent<HighlightOnLook>();
                    if (oldHighlight != null) oldHighlight.Unhighlight();
                }

                // Highlight new
                currentTarget = hit.transform.gameObject;
                var newHighlight = currentTarget.GetComponent<HighlightOnLook>();
                if (newHighlight != null) newHighlight.Highlight();

                // Cache dial if available
                currentDial = currentTarget.GetComponent<DialController>();
            }

            // Check for scroll input
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0 && currentDial != null)
            {
                Debug.Log("Scroll input: " + scrollInput);
                currentDial.SendMessage("AdjustDial", scrollInput, SendMessageOptions.DontRequireReceiver);
            }

            // Check for click interaction (e.g., select/unselect)
            if (Input.GetMouseButtonDown(0))
            {
                hit.transform.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
            }
        }
        else
        {
            // If not hitting anything, remove highlight from previous
            if (currentTarget != null)
            {
                var oldHighlight = currentTarget.GetComponent<HighlightOnLook>();
                if (oldHighlight != null) oldHighlight.Unhighlight();
            }

            currentTarget = null;
            currentDial = null;
        }
    }
}
