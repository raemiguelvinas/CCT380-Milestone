using UnityEngine;
using UnityEngine.UI;

// drives a filled radial image from a 0 to 1 value like the swing energy
// wire a float game event listener on the energy event to set value
// the mapping curve and smoothing live in the swing config asset
[RequireComponent(typeof(Image))]
public class FillMeterDisplay : MonoBehaviour
{
    [SerializeField] private SwingConfig config;
    private Image image;
    private float target = 1f;
    private float shown = 1f;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (config == null)
        {
            Debug.LogError($"{name}: no SwingConfig assigned...", this);
            config = ScriptableObject.CreateInstance<SwingConfig>();
        }
        apply();
    }

    // 0 is empty and 1 is full
    public void setValue(float value01)
    {
        target = Mathf.Clamp01(value01);
    }

    private void Update()
    {
        if (config.meter.smoothing <= 0f)
        {
            shown = target;
        }
        else
        {
            // frame rate independent smoothing
            // 1 minus e to the power of negative smoothing times dt is the fraction of the gap to close this frame
            // unscaled time keeps the ui moving even when the game is slowed or paused
            float t = 1f - Mathf.Exp(-config.meter.smoothing * Time.unscaledDeltaTime);
            shown = Mathf.Lerp(shown, target, t);
        }
        apply();
    }

    private void apply()
    {
        // the curve turns the meter value into the fill amount to show
        image.fillAmount = Mathf.Clamp01(config.meter.fillCurve.Evaluate(shown));
    }
}
