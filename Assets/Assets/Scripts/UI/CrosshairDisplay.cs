using UnityEngine;
using UnityEngine.UI;

// swaps the crosshair image between its targeting and idle looks
// wire a bool game event listener on the aim target event to set targeting
// the sprites live in the crosshair config asset
[RequireComponent(typeof(Image))]
public class CrosshairDisplay : MonoBehaviour
{
    [SerializeField] private CrosshairConfig config;
    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (config == null)
        {
            Debug.LogError($"{name}: no CrosshairConfig assigned...", this);
            config = ScriptableObject.CreateInstance<CrosshairConfig>();
        }
        setTargeting(false);
    }

    public void setTargeting(bool targeting)
    {
        var sprite = config.idleSprite;
        if (targeting)
        {
            sprite = config.targetingSprite;
        }

        if (sprite != null)
        {
            image.sprite = sprite;
        }
    }
}
