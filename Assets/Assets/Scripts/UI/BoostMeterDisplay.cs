using UnityEngine;
using UnityEngine.UI;

// shows whether the grapple boost is available as on or used up as off
// wire a float game event listener on the grapple charges event to set charges
// the colors live in the grapple config asset
[RequireComponent(typeof(Image))]
public class BoostMeterDisplay : MonoBehaviour
{
    [SerializeField] private GrappleConfig config;
    private Image image;
    private bool on = true;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (config == null)
        {
            Debug.LogError($"{name}: no GrappleConfig assigned...", this);
            config = ScriptableObject.CreateInstance<GrappleConfig>();
        }
        image.color = config.boostMeter.onColor;
    }

    // anything above 0 means a boost is available
    public void setCharges(float charges)
    {
        on = charges > 0.5f;
    }

    private void Update()
    {
        Color target = config.boostMeter.offColor;
        if (on)
        {
            target = config.boostMeter.onColor;
        }

        if (config.boostMeter.fadeSpeed <= 0f)
        {
            image.color = target;
        }
        else
        {
            // frame rate independent smoothing
            // 1 minus e to the power of negative fade speed times dt is the fraction of the gap to close this frame
            // unscaled time keeps the ui fading even when the game is slowed or paused
            float t = 1f - Mathf.Exp(-config.boostMeter.fadeSpeed * Time.unscaledDeltaTime);
            image.color = Color.Lerp(image.color, target, t);
        }
    }
}
