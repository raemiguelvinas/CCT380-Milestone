using UnityEngine;

// draws the grapple rope with a line renderer
// wire three game event listeners to it
// grapple fired calls on cast and grapple hit calls on hit and grapple miss calls on miss
//
// cast means the tip flies from the hand toward the target at a constant speed
// so far targets take longer than near ones
// the rope is a sine wave in a random plane near the hand and straight at the tip
// and it snaps straight the moment the tip lands with a small quick wobble that dies away
// hit means when the pull starts the rope lets go of the hand and stays where it was
// as you get pulled along it and then it reels away
// the pull timing is not affected by the rope because it comes from the grapple
// miss means the tip lands and holds for a moment then retracts into the hand
//
// all the numbers live in the grapple config asset
public class GrappleRope : MonoBehaviour
{
    [SerializeField] private GrappleConfig config;
    [SerializeField] private PlayerMovementInput player;
    [SerializeField] private Transform hand; // the left hand bone
    [SerializeField] private LineRenderer line;
    [Tooltip("Raised with the landing point and the surface normal when the tip hits a surface. Wire a PointNormalGameEventListener -> PrefabSpawner.spawnAtSurface to spawn a particle there.")]
    [SerializeField] private PointNormalGameEvent onLanded;

    private enum State { Idle, Extending, Hit, Miss }
    private State state = State.Idle;
    private Vector3 target; // where the tip is flying to
    private Vector3 landNormal; // the surface normal there
    private Vector3 tip; // where the tip is right now
    private Vector3 anchor; // where the rope stays attached once you're being pulled
    private bool arrived; // has the tip reached the target
    private float sinceArrival; // seconds since it did
    private float waveAngle; // which way the wave swings as an angle around the rope and random per cast
    private Vector3[] positions;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"{name}: No GrappleConfig assigned. Using defaults.", this);
            config = ScriptableObject.CreateInstance<GrappleConfig>();
        }

        if (player == null)
        {
            player = GetComponentInParent<PlayerMovementInput>();
        }

        if (line == null)
        {
            line = GetComponent<LineRenderer>();
        }

        positions = new Vector3[Mathf.Max(2, config.rope.points)];
        line.useWorldSpace = true;
        line.positionCount = positions.Length;
        line.enabled = false;
    }

    // call these from game event listeners
    public void onCast()
    {
        target = player.GrappleTarget;
        landNormal = player.GrappleSurfaceNormal;
        tip = getHandPosition();
        arrived = false;
        sinceArrival = 0f;

        // a random angle around the full circle in radians
        waveAngle = Random.value * Mathf.PI * 2f;
        state = State.Extending;
        line.enabled = true;
    }

    public void onHit()
    {
        if (state == State.Idle)
        {
            return;
        }

        // the rope stops following the hand from here on
        anchor = getHandPosition();
        state = State.Hit;
    }

    public void onMiss()
    {
        if (state == State.Idle)
        {
            return;
        }

        state = State.Miss;
    }

    private void Update()
    {
        if (state == State.Idle)
        {
            return;
        }

        float dt = Time.deltaTime;
        Vector3 handPos = getHandPosition();

        // the tip flies at a constant speed and this keeps going even if the pull starts before it lands
        if (!arrived)
        {
            tip = Vector3.MoveTowards(tip, target, config.rope.ropeSpeed * dt);

            // squared distance is cheaper than a real distance and works fine for a tiny threshold
            if ((tip - target).sqrMagnitude < 0.0001f)
            {
                arrived = true;
                sinceArrival = 0f;
                if (onLanded != null && player.GrappleLandsOnSurface)
                {
                    onLanded.raise(target, landNormal);
                }
            }
        }
        else
        {
            sinceArrival += dt;
        }

        // wavy in flight and snaps straight as soon as the tip lands
        float wave = 1f;
        if (arrived)
        {
            wave = 1f - smooth(sinceArrival, config.rope.straightenTime);
        }

        float widthScale = 1f;
        Vector3 start = handPos;

        switch (state)
        {
            case State.Extending:
                // start follows the hand and the tip flies out
                break;

            case State.Hit:
                // 0 while lingering then 1 once fully reeled away
                float vanish = 0f;
                if (arrived)
                {
                    vanish = Mathf.Clamp01((sinceArrival - config.rope.hitLingerTime) / Mathf.Max(config.rope.hitFadeTime, 0.0001f));
                }

                // reels away toward the target
                start = Vector3.Lerp(anchor, target, vanish);
                widthScale = 1f - vanish;
                if (vanish >= 1f)
                {
                    stop();
                    return;
                }
                break;

            case State.Miss:
                if (arrived && sinceArrival > config.rope.missHoldTime)
                {
                    tip = Vector3.MoveTowards(tip, handPos, config.rope.retractSpeed * dt);
                    if ((tip - handPos).sqrMagnitude < 0.0025f)
                    {
                        stop();
                        return;
                    }
                }
                break;
        }

        // after landing there is a small quick wobble like a plucked string that dies away and leaves it straight
        // a sine wave gives the back and forth and e to the power of negative decay times time shrinks it over time
        float impact = 0f;
        if (arrived)
        {
            float decayFactor = Mathf.Exp(-config.rope.impactDecay * sinceArrival);
            float swingBackAndForth = Mathf.Sin(2f * Mathf.PI * config.rope.impactFrequency * sinceArrival);
            impact = decayFactor * swingBackAndForth;
        }

        line.widthMultiplier = config.rope.width * widthScale;
        draw(start, tip, wave, impact);
    }

    // a rope from a to b
    // the wiggle is a flat sine wave in a random plane
    // zero at the hand and biggest a little way along and gone at the far end
    // wave is 1 for the full wiggle and 0 for straight
    // impact is the landing wobble and swings between negative 1 and 1
    private void draw(Vector3 a, Vector3 b, float wave, float impact)
    {
        Vector3 dir = b - a;
        float len = dir.magnitude;

        if (len < 0.01f)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = a;
            }
            line.SetPositions(positions);
            return;
        }

        dir /= len;

        // two directions at right angles to the rope so we can wiggle in any plane around it
        // the cross product of two directions gives a direction at right angles to both
        Vector3 side = Vector3.Cross(dir, Vector3.up);
        if (side.sqrMagnitude < 0.001f)
        {
            // the rope is pointing straight up or down so use a different reference direction
            side = Vector3.Cross(dir, Vector3.right);
        }
        side.Normalize();
        Vector3 up = Vector3.Cross(side, dir);

        // the direction the wave swings in is a random angle around the rope so each cast waves in a different plane
        // cos and sin of the angle mix the two side directions into one direction at that angle
        float cosAngle = Mathf.Cos(waveAngle);
        float sinAngle = Mathf.Sin(waveAngle);
        Vector3 swing = side * cosAngle + up * sinAngle;

        // a short rope doesn't flail so the size is capped to a fraction of the rope length
        float amplitude = Mathf.Min(config.rope.waveAmplitude, len * 0.2f) * wave;
        float impactAmplitude = Mathf.Min(config.rope.impactWiggle, len * 0.1f) * impact;

        for (int i = 0; i < positions.Length; i++)
        {
            // how far along the rope this point is from 0 at the hand to 1 at the tip
            float f = i / (float)(positions.Length - 1);

            // sine of pi times f is 0 at both ends and 1 in the middle
            // raising f to the bias power moves the biggest wiggle toward the hand when the bias is below 1
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Pow(f, config.rope.waveBias));

            // the phase is the angle along the sine wave
            // f times wave count times a full circle gives the wiggles along the rope
            // subtracting time times speed makes them travel
            float phase = f * config.rope.waveCount * Mathf.PI * 2f - Time.time * config.rope.waveSpeed;
            Vector3 offset = swing * (Mathf.Sin(phase) * amplitude * envelope);

            // the whole rope swinging like a plucked string is a single half sine arc that is zero at both ends
            offset += swing * (impactAmplitude * Mathf.Sin(Mathf.PI * f));

            positions[i] = Vector3.Lerp(a, b, f) + offset;
        }
        line.SetPositions(positions);
    }

    private Vector3 getHandPosition()
    {
        if (hand != null)
        {
            return hand.TransformPoint(config.rope.handOffset);
        }
        return transform.position;
    }

    // eases from 0 to 1 over the duration with a gentle start and end
    private float smooth(float time, float duration)
    {
        float fraction = Mathf.Clamp01(time / Mathf.Max(duration, 0.0001f));
        return Mathf.SmoothStep(0f, 1f, fraction);
    }

    private void stop()
    {
        state = State.Idle;
        line.enabled = false;
    }

    private void OnDisable()
    {
        if (line != null)
        {
            stop();
        }
    }
}
