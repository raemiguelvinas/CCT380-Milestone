using UnityEngine;

// turns trail renderers on for a set time after an event and then off
// the trails fade themselves out using their own time and width curve and color gradient settings
// so there's no fade code here
// hook activate up to a game event listener's response
// how long they stay on lives in the grapple config asset
// use trails that speed trails isn't managing or the two will fight
public class TimedTrails : MonoBehaviour
{
    [SerializeField] private GrappleConfig config;
    [SerializeField] private TrailRenderer[] trailRenderers;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"{name}: No GrappleConfig assigned. Using the default duration.", this);
            config = ScriptableObject.CreateInstance<GrappleConfig>();
        }
        setTrails(false);
    }

    // turn on for the config's duration and calling it again restarts the timer
    public void activate()
    {
        activateFor(config.boostTrail.duration);
    }

    // turn on for a specific number of seconds
    public void activateFor(float seconds)
    {
        setTrails(true);
        CancelInvoke(nameof(turnOff));
        Invoke(nameof(turnOff), seconds);
    }

    private void turnOff()
    {
        setTrails(false);
    }

    private void setTrails(bool on)
    {
        for (int i = 0; i < trailRenderers.Length; i++)
        {
            if (trailRenderers[i] != null)
            {
                trailRenderers[i].emitting = on;
            }
        }
    }
}
