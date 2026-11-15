using UnityEngine;

// tuning for the right click swing
// this also holds the swing meter settings in its own section
// create it from the asset create menu under config
[CreateAssetMenu(menuName = "Config/Swing Config", fileName = "SwingConfig")]
public class SwingConfig : ScriptableObject
{
    [Header("Targeting")]
    [Tooltip("Surfaces with this tag can be swung from.")]
    public string swingTag = "Swingable";
    [Tooltip("Max distance from the camera the swing ray can reach")]
    public float range = 40f;
    [Tooltip("The rope is never shorter than this, so you can't attach to something right next to you.")]
    public float minRopeLength = 3f;
    [Tooltip("Radius of the ball swept along the aim line (metres). Bigger = more forgiving aim")]
    public float aimRadius = 0.8f;

    [Header("Cooldown")]
    [Tooltip("Seconds after letting go (or being let go) before you can start another swing")]
    public float cooldown = 0.15f;

    [Header("Energy (limits how long you can swing)")]
    [Tooltip("Energy used per second while swinging, as a fraction of a full bar. 0.25 = four seconds of swinging")]
    public float drainPerSecond = 0.25f;
    [Tooltip("Energy refilled per second while you're on the ground, as a fraction of a full bar")]
    public float regenPerSecond = 1.5f;
    [Tooltip("You need at least this much energy (0 to 1) to start a swing")]
    public float minEnergyToStart = 0.15f;
    [Tooltip("Energy spent just by starting a swing (0 to 1), on top of the drain while holding. Makes spamming short swings costly.")]
    public float startCost = 0.1f;

    [Header("Feel")]
    [Tooltip("1 = normal gravity. Lower = floatier, longer swings.")]
    public float gravityMultiplier = 0.8f;
    [Tooltip("How hard you can push yourself around while swinging")]
    public float inputAcceleration = 12f;
    [Tooltip("Speed lost per second to air resistance (0.15 = 15%)")]
    public float drag = 0.15f;
    public float maxSpeed = 35f;

    [Header("Rope Catch")]
    [Tooltip("When your path meets the rope's length, the rope turns your motion onto the swing arc instead of stopping it dead. This is how far before full length that starts  Bigger = a gentler, earlier catch.")]
    public float softZone = 3f;
    [Tooltip("How quickly the catch turns your motion onto the arc. Higher = a more complete catch (less speed lost); lower = looser.")]
    public float bendRate = 40f;
    [Range(0f, 1f), Tooltip("How much of your speed the catch keeps while turning you. 1 = all of it, 0 = a hard pendulum that deletes the outward speed. The catch only ever uses the speed you arrived with, so this can't create free energy.")]
    public float speedRetention = 1f;

    [Header("Release")]
    [Tooltip("Multiplies your speed when you let go. Above 1 = a little extra fling.")]
    public float releaseSpeedMultiplier = 1f;
    [Tooltip("The fling bonus builds up over this many seconds of swinging, so quick taps of the button get almost none of it.")]
    public float releaseBonusRampTime = 0.6f;
    [Tooltip("Cap on the sideways speed you keep after letting go.")]
    public float maxReleaseSpeed = 30f;

    [Header("Animation")]
    [Tooltip("A swing shorter than this (in seconds) skips the end flip and plays the interrupt animation instead. Landing mid-swing always does")]
    public float minFlipTime = 0.35f;

    [Header("Body Pose")]
    [Tooltip("The body hangs along the rope (head toward the anchor). Seconds it takes to ease into that pose when the swing starts.")]
    public float poseBlendTime = 0.15f;
    [Tooltip("Extra degrees the head tips back at full lean speed.")]
    public float leanBackAngle = 25f;
    [Tooltip("Swing speed (units/sec) at which the lean is at its maximum.")]
    public float leanFullSpeed = 25f;
    [Tooltip("Extra yaw if the swing animation doesn't lead with the character's forward.")]
    public float facingYawOffset = 0f;

    [Header("Rope")]
    public float ropeWidth = 0.06f;

    [Header("Aim Marker")]
    [Tooltip("Size of the marker showing where a swing would attach.")]
    public float indicatorSize = 0.5f;
    [Tooltip("How much the marker pulses (0 = steady).")]
    public float indicatorPulse = 0.15f;
    public float indicatorPulseSpeed = 6f;
    [Tooltip("The marker shrinks away as the camera gets close to it, so it can never fill the screen. At or below this distance (metres) from the camera it's hidden.")]
    public float indicatorHideDistance = 2f;
    [Tooltip("At or beyond this distance from the camera the marker is full size. Between the hide distance and this, it shrinks smoothly.")]
    public float indicatorFullDistance = 6f;

    [Header("Swing Meter")]
    public SwingMeterSettings meter = new SwingMeterSettings();
}

// how a filled radial meter maps a 0 to 1 value onto the image's fill amount and how smoothly it moves
// the curve is needed because a radial image doesn't fill evenly by angle
// and the swing arc only covers part of the circle
[System.Serializable]
public class SwingMeterSettings
{
    [Tooltip("X = the meter value (0 empty, 1 full). Y = the Image's Fill Amount to show for it")]
    public AnimationCurve fillCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("How fast the meter catches up with the real value. Higher = snappier. 0 = instant.")]
    public float smoothing = 12f;
}
