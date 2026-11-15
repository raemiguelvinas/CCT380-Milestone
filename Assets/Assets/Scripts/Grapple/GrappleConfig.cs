using UnityEngine;

// all the tuning numbers for the grapple and momentum mechanic as an asset
// this also holds the grapple camera and rope and boost trail and boost meter settings in their own sections
// create one from the asset create menu under grapple then drag it into the player's grapple slot
// make several like default or heavy or floaty and swap them to change the feel without touching code
[CreateAssetMenu(menuName = "Grapple/Grapple Config", fileName = "NewGrappleConfig")]
public class GrappleConfig : ScriptableObject
{
    [Header("Targeting")]
    public string hookableTag = "Hookable";
    [Tooltip("Max distance from the camera the hook ray can reach")]
    public float range = 40f;
    [Tooltip("Which layers the hook ray can hit")]
    public LayerMask hookMask = ~0;
    [Tooltip("Radius of the ball swept along the aim line (metres). Bigger = more forgiving aim; near 0 behaves like a thin ray")]
    public float aimRadius = 0.5f;

    [Header("Air Uses")]
    [Tooltip("How many grapple boosts you get before touching the ground again. Standing on something refills them")]
    public int airUses = 1;

    [Header("Cast")]
    [Tooltip("Delay before the pull starts. Gravity is stalled during it")]
    public float castTime = 0.2f;
    [Tooltip("Scales movement input while casting (0 = frozen)")]
    public float castMoveMultiplier = 0.3f;
    [Tooltip("Deg/sec the body turns toward the target during the cast")]
    public float castTurnSpeed = 2160f;

    [Header("Pull")]
    public float pullSpeed = 30f;
    [Tooltip("Max pull time. You don't have to reach the target")]
    public float pullDuration = 0.4f;
    [Range(0f, 1f), Tooltip("The pull tapers down to this fraction of Pull Speed")]
    public float endSpeedFraction = 0.4f;
    [Tooltip("The pull ends early when this close to the target")]
    public float stopDistance = 1.5f;
    public float cooldown = 0.3f;
    [Tooltip("A successful grapple gives your air dash back")]
    public bool refreshesAirDash = true;
    [Tooltip("Deg/sec the body turns toward the pull direction")]
    public float turnSpeed = 1080f;
    [Tooltip("Extra yaw if the grapple animation doesn't lead with the character's forward")]
    public float facingYawOffset = 0f;

    [Header("Momentum")]
    [Tooltip("Carry the pull's horizontal speed after it ends")]
    public bool keepMomentum = true;
    [Tooltip("Fraction of the pull's horizontal speed you keep")]
    public float momentumScale = 0.6f;
    public float maxMomentum = 15f;
    [Tooltip("Units/sec^2 of momentum lost while airborne")]
    public float airDrag = 4f;
    [Tooltip("Units/sec^2 of momentum lost on the ground (after the wavejump window.")]
    public float groundFriction = 40f;

    [Header("Air Steering")]
    [Tooltip("Max deg/sec you can swing carried momentum toward your input")]
    public float steerSpeed = 360f;
    [Tooltip("Units/sec^2 of momentum lost while pushing directly against it")]
    public float brake = 20f;
    [Tooltip("Fraction of speed lost per radian turned")]
    public float turnLoss = 0.3f;

    [Header("Wavejump")]
    [Tooltip("A jump press this long before landing still counts")]
    public float jumpBufferTime = 0.1f;
    [Tooltip("Jump within this long of landing to keep your momentum")]
    public float wavejumpWindow = 0.1f;
    [Tooltip("Minimum momentum for a jump to count as a wavejump.")]
    public float minWavejumpSpeed = 2f;
    public float wavejumpSpeedMultiplier = 1.1f;
    public float wavejumpHeightMultiplier = 0.7f;

    [Header("Wavejump Animation")]
    [Tooltip("Animator state names; one is picked at random")]
    public string[] waveJumpStates = { "WaveJumpA", "WaveJumpB", "WaveJumpC" };
    [Tooltip("Momentum needed to play these instead of the normal jump anim")]
    public float waveJumpAnimMinSpeed = 5f;
    public float waveJumpCrossfade = 0.05f;

    [Header("Grapple Camera")]
    public GrappleCameraSettings camera = new GrappleCameraSettings();

    [Header("Grapple Rope")]
    public GrappleRopeSettings rope = new GrappleRopeSettings();

    [Header("Boost Trail")]
    public BoostTrailSettings boostTrail = new BoostTrailSettings();

    [Header("Boost Meter")]
    public BoostMeterSettings boostMeter = new BoostMeterSettings();
}

