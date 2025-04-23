using UnityEngine;

public class HighlightOnLook : MonoBehaviour
{
    private Renderer rend;
    private Color originalColor;
    public Color highlightColor = Color.yellow;

    void Start()
    {
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
    }

    public void Highlight()
    {
        rend.material.color = highlightColor;
    }

    public void Unhighlight()
    {
        rend.material.color = originalColor;
    }
}