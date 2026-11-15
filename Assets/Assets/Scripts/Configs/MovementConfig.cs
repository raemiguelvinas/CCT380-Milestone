using UnityEngine;

// core movement tuning for running jumping air dash camera look and landing
// this also holds the wall jump and the run dust and speed trails and wall slide effect settings in their own sections
// create it from the asset create menu under config
[CreateAssetMenu(menuName = "Config/Movement Config", fileName = "MovementConfig")]
public class MovementConfig : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpHeight = 1.5f;
    [Tooltip("Negative number. More negative = falls faster.")]
    public float gravity = -20f;
    [Tooltip("How smoothly the move animation blends (seconds).")]
    public float dampTime = 0.1f;

    [Header("Air Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.25f;
    [Tooltip("Small upward pop at the start of an air dash.")]
    public float dashUpVelocity = 2f;
    [Tooltip("Deg/sec the body turns to face the dash.")]
    public float dashTurnSpeed = 720f;

    [Header("Air Dash Momentum")]
    [Tooltip("Dashing within this many degrees of your momentum adds to it (with no dash burst). Further off than this and it's a normal dash that resets your momentum.")]
    public float dashMomentumAngle = 45f;
    [Tooltip("Speed added to your momentum by a dash along it.")]
    public float dashMomentumBonus = 5f;
    [Tooltip("Dashing can't push your momentum past this (it never lowers momentum you already had).")]
    public float dashMomentumMax = 20f;
    [Tooltip("You need at least this much momentum for a dash to add to it. Below this it's a normal dash (burst, and momentum reset).")]
    public float dashMomentumMinSpeed = 5f;
    [Tooltip("Units/sec the speed a dash added bleeds back off, settling you back to the momentum you had before the dash.")]
    public float dashBonusDecay = 5f;

    [Header("Look")]
    public float mouseSensitivity = 0.1f;
    public float gamepadSensitivity = 120f;
    public float minPitch = -40f;
    public float maxPitch = 70f;

    [Header("Landing")]
    [Tooltip("Fall speed below this doesn't count as a landing.")]
    public float minLandSpeed = 3f;
    [Tooltip("Fall speed at which the landing animation layer is at full weight.")]
    public float hardLandSpeed = 15f;
    [Tooltip("Landing layer weight for the softest landing.")]
    public float minLandWeight = 0.3f;

    [Header("Wall Jump")]
    public WallJumpSettings wallJump = new WallJumpSettings();

    [Header("Run Dust")]
    public RunDustSettings runDust = new RunDustSettings();

    [Header("Speed Trails")]
    public SpeedTrailsSettings speedTrails = new SpeedTrailsSettings();

    [Header("Wall Slide Effect")]
    public WallSlideEffectSettings wallSlideEffect = new WallSlideEffectSettings();
}

// tuning for jumping off walls modelled on deadlock's
[System.Serializable]
public class WallJumpSettings
{
    [Header("Detection")]
    [Tooltip("Layers that count as walls.")]
    public LayerMask wallMask = ~0;
    [Tooltip("How far past your body a wall can be and still be jumpable (metres).")]
    public float checkDistance = 0.4f;
    [Tooltip("Surfaces whose normal tilts more than this up/down (0 = perfectly vertical) aren't walls.")]
    public float maxNormalY = 0.25f;

    [Header("Jump")]
    public float jumpHeight = 1.5f;
    [Tooltip("Speed pushing you off the wall when you hold straight into it. You always leave the wall.")]
    public float minPush = 3f;
    [Tooltip("Speed pushing you off the wall when you hold straight away from it. Between the two it blends with how far you're holding away.")]
    public float maxPush = 7f;
    [Tooltip("Extra speed along the wall in the direction you're holding, on top of the momentum you already had along it.")]
    public float sidewaysSpeed = 3f;
    [Tooltip("Cap on the carried horizontal speed you leave the wall with.")]
    public float maxSpeed = 18f;

    [Header("Push Momentum")]
    [Range(0f, 1f), Tooltip("Fraction of the push you feel on the first frame. It then eases up to full over the ease-in time instead of popping.")]
    public float pushStartFraction = 0.35f;
    [Tooltip("Seconds the push takes to ease in to full strength.")]
    public float pushRampTime = 0.12f;
    [Tooltip("Units/sec^2 the push fades by. Lower = you carry the momentum further.")]
    public float pushDrag = 6f;
    [Range(0f, 1f), Tooltip("How much of your input against the push still counts while the push is at max (it counts for more as the push fades). 0 = can't fight it at all, 1 = no resistance.")]
    public float counterControl = 0.25f;

    [Header("Wall Slide")]
    [Tooltip("How close to the wall (metres past your body) you have to be to count as sliding on it. Smaller than the wall jump check, so you're actually touching it.")]
    public float slideCheckDistance = 0.15f;
    [Tooltip("Seconds the slide carries on if the wall check briefly loses the wall. Stops the animation and gravity flickering.")]
    public float slideGraceTime = 0.1f;
    [Range(0f, 1f), Tooltip("Gravity while sliding, as a fraction of normal. Lower = a slower slide.")]
    public float slideGravityMultiplier = 0.25f;
    [Tooltip("Fastest you can fall while sliding (units/sec).")]
    public float slideMaxFallSpeed = 5f;
    [Tooltip("How hard you're braked down to the slide speed if you hit the wall falling faster (units/sec^2).")]
    public float slideBrake = 40f;

    [Header("Animation")]
    [Tooltip("Wall within this many degrees of straight ahead plays the 'wall forward' slide.")]
    public float forwardHalfAngle = 45f;
    [Tooltip("Wall within this many degrees of straight behind plays no slide animation.")]
    public float backHalfAngle = 45f;
    [Tooltip("Crossfade into the wall jump animation (seconds).")]
    public float animCrossfade = 0.08f;
    [Tooltip("How long after a wall jump the body keeps facing the wall's normal (the way the jump goes), turning at the dash's turn speed.")]
    public float jumpFaceTime = 0.5f;
    [Tooltip("Extra yaw for the wall jump clip, like the dash's -90. If the clip faces the wrong way, try 90, -90 or 180.")]
    public float jumpFaceYawOffset = 0f;

    [Header("Vertical Bounce")]
    [Tooltip("Wall jumping out of a grapple pull is allowed, and ends the pull.")]
    public bool allowDuringPull = true;
    [Tooltip("How long after a pull leaves you moving upward that a wall jump can still carry that speed.")]
    public float bounceWindow = 1f;
    [Range(0f, 1.5f), Tooltip("Fraction of your upward speed added on top of the normal wall jump kick. It only counts if the amount added is above the grapple's minWavejumpSpeed, like a wavejump; then it plays the wall bounce effects (OnWallBounce).")]
    public float bounceCarry = 0.4f;

    [Header("Fatigue")]
    [Tooltip("A wall jump this soon after the last one is lower; it recovers fully over this many seconds. Landing clears it.")]
    public float fatigueTime = 0.5f;
    [Range(0f, 1f), Tooltip("Jump height multiplier right after a wall jump (recovers up to 1 over the fatigue time). Only height is reduced, not the push.")]
    public float fatigueMultiplier = 0.4f;
}

// dust puffs while running
[System.Serializable]
public class RunDustSettings
{
    public GameObject dustPrefab;
    [Tooltip("Min horizontal speed for dust to appear.")]
    public float moveThreshold = 4f;
    [Tooltip("Seconds between puffs.")]
    public float interval = 0.15f;
}

// when the speed trails switch on and how strong they get
[System.Serializable]
public class SpeedTrailsSettings
{
    [Header("Speed (m/s)")]
    [Tooltip("Trails switch on above this speed.")]
    public float activateSpeed = 12f;
    [Tooltip("...and switch off below this one. Keep it a bit lower to avoid flicker.")]
    public float deactivateSpeed = 9f;
    [Tooltip("At this speed the effect is at full strength.")]
    public float fullStrengthSpeed = 25f;
    [Tooltip("Higher = reacts faster to speed changes.")]
    public float smoothing = 15f;

    [Header("Strength")]
    [Tooltip("Particle emission rate at full strength.")]
    public float maxEmissionRate = 40f;
    [Range(0f, 1f), Tooltip("Trail renderers are this fraction of their set width at the activation speed.")]
    public float minWidthFraction = 0.4f;
}

// tuning for the particle effect that follows you down a wall
// used by the wall slide effect script
[System.Serializable]
public class WallSlideEffectSettings
{
    [Tooltip("Only emit once you're actually sliding down at least this fast (units/sec), so there's no dust while you're hanging still.")]
    public float minSlideSpeed = 0.5f;
    [Tooltip("Which of the particle system's axes points away from the wall. Up suits the usual particle setup; Forward suits effects built to shoot along their blue Z arrow.")]
    public PrefabSpawnConfig.AlignAxis alignAxis = PrefabSpawnConfig.AlignAxis.Up;
    [Tooltip("Lift the effect off the wall by this much so it doesn't clip into it.")]
    public float surfaceOffset = 0.05f;
    [Tooltip("Move the effect up (+) or down (-) from the contact point, e.g. down toward your feet or hands. The contact point is at chest height.")]
    public float heightOffset = 0f;
}
