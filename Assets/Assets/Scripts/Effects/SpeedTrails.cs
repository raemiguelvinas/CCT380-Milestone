using UnityEngine;

// turns air stream trails on when the character is moving fast and off when slow
// the trails are particle systems and or trail renderers on the hands and feet
// strength scales with speed
// it uses hysteresis so it doesn't flicker around the threshold
// that means the on speed is higher than the off speed
// the numbers live in the movement config asset
public class SpeedTrails : MonoBehaviour
{
    [SerializeField] private MovementConfig config;
    [SerializeField] private CharacterController source; // leave empty to use the one on this object
    [SerializeField] private TrailRenderer[] trailRenderers;
    [SerializeField] private ParticleSystem[] particleTrails;

    private float smoothedSpeed;
    private bool active;
    private float[] baseWidths;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"{name}: No MovementConfig assigned. Using defaults.", this);
            config = ScriptableObject.CreateInstance<MovementConfig>();
        }

        if (source == null)
        {
            source = GetComponent<CharacterController>();
        }

        // remember each trail's set width so the strength can scale it
        int trailCount = 0;
        if (trailRenderers != null)
        {
            trailCount = trailRenderers.Length;
        }
        baseWidths = new float[trailCount];
        for (int i = 0; i < baseWidths.Length; i++)
        {
            if (trailRenderers[i] != null)
            {
                baseWidths[i] = trailRenderers[i].widthMultiplier;
            }
        }

        applyTrails(false, 0f);
    }

    private void Update()
    {
        if (source == null)
        {
            return;
        }

        float speed = source.velocity.magnitude;

        // frame rate independent smoothing
        // 1 minus e to the power of negative smoothing times dt is the fraction of the gap to close this frame
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 1f - Mathf.Exp(-config.speedTrails.smoothing * Time.deltaTime));

        if (!active && smoothedSpeed >= config.speedTrails.activateSpeed)
        {
            active = true;
        }
        else if (active && smoothedSpeed <= config.speedTrails.deactivateSpeed)
        {
            active = false;
        }

        // inverse lerp turns the speed into a 0 to 1 strength
        // 0 at the off speed and 1 at the full strength speed
        float strength = 0f;
        if (active)
        {
            strength = Mathf.InverseLerp(config.speedTrails.deactivateSpeed, config.speedTrails.fullStrengthSpeed, smoothedSpeed);
        }
        applyTrails(active, strength);
    }

    private void applyTrails(bool on, float strength)
    {
        if (trailRenderers != null)
        {
            for (int i = 0; i < trailRenderers.Length; i++)
            {
                var tr = trailRenderers[i];
                if (tr == null)
                {
                    continue;
                }

                tr.emitting = on;
                if (on)
                {
                    // width goes from the minimum fraction at no strength up to the full width at full strength
                    tr.widthMultiplier = baseWidths[i] * Mathf.Lerp(config.speedTrails.minWidthFraction, 1f, strength);
                }
            }
        }

        if (particleTrails != null)
        {
            for (int i = 0; i < particleTrails.Length; i++)
            {
                var ps = particleTrails[i];
                if (ps == null)
                {
                    continue;
                }

                var emission = ps.emission;
                emission.enabled = on;
                if (on)
                {
                    emission.rateOverTime = config.speedTrails.maxEmissionRate * strength;
                }
            }
        }
    }

    private void OnDisable()
    {
        applyTrails(false, 0f);
    }
}