// how the camera reacts to the grapple and to wavejumps
[System.Serializable]
public class GrappleCameraSettings
{
    [Header("Cast (zoom in)")]
    [Tooltip("Camera distance is multiplied by this while casting. Below 1 = closer.")]
    public float castDistanceMultiplier = 0.7f;
    [Tooltip("Added to the FOV while casting ")]
    public float castFovChange = -4f;
    [Tooltip("How fast it eases into the cast look. Higher = quicker.")]
    public float castSmoothing = 14f;

    [Header("Boost (FOV out)")]
    [Tooltip("Added to the FOV during the grapple boost")]
    public float boostFovChange = 22f;
    [Tooltip("Camera distance multiplier during the boost. 1 = normal distance.")]
    public float boostDistanceMultiplier = 1.1f;
    public float boostSmoothing = 9f;

    [Header("Swing")]
    [Range(0f, 1f), Tooltip("Camera side while swinging. 1 = the normal over the shoulder offset, 0 = centered behind player.")]
    public float swingCameraSide = 0.6f;
    [Tooltip("Camera distance is multiplied by this while swinging. Above 1 = further back.")]
    public float swingDistanceMultiplier = 1.25f;
    public float swingSmoothing = 6f;

    [Header("Walls")]
    [Tooltip("If a wall is on the camera's shoulder side, slide the camera toward the middle just far enough to stay clear of it. stops the camera collapsing onto you when you're against a wall")]
    public bool centerNearWalls = true;
    [Tooltip("Extra gap kept between the camera's shoulder position and the wall, on top of the camera's own radius")]
    public float wallClearance = 0.2f;
    [Tooltip("How fast the camera slides toward the middle near a wall. Higher = quicker.")]
    public float wallCenterSmoothing = 10f;
    [Tooltip("After a wall stops pushing the camera in it waits this long before easing back out (stops weird camera jerking issues)")]
    public float wallReleaseDelay = 0.35f;

    [Header("Return to normal")]
    public float returnSmoothing = 4f;

    [Header("Wavejump kick")]
    [Tooltip("Extra FOV that pops in on a wavejump, then fades.")]
    public float wavejumpFovKick = 8f;
    [Tooltip("How long the kick holds before fading (seconds)")]
    public float kickHoldTime = 0.08f;
    public float kickRiseSmoothing = 22f;
    public float kickFallSmoothing = 6f;
}

// how the grapple rope looks and moves
[System.Serializable]
public class GrappleRopeSettings
{
    [Header("Shape")]
    [Tooltip("Nudge the start point, in the hand's local space")]
    public Vector3 handOffset;
    public int points = 24;
    public float width = 0.06f;
    [Tooltip("How far the rope wobbles (metres).")]
    public float waveAmplitude = 0.4f;
    [Tooltip("How many wiggles along the rope.")]
    public float waveCount = 3f;
    [Tooltip("How fast the wiggles travel.")]
    public float waveSpeed = 14f;
    [Tooltip("Where along the rope the wiggle is biggest. Lower = bunched up near the hand (the tip is always straight); 1 = in the middle.")]
    public float waveBias = 0.4f;

    [Header("Landing Wobble")]
    [Tooltip("Size of the little wobble when the tip hits (metres)")]
    public float impactWiggle = 0.15f;
    [Tooltip("Wobbles per second.")]
    public float impactFrequency = 14f;
    [Tooltip("How fast the wobble dies out. Higher = quicker.")]
    public float impactDecay = 14f;

    [Header("Speed (metres per second)")]
    [Tooltip("How fast the tip flies out, regardless of distance.")]
    public float ropeSpeed = 120f;
    [Tooltip("How fast a missed rope comes back.")]
    public float retractSpeed = 120f;

    [Header("Timing (seconds)")]
    [Tooltip("How quickly the rope snaps straight once the tip lands")]
    public float straightenTime = 0.04f;
    [Tooltip("After landing and being pulled, the straight rope stays up this long.")]
    public float hitLingerTime = 0.1f;
    [Tooltip("Then it reels away over this long.")]
    public float hitFadeTime = 0.2f;
    [Tooltip("A missed rope waits this long before retracting.")]
    public float missHoldTime = 0.12f;
}

// how long a set of timed trails stays on
[System.Serializable]
public class BoostTrailSettings
{
    [Tooltip("Seconds the trails stay on after being triggered.")]
    public float duration = 0.5f;
}

// how the grapple boost indicator looks when you have a boost and when you don't
// set the off color's alpha to 0 to hide it completely while it's used up
[System.Serializable]
public class BoostMeterSettings
{
    public Color onColor = Color.white;
    public Color offColor = new Color(1f, 1f, 1f, 0.25f);
    [Tooltip("How fast it fades between on and off. Higher = snappier. 0 = instant.")]
    public float fadeSpeed = 14f;
}
