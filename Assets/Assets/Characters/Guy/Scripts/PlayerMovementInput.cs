using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator), typeof(CharacterController))]
public class PlayerMovementInput : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string dashActionName = "Dash";
    [SerializeField] private string grappleActionName = "Grapple";
    [SerializeField] private string swingActionName = "Swing";

    [Header("Animator Parameters")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string dashTrigger = "AirDash";
    [SerializeField] private string hookCastTrigger = "HookCast"; // throwing the hook
    [SerializeField] private string hookPullTrigger = "HookPull"; // being pulled toward the hit
    [SerializeField] private string groundedParam = "Grounded";
    [SerializeField] private string swingStartTrigger = "SwingStart"; // fired when a swing begins and skipped if the parameter doesn't exist
    [SerializeField] private string swingEndTrigger = "SwingEnd"; // fired when a swing ends and skipped if the parameter doesn't exist
    [SerializeField] private string swingInterruptTrigger = "SwingInterrupt"; // fired instead of swing end when the swing is cut short by landing or was very brief and skipped if missing

    [Header("Landing Animation")]
    [SerializeField] private string landTrigger = "Land"; // trigger for the additive landing layer and skipped if the parameter doesn't exist
    [SerializeField] private string landLayerName = "Landing"; // additive layer whose weight is scaled by impact and skipped if missing

    [Header("Camera Look")]
    [SerializeField] private Transform cameraTarget;

    [Header("Grapple Hook")]
    [SerializeField] private Camera aimCamera; // leave empty to use the main camera

    [Header("Configs (all the tuning numbers live in these assets)")]
    [SerializeField] private MovementConfig movement; // running and jumping and air dash and look and landing and wall jump
    [SerializeField] private GrappleConfig grapple; // grapple and momentum and wavejump
    [SerializeField] private SwingConfig swing; // the right click swing
    [SerializeField] private string wallSlideParam = "WallSlide"; // animator int where 0 is none and 1 is wall ahead and 2 is wall on the left and 3 is wall on the right and skipped if missing
    [SerializeField] private string wallJumpState = "Wall Jump"; // animator state played on a wall jump

    [Header("Event Channels (all optional)")]
    [SerializeField] private GameEvent onGrappleFired; // click and the cast begins
    [SerializeField] private GameEvent onGrappleHit; // cast found a target and the pull begins
    [SerializeField] private GameEvent onGrappleMiss; // cast found nothing
    [SerializeField] private GameEvent onGrappleEnded; // the grapple is over because the pull finished or a missed cast gave up
    [SerializeField] private GameEvent onWavejump;
    [SerializeField] private Vector3GameEvent onAirDash; // carries the dash direction
    [SerializeField] private GameEvent onJump; // any jump and wavejumps raise the wavejump event as well
    [SerializeField] private FloatGameEvent onLanded; // carries the fall speed
    [SerializeField] private GameEvent onSwingStart;
    [SerializeField] private GameEvent onSwingEnd;
    [SerializeField] private FloatGameEvent onSwingEnergyChanged; // 0 to 1 for a ui bar
    [SerializeField] private FloatGameEvent onGrappleChargesChanged; // grapple uses left for the ui
    [SerializeField] private BoolGameEvent onAimTargetChanged; // true while the crosshair is on something you can grapple or swing from
    [SerializeField] private PointNormalGameEvent onWallJump; // where on the wall you jumped from and the wall's normal for a prefab rotated to the wall
    [SerializeField] private PointNormalGameEvent onWallBounce; // a wall jump that carried boost speed so the wavejump effects turned to the wall
    [SerializeField] private PointNormalGameEvent onWallSlideStart; // contact point and wall normal when a slide begins
    [SerializeField] private GameEvent onWallSlideEnd;

    public Vector2 MoveInput { get; private set; }

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction grappleAction;
    private InputAction swingAction;
    private Animator animator;
    private bool hasSwingStartTrigger;
    private bool hasSwingEndTrigger;
    private bool hasSwingInterruptTrigger;
    private CharacterController charController;

    private float yaw;
    private float pitch;
    private float verticalVelocity;

    private bool isDashing;
    private bool airDashUsed; // one dash per airtime
    private bool realigning; // turning back to face the camera after a dash
    private float dashTimer;
    private Vector3 dashDir; // world direction locked at dash start
    private bool dashKeepsMomentum; // this dash is adding to your momentum so it skips the burst and cuts straight to the new speed
    private float dashBonusLeft; // how much of the dash's added speed is still bleeding back off

    private enum HookState { None, Casting, Pulling }
    private HookState hookState = HookState.None;
    private float hookTimer;
    private float hookCooldownTimer;
    private bool hookHit; // did the cast ray find a hookable
    private Vector3 hookPoint; // world point the ray hit
    private Vector3 hookDir; // world direction of the pull locked at pull start
    private Vector3 hookAimDir; // direction of the crosshair ray at the moment you cast
    private Vector3 boost; // carried horizontal momentum from grapple pulls and added on top of input movement
    private float jumpBufferTimer;
    private float groundedTime; // seconds continuously grounded and 0 while airborne
    private bool wasGrounded;
    private int landLayerIndex = -1;
    private bool hasLandTrigger;

    // fired once on touchdown with the fall speed in meters per second
    // handy for vfx and camera shake and audio later
    public event System.Action<float> Landed;

    // where the grapple ray ended on the last cast
    // that is the hook point on a hit and the blocking surface or max range on a miss
    public Vector3 GrappleTarget
    {
        get
        {
            return lastRayEnd;
        }
    }

    // how long the cast takes and the rope uses this so it lands exactly when the cast ends
    public float GrappleCastTime
    {
        get
        {
            return grapple.castTime;
        }
    }

    // the surface normal where the last cast ended and it points away from the surface
    public Vector3 GrappleSurfaceNormal
    {
        get
        {
            return lastHitNormal;
        }
    }

    // true if the last cast ended on a surface whether hookable or not
    // false if it just flew to max range in open air
    public bool GrappleLandsOnSurface
    {
        get
        {
            return lastResult == CastResult.Hookable || lastResult == CastResult.Blocked;
        }
    }

    [Header("Grapple Debug")]
    [SerializeField] private bool logHookResults = true; // print what the hook ray hit to the console
    [SerializeField] private bool drawHookGizmos = true;

    // last cast stored so the gizmo drawing can draw exactly what the hook search calculated
    private enum CastResult { None, Miss, Blocked, Hookable }
    private CastResult lastResult = CastResult.None;
    private Vector3 lastRayOrigin;
    private Vector3 lastRayEnd;
    private Vector3 lastHitPoint;
    private Vector3 lastHitNormal = Vector3.up;

    // swing state
    private bool isSwinging;
    private Vector3 swingAnchor;
    private float swingRopeLength;
    private Vector3 swingVelocity;

    public bool IsSwinging
    {
        get
        {
            return isSwinging;
        }
    }

    public bool IsWallSliding
    {
        get
        {
            return wallSliding;
        }
    }

    // valid while sliding
    public Vector3 WallNormal
    {
        get
        {
            return wallSlideNormal;
        }
    }

    public Vector3 WallPoint
    {
        get
        {
            return wallSlidePoint;
        }
    }

    public Vector3 SwingAnchor
    {
        get
        {
            return swingAnchor;
        }
    }

    private float lastWallJumpTime = -10f; // for wall jump fatigue and the push's ease in
    private Vector3 wallPush; // the push off a wall and its own momentum that boost braking and steering can't touch
    private bool wallSliding;
    private Vector3 wallSlideNormal;
    private Vector3 wallSlidePoint;
    private float wallSlideGrace; // keeps the slide alive for a moment if the wall check flickers
    private int wallAnimValue = -1; // last value sent to the wall slide animator int
    private bool hasWallSlideParam;
    private float wallJumpFaceTimer; // how long the body keeps facing the wall normal after a wall jump
    private Vector3 wallJumpFaceDir;
    private float lastBoostUpTime = -10f; // when a grapple pull last left you moving upward
    private float swingCooldownTimer; // counts down after a swing ends
    private float swingTime; // how long the current swing has lasted
    private float swingPoseBlend; // goes from 0 to 1 as the body eases into the hanging pose
    private Vector3 swingFacing; // horizontal direction the body faces for the whole swing and it's the way you were facing when it started
    private Quaternion swingPoseFrom; // the body's rotation when the swing started

    // resources and the ui will read these or listen to the events later
    private float swingEnergy = 1f;
    private int grappleCharges;

    public float SwingEnergy01
    {
        get
        {
            return swingEnergy;
        }
    }

    public int GrappleCharges
    {
        get
        {
            return grappleCharges;
        }
    }

    public int MaxGrappleCharges
    {
        get
        {
            if (grapple != null)
            {
                return grapple.airUses;
            }
            return 0;
        }
    }

    // where a swing would attach right now for the aim marker
    private bool aimTargetValid;
    private bool swingPreviewValid;
    private Vector3 swingPreviewPoint;

    public bool SwingPreviewValid
    {
        get
        {
            return swingPreviewValid;
        }
    }

    public bool AimTargetValid
    {
        get
        {
            return aimTargetValid;
        }
    }

    public Vector3 SwingPreviewPoint
    {
        get
        {
            return swingPreviewPoint;
        }
    }

    // ball sweep results that are reused every cast so it doesn't make garbage
    private readonly RaycastHit[] castBuffer = new RaycastHit[32];
    private static readonly HitDistanceComparer hitComparer = new HitDistanceComparer();

    private class HitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        // compare keeps its capital letter because the interface needs that name
        public int Compare(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        charController = GetComponent<CharacterController>();

        if (movement == null)
        {
            Debug.LogError($"{name}: No MovementConfig assigned. Using defaults... assign one in the inspector.", this);
            movement = ScriptableObject.CreateInstance<MovementConfig>();
        }

        if (grapple == null)
        {
            Debug.LogError($"{name}: No GrappleConfig assigned. Using defaults... assign one in the inspector.", this);
            grapple = ScriptableObject.CreateInstance<GrappleConfig>();
        }

        if (swing == null)
        {
            Debug.LogError($"{name}: No SwingConfig assigned. Using defaults... assign one in the inspector.", this);
            swing = ScriptableObject.CreateInstance<SwingConfig>();
        }

        grappleCharges = grapple.airUses;
        swingEnergy = 1f;

        // cache the animator bits once so a missing layer or parameter just skips the animation instead of spamming warnings
        landLayerIndex = animator.GetLayerIndex(landLayerName);
        hasLandTrigger = hasParameter(landTrigger, AnimatorControllerParameterType.Trigger);
        hasSwingStartTrigger = hasParameter(swingStartTrigger, AnimatorControllerParameterType.Trigger);
        hasSwingEndTrigger = hasParameter(swingEndTrigger, AnimatorControllerParameterType.Trigger);
        hasSwingInterruptTrigger = hasParameter(swingInterruptTrigger, AnimatorControllerParameterType.Trigger);
        hasWallSlideParam = hasParameter(wallSlideParam, AnimatorControllerParameterType.Int);

        if (inputActions == null)
        {
            Debug.LogError($"{name}: No InputActionAsset assigned...");
            return;
        }

        var map = inputActions.FindActionMap(actionMapName, throwIfNotFound: false);
        if (map == null)
        {
            Debug.LogError($"{name}: Action map '{actionMapName}' not found...");
            return;
        }

        moveAction = map.FindAction(moveActionName, false);
        lookAction = map.FindAction(lookActionName, false);
        jumpAction = map.FindAction(jumpActionName, false);
        dashAction = map.FindAction(dashActionName, false);
        grappleAction = map.FindAction(grappleActionName, false);
        swingAction = map.FindAction(swingActionName, false);

        yaw = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        setActionEnabled(moveAction, true);
        setActionEnabled(lookAction, true);
        setActionEnabled(jumpAction, true);
        setActionEnabled(dashAction, true);
        setActionEnabled(grappleAction, true);
        setActionEnabled(swingAction, true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        setActionEnabled(moveAction, false);
        setActionEnabled(lookAction, false);
        setActionEnabled(jumpAction, false);
        setActionEnabled(dashAction, false);
        setActionEnabled(grappleAction, false);
        setActionEnabled(swingAction, false);
    }

    // an action can be missing if the input asset is missing it so skip those
    private void setActionEnabled(InputAction action, bool enable)
    {
        if (action == null)
        {
            return;
        }

        if (enable)
        {
            action.Enable();
        }
        else
        {
            action.Disable();
        }
    }

    private void Update()
    {
        handleLook();
        handleMovement();

        // after body rotation so the camera doesn't jerk
        applyCameraRotation();
    }

    // only updates the yaw and pitch numbers and the rotation is applied later
    private void handleLook()
    {
        if (lookAction == null)
        {
            return;
        }

        Vector2 look = lookAction.ReadValue<Vector2>();

        // mouse delta is already per frame but a stick needs delta time
        bool isGamepad = false;
        if (lookAction.activeControl != null)
        {
            isGamepad = lookAction.activeControl.device is Gamepad;
        }

        float scale = movement.mouseSensitivity;
        if (isGamepad)
        {
            scale = movement.gamepadSensitivity * Time.deltaTime;
        }

        yaw += look.x * scale;

        // moving the mouse up looks up so pitch goes down and use plus equals here for inverted y
        pitch -= look.y * scale;
        pitch = Mathf.Clamp(pitch, movement.minPitch, movement.maxPitch);
    }

    private void handleMovement()
    {
        if (moveAction != null)
        {
            MoveInput = moveAction.ReadValue<Vector2>();
        }
        else
        {
            MoveInput = Vector2.zero;
        }

        // the camera's left and right angle only and this makes input camera relative
        // multiplying a direction by this rotation turns it to follow where the camera looks
        Quaternion camYaw = Quaternion.Euler(0f, yaw, 0f);

        // ground check
        bool grounded = charController.isGrounded;
        if (grounded)
        {
            groundedTime += Time.deltaTime;

            // landed mid dash so turn back smoothly
            if (isDashing)
            {
                realigning = true;
            }
            airDashUsed = false;
            isDashing = false;

            // avoid being stuck in the dash state
            animator.ResetTrigger(dashTrigger);

            // landing clears wall jump fatigue
            lastWallJumpTime = -10f;
            wallPush = Vector3.zero;
        }
        else
        {
            groundedTime = 0f;
        }

        // standing on something refills the grapple and the swing
        if (grounded && !isSwinging)
        {
            refillResources(Time.deltaTime);
        }

        // landing fires once on the frame you touch down
        // vertical velocity is still the fall speed here because the ground stick further down hasn't run yet
        // falling is negative so flipping the sign gives the fall speed as a positive number
        if (grounded && !wasGrounded)
        {
            handleLanded(-verticalVelocity);
        }
        wasGrounded = grounded;

        // jump buffer remembers a press for a moment so jumping right as you land isn't frame perfect
        jumpBufferTimer -= Time.deltaTime;
        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            jumpBufferTimer = grapple.jumpBufferTime;
        }

        // what's under the crosshair and this drives the aim marker and the crosshair icon
        updateSwingPreview(grounded);
        updateAimTarget();

        // swing by holding right click and while swinging it fully takes over movement
        if (updateSwing(camYaw, grounded))
        {
            endWallSlide();
            animator.SetBool(groundedParam, grounded);
            animator.SetFloat(moveXParam, MoveInput.x, movement.dampTime, Time.deltaTime);
            animator.SetFloat(moveYParam, MoveInput.y, movement.dampTime, Time.deltaTime);
            return;
        }

        // grapple hook that goes from cast to pull and it runs before jump and dash so it can block them
        updateHook();

        // you can jump out of a pull that has you on the ground like grappling at the floor ahead of you
        // the small age check ignores the stale grounded flag on the first frames of a pull launched from the ground
        bool pullGrounded = hookState == HookState.Pulling && grounded && hookTimer < grapple.pullDuration - 0.03f;
        bool wantsJump = jumpBufferTimer > 0f && grounded && (hookState == HookState.None || pullGrounded);

        if (wantsJump)
        {
            bool fromPull = hookState == HookState.Pulling;

            // this stores the pull's speed as momentum and there's no landing because we're jumping
            if (fromPull)
            {
                endHook(false);
            }
            jumpBufferTimer = 0f;

            // wavejump means jumping within a moment of landing or straight out of a pull keeps your momentum
            // any later and ground friction has already eaten it
            float height = movement.jumpHeight;
            string jumpState = "Jump";
            if (grapple.keepMomentum && boost.magnitude > grapple.minWavejumpSpeed && (fromPull || groundedTime <= grapple.wavejumpWindow))
            {
                boost = Vector3.ClampMagnitude(boost * grapple.wavejumpSpeedMultiplier, grapple.maxMomentum);
                if (onWavejump != null)
                {
                    onWavejump.raise();
                }
                height *= grapple.wavejumpHeightMultiplier;

                // fast enough so play one of the wavejump anims at random instead of the normal jump
                if (boost.magnitude >= grapple.waveJumpAnimMinSpeed && grapple.waveJumpStates != null && grapple.waveJumpStates.Length > 0)
                {
                    jumpState = grapple.waveJumpStates[Random.Range(0, grapple.waveJumpStates.Length)];
                }
            }

            if (onJump != null)
            {
                onJump.raise();
            }

            // the launch speed needed to reach a height comes from energy
            // speed squared equals 2 times gravity times height so speed is the square root of that
            // gravity is negative here so the negative 2 makes the number under the root positive
            verticalVelocity = Mathf.Sqrt(height * -2f * movement.gravity);

            // don't leave a stale trigger behind
            animator.ResetTrigger(jumpTrigger);

            // play jump directly instead of racing the landing transitions
            animator.CrossFadeInFixedTime(jumpState, grapple.waveJumpCrossfade, 0);
        }
        else if (jumpBufferTimer > 0f && !grounded && (hookState == HookState.None || (hookState == HookState.Pulling && movement.wallJump.allowDuringPull)))
        {
            tryWallJump(camYaw);
        }

        // sliding down a wall gives a slower fall and the wall slide animations
        updateWallSlide(camYaw, grounded);

        bool hookBusy = hookState != HookState.None;
        if (grounded && verticalVelocity < 0f && !hookBusy)
        {
            // keeps is grounded reliable
            verticalVelocity = -2f;
        }

        // start air dash
        if (!grounded && !isDashing && !airDashUsed && !hookBusy && dashAction != null && dashAction.WasPressedThisFrame())
        {
            Vector3 input = new Vector3(MoveInput.x, 0f, MoveInput.y);

            // shift alone means forward
            if (input.sqrMagnitude < 0.01f)
            {
                input = Vector3.forward;
            }

            dashDir = camYaw * input.normalized;
            dashTimer = movement.dashDuration;
            isDashing = true;
            airDashUsed = true;

            // a new dash cancels any realigning
            realigning = false;
            verticalVelocity = movement.dashUpVelocity;

            // dashing along your momentum that is fast enough and within the angle adds to it
            // there's no burst so your speed just jumps to the new value and the added part bleeds back off
            // otherwise it's a normal dash and throws your momentum away
            Vector3 flatDash = new Vector3(dashDir.x, 0f, dashDir.z).normalized;
            wallPush = Vector3.zero;
            wallJumpFaceTimer = 0f;

            // the angle between two directions is small when they point the same way
            dashKeepsMomentum = boost.magnitude >= movement.dashMomentumMinSpeed && Vector3.Angle(boost, flatDash) <= movement.dashMomentumAngle;
            if (dashKeepsMomentum)
            {
                float before = boost.magnitude;

                // add the bonus along the dash direction but never push past the max
                // unless the momentum was already above the max in which case it can't be lowered
                boost = Vector3.ClampMagnitude(boost + flatDash * movement.dashMomentumBonus, Mathf.Max(movement.dashMomentumMax, before));

                // how much speed the dash actually added so it can bleed back off later
                dashBonusLeft = Mathf.Max(0f, boost.magnitude - before);
            }
            else
            {
                boost = Vector3.zero;
                dashBonusLeft = 0f;
            }

            animator.SetTrigger(dashTrigger);
            if (onAirDash != null)
            {
                onAirDash.raise(dashDir);
            }

            // if the anim is delayed after jump to fall try cross fading to the air dash state over a very short time
        }

        // horizontal velocity and body rotation
        Vector3 horizontal;
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            // t goes from 0 to 1 over the dash and the speed tapers from dash speed to 0
            float t = 1f - Mathf.Clamp01(dashTimer / movement.dashDuration);
            if (dashKeepsMomentum)
            {
                // same as normal movement with no drop
                horizontal = Vector3.ClampMagnitude(camYaw * new Vector3(MoveInput.x, 0f, MoveInput.y), 1f) * movement.moveSpeed + boost;
            }
            else
            {
                horizontal = dashDir * Mathf.Lerp(movement.dashSpeed, 0f, t);
            }

            // the dash anim leads with the character's right hence the negative 90 offset
            Quaternion target = Quaternion.LookRotation(dashDir) * Quaternion.Euler(0f, -90f, 0f);

            // rotate towards turns by at most speed times dt degrees each frame
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, movement.dashTurnSpeed * Time.deltaTime);

            if (dashTimer <= 0f)
            {
                isDashing = false;

                // dash over so turn back toward the camera
                realigning = true;
            }
        }
        else
        {
            // normal input movement plus any carried grapple momentum on top
            // clamping the length to 1 stops diagonal input from being faster than straight input
            Vector3 runInput = Vector3.ClampMagnitude(camYaw * new Vector3(MoveInput.x, 0f, MoveInput.y), 1f) * movement.moveSpeed;

            // the wall jump push eases in over a moment and then bleeds off slowly
            // holding against it only counts for more the less of it is left
            // so you can't just cancel it the instant you press the other way
            Vector3 push = Vector3.zero;
            if (wallPush.sqrMagnitude > 0.0001f)
            {
                Vector3 pushDir = wallPush.normalized;

                // the dot product is how much of the input points along the push
                // negative means holding against the push
                float against = Vector3.Dot(runInput, pushDir);
                if (against < 0f)
                {
                    // push left is 1 when the push is at its max and goes toward 0 as it fades
                    float pushLeft = Mathf.Clamp01(wallPush.magnitude / Mathf.Max(movement.wallJump.maxPush, 0.01f));

                    // control is how much of your input still counts and it goes from full control with no push
                    // down to the counter control amount at a full push
                    float control = Mathf.Lerp(1f, movement.wallJump.counterControl, pushLeft);

                    // take away the part of the input that fights the push in proportion to the lost control
                    runInput -= pushDir * against * (1f - control);
                }

                // ramp goes from 0 to 1 over the ramp time since the wall jump
                float ramp = Mathf.Clamp01((Time.time - lastWallJumpTime) / Mathf.Max(movement.wallJump.pushRampTime, 0.0001f));

                // the push starts at a fraction of its strength and eases up to full
                push = wallPush * Mathf.Lerp(movement.wallJump.pushStartFraction, 1f, ramp);
                wallPush = Vector3.MoveTowards(wallPush, Vector3.zero, movement.wallJump.pushDrag * Time.deltaTime);
            }
            horizontal = runInput + boost + push;

            // grapple cast and pull make you face the target including up and down like the air dash does
            // during the cast you face the target or where you aimed if the cast missed
            Vector3 hookFaceDir = Vector3.zero;
            if (hookState == HookState.Pulling)
            {
                hookFaceDir = hookDir;
            }
            else if (hookState == HookState.Casting)
            {
                if (hookHit)
                {
                    hookFaceDir = (hookPoint - charController.bounds.center).normalized;
                }
                else
                {
                    hookFaceDir = hookAimDir;
                }
            }

            if (hookFaceDir.sqrMagnitude > 0.0001f)
            {
                Quaternion hookTarget = Quaternion.LookRotation(hookFaceDir) * Quaternion.Euler(0f, grapple.facingYawOffset, 0f);

                // snap round fast during the short cast
                float faceTurnSpeed = grapple.turnSpeed;
                if (hookState == HookState.Casting)
                {
                    faceTurnSpeed = grapple.castTurnSpeed;
                }
                transform.rotation = Quaternion.RotateTowards(transform.rotation, hookTarget, faceTurnSpeed * Time.deltaTime);

                // when the pull ends ease back upright toward the camera
                realigning = true;
            }
            else if (wallJumpFaceTimer > 0f)
            {
                // wall jump anim faces the wall's normal the way the air dash faces its direction
                // it uses the same turn speed plus an offset for the clip
                wallJumpFaceTimer -= Time.deltaTime;
                Quaternion wallTarget = Quaternion.LookRotation(wallJumpFaceDir) * Quaternion.Euler(0f, movement.wallJump.jumpFaceYawOffset, 0f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, wallTarget, movement.dashTurnSpeed * Time.deltaTime);

                // when it's over ease back toward the camera
                realigning = true;
            }
            else if (realigning)
            {
                // smoothly turn back to face the camera after a dash or grapple
                transform.rotation = Quaternion.RotateTowards(transform.rotation, camYaw, movement.dashTurnSpeed * Time.deltaTime);

                if (Quaternion.Angle(transform.rotation, camYaw) < 0.5f)
                {
                    // close enough so lock back on
                    realigning = false;
                }
            }
            else
            {
                // normal means face the camera direction
                transform.rotation = camYaw;
            }
        }

        // air steering
        // carried momentum can be turned toward your input but
        // the more your input opposes it the slower it turns and head on means no turn
        // turning costs speed so zig zagging bleeds momentum instead of swinging it back and forth
        // pushing against it past 90 degrees brakes it instead of redirecting it
        if (!grounded && !isDashing && hookState != HookState.Pulling && boost.sqrMagnitude > 0.0001f && MoveInput.sqrMagnitude > 0.01f)
        {
            Vector3 want = (camYaw * new Vector3(MoveInput.x, 0f, MoveInput.y)).normalized;
            Vector3 dir = boost.normalized;
            float speed = boost.magnitude;

            // the dot product of two directions is 1 for the same way and 0 for sideways and negative 1 for straight against
            float align = Vector3.Dot(dir, want);

            if (align < 0f)
            {
                // braking gets stronger the more directly you push against the momentum
                speed = Mathf.Max(0f, speed - grapple.brake * -align * Time.deltaTime);
            }

            // the most it can turn this frame in radians
            // degrees per second times degrees to radians times dt gives the turn this frame
            // align plus 1 divided by 2 scales it from full when aligned down to none when head on
            float maxRad = grapple.steerSpeed * Mathf.Deg2Rad * Time.deltaTime * (align + 1f) * 0.5f;
            Vector3 turned = Vector3.RotateTowards(dir, want, maxRad, 0f);

            // the angle actually turned in radians and the speed lost grows with it
            float turnedRad = Vector3.Angle(dir, turned) * Mathf.Deg2Rad;
            speed *= 1f - Mathf.Clamp01(turnedRad * grapple.turnLoss);

            boost = turned * speed;
        }

        // momentum decay with heavy ground friction after the wavejump window and light drag in the air
        if (hookState != HookState.Pulling && boost.sqrMagnitude > 0f)
        {
            float decay = grapple.airDrag;
            if (grounded)
            {
                decay = 0f;
                if (groundedTime > grapple.wavejumpWindow)
                {
                    decay = grapple.groundFriction;
                }
            }

            // move towards moves the momentum toward zero by at most decay times dt each frame
            boost = Vector3.MoveTowards(boost, Vector3.zero, decay * Time.deltaTime);
        }

        // the speed a dash added bleeds back off so you settle back to the momentum you had before it
        if (dashBonusLeft > 0f)
        {
            if (boost.sqrMagnitude > 0f)
            {
                // bleed the smallest of what's left and what this frame allows and what momentum exists
                float bleed = Mathf.Min(dashBonusLeft, movement.dashBonusDecay * Time.deltaTime, boost.magnitude);
                boost -= boost.normalized * bleed;
                dashBonusLeft -= bleed;
            }
            else
            {
                dashBonusLeft = 0f;
            }
        }

        // gravity and move
        Vector3 velocity;
        if (hookState == HookState.Pulling)
        {
            wallPush = Vector3.zero;
            wallJumpFaceTimer = 0f;

            // fully overrides normal movement with a fast pull toward the hit that tapers off
            // t goes from 0 to 1 over the pull and the speed slides from full to the end fraction of full
            float t = 1f - Mathf.Clamp01(hookTimer / grapple.pullDuration);
            float speed = Mathf.Lerp(grapple.pullSpeed, grapple.pullSpeed * grapple.endSpeedFraction, t);
            velocity = hookDir * speed;
        }
        else
        {
            if (hookState == HookState.Casting)
            {
                // gravity stalled while the hook is being cast
                verticalVelocity = 0f;
                horizontal *= grapple.castMoveMultiplier;
            }
            else if (wallSliding)
            {
                // sliding down a wall means weaker gravity and anything faster than the slide speed is braked down to it
                // gravity is negative so adding it times dt pulls the vertical speed downward
                verticalVelocity += movement.gravity * movement.wallJump.slideGravityMultiplier * Time.deltaTime;
                float slideMax = -movement.wallJump.slideMaxFallSpeed;
                if (verticalVelocity < slideMax)
                {
                    verticalVelocity = Mathf.MoveTowards(verticalVelocity, slideMax, movement.wallJump.slideBrake * Time.deltaTime);
                }
            }
            else
            {
                verticalVelocity += movement.gravity * Time.deltaTime;
            }

            velocity = horizontal + Vector3.up * verticalVelocity;
        }

        // velocity is distance per second so times dt is the distance to move this frame
        charController.Move(velocity * Time.deltaTime);

        // animator
        animator.SetBool(groundedParam, grounded);
        animator.SetFloat(moveXParam, MoveInput.x, movement.dampTime, Time.deltaTime);
        animator.SetFloat(moveYParam, MoveInput.y, movement.dampTime, Time.deltaTime);
    }

    // swing by holding right click on a swingable surface

    // returns true while swinging and then this has already moved the player this frame
    private bool updateSwing(Quaternion camYaw, bool grounded)
    {
        if (swingAction == null)
        {
            return false;
        }

        swingCooldownTimer -= Time.deltaTime;

        if (!isSwinging)
        {
            if (swingAction.WasPressedThisFrame() && hookState != HookState.Casting && !grounded && swingCooldownTimer <= 0f
                && swingEnergy >= swing.minEnergyToStart && tryFindSwingPoint(out Vector3 point))
            {
                startSwing(point);
            }

            if (!isSwinging)
            {
                return false;
            }
        }
        else if (!swingAction.IsPressed() || (grappleAction != null && grappleAction.WasPressedThisFrame()))
        {
            // let go or press left click to fire the grapple out of the swing
            endSwing();
            return false;
        }

        // landing ends the swing
        if (grounded)
        {
            endSwing(true);
            return false;
        }

        swingTime += Time.deltaTime;

        // holding the swing drains it and if you run out you let go
        setSwingEnergy(swingEnergy - swing.drainPerSecond * Time.deltaTime);
        if (swingEnergy <= 0f)
        {
            endSwing();
            return false;
        }

        float dt = Time.deltaTime;
        Vector3 pos = charController.bounds.center;

        // step 1 is the catch
        // if you arrive at the rope's length moving away from the anchor like flying in sideways after a boost
        // turn that motion round onto the swing arc and keep your speed instead of deleting it
        // this only reshapes the speed you arrived with because it's done before gravity and input are added
        // so it can't create energy out of nothing
        Vector3 fromAnchor = pos - swingAnchor;
        float dist = fromAnchor.magnitude;
        if (dist > 0.001f)
        {
            // the direction pointing from the anchor out to you
            Vector3 outward = fromAnchor / dist;

            // the dot product with that direction is how much of your velocity points away from the anchor
            // above 0 means moving away from the anchor
            float radial = Vector3.Dot(swingVelocity, outward);
            if (radial > 0f)
            {
                // tension is 0 when the rope is slack and 1 when it's taut
                // it ramps up over the soft zone just before the rope's full length
                float tension = Mathf.InverseLerp(swingRopeLength - swing.softZone, swingRopeLength, dist);

                // how much of the turn to apply this frame and it's the tension times the frame rate independent catch rate
                float bendAmount = tension * (1f - Mathf.Exp(-swing.bendRate * dt));

                // take away the outward part of the velocity and what's left is the part along the arc
                Vector3 tangent = swingVelocity - outward * radial;
                float tangentSpeed = tangent.magnitude;

                if (tangentSpeed > 0.01f)
                {
                    // speed retention picks how much of the full speed the arc keeps
                    // 0 keeps only the arc part and 1 keeps all of the arriving speed
                    float newSpeed = Mathf.Lerp(tangentSpeed, swingVelocity.magnitude, swing.speedRetention);

                    // slerp turns the velocity direction gradually toward the arc direction
                    swingVelocity = Vector3.Slerp(swingVelocity, tangent / tangentSpeed * newSpeed, bendAmount);
                }
                else
                {
                    // heading straight out so the rope just slows you
                    swingVelocity -= outward * (radial * bendAmount);
                }
            }
        }

        // step 2 is the forces which are gravity and your steering that pumps the swing and a bit of air drag
        swingVelocity.y += movement.gravity * swing.gravityMultiplier * dt;
        Vector3 input = camYaw * new Vector3(MoveInput.x, 0f, MoveInput.y);
        swingVelocity += input * (swing.inputAcceleration * dt);

        // drag removes a fraction of the speed each second so multiply by 1 minus drag times dt
        // the max with 0 stops a big dt from flipping the velocity backwards
        swingVelocity *= Mathf.Max(0f, 1f - swing.drag * dt);
        swingVelocity = Vector3.ClampMagnitude(swingVelocity, swing.maxSpeed);

        // step 3 is to move and if that would take you past the rope's length the rope pulls you back onto the circle
        // your velocity becomes the movement that actually happened
        // so the rope cancels gravity's outward pull without ever adding speed
        Vector3 next = pos + swingVelocity * dt;
        Vector3 nextFromAnchor = next - swingAnchor;
        float nextDist = nextFromAnchor.magnitude;
        if (nextDist > swingRopeLength)
        {
            // put the point back on the circle by keeping its direction from the anchor and using the rope length as the distance
            next = swingAnchor + nextFromAnchor / nextDist * swingRopeLength;

            // distance moved divided by time is the real velocity
            swingVelocity = (next - pos) / dt;
        }

        charController.Move(next - pos);

        // ran into something so keep only the speed that really happened
        if (charController.collisionFlags != CollisionFlags.None && dt > 0f)
        {
            swingVelocity = (charController.bounds.center - pos) / dt;
        }

        verticalVelocity = swingVelocity.y;
        updateSwingPose(camYaw, charController.bounds.center);
        realigning = false;
        return true;
    }

    // wall slide and wall jump

    // looks for a wall within reach in 8 directions around you
    // and gives back the nearest one's horizontal normal and contact point
    private bool tryFindWall(float checkDistance, out Vector3 normal, out Vector3 point)
    {
        normal = Vector3.zero;
        point = Vector3.zero;
        Vector3 origin = charController.bounds.center;
        float reach = charController.radius + checkDistance;
        float best = float.MaxValue;

        for (int i = 0; i < 8; i++)
        {
            // 8 directions around a circle are 45 degrees apart
            Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
            int count = Physics.RaycastNonAlloc(origin, dir, castBuffer, reach, movement.wallJump.wallMask, QueryTriggerInteraction.Ignore);
            for (int h = 0; h < count; h++)
            {
                RaycastHit hit = castBuffer[h];

                // not ourselves
                if (hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                // a perfectly vertical wall has a normal with no up or down part
                // so floors and ceilings and slopes with a big up or down part aren't walls
                if (Mathf.Abs(hit.normal.y) > movement.wallJump.maxNormalY)
                {
                    continue;
                }

                if (hit.distance < best)
                {
                    best = hit.distance;

                    // flatten the normal so it only points sideways
                    normal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
                    point = hit.point;
                }
            }
        }
        return best < float.MaxValue;
    }

    // wall slide happens in the air while falling with a wall right against you
    // gravity is weaker and that's applied where gravity is
    // the wall slide animator int says which side the wall is on so the matching animation plays
    private void updateWallSlide(Quaternion camYaw, bool grounded)
    {
        bool canSlide = !grounded && !isDashing && hookState == HookState.None && verticalVelocity < 0f;
        if (!canSlide)
        {
            endWallSlide();
            return;
        }

        if (tryFindWall(movement.wallJump.slideCheckDistance, out Vector3 n, out Vector3 p))
        {
            wallSlideNormal = n;
            wallSlidePoint = p;
            wallSlideGrace = movement.wallJump.slideGraceTime;
        }
        else
        {
            wallSlideGrace -= Time.deltaTime;
        }

        if (wallSlideGrace <= 0f)
        {
            endWallSlide();
            return;
        }

        if (!wallSliding)
        {
            wallSliding = true;
            if (onWallSlideStart != null)
            {
                onWallSlideStart.raise(wallSlidePoint, wallSlideNormal);
            }
        }

        // which side is the wall on relative to the way you're facing where 0 is ahead and 90 is on your right
        // the inverse rotation turns the world direction into the camera's own frame
        // the wall is the opposite of its normal because the normal points away from the wall
        Vector3 toWall = Quaternion.Inverse(camYaw) * -wallSlideNormal;

        // atan2 of sideways over forward gives the angle in radians and times rad to deg makes it degrees
        float angle = Mathf.Atan2(toWall.x, toWall.z) * Mathf.Rad2Deg;
        float abs = Mathf.Abs(angle);
        int anim;
        if (abs <= movement.wallJump.forwardHalfAngle)
        {
            // wall ahead
            anim = 1;
        }
        else if (abs >= 180f - movement.wallJump.backHalfAngle)
        {
            // wall behind and no animation for it
            anim = 0;
        }
        else if (angle < 0f)
        {
            // left
            anim = 2;
        }
        else
        {
            // right
            anim = 3;
        }
        setWallSlideAnim(anim);
    }

    private void endWallSlide()
    {
        if (wallSliding)
        {
            wallSliding = false;
            if (onWallSlideEnd != null)
            {
                onWallSlideEnd.raise();
            }
        }
        wallSlideGrace = 0f;
        setWallSlideAnim(0);
    }

    private void setWallSlideAnim(int value)
    {
        if (!hasWallSlideParam || value == wallAnimValue)
        {
            return;
        }

        wallAnimValue = value;
        animator.SetInteger(wallSlideParam, value);
    }

    // wall jump is modelled on deadlock's
    // it gives a fixed upward kick and the momentum you already had along the wall and a push off it
    // the direction comes from what you hold with more push when you hold away from the wall and less into it
    // but you always leave it
    private bool tryWallJump(Quaternion camYaw)
    {
        if (!tryFindWall(movement.wallJump.checkDistance, out Vector3 wallNormal, out Vector3 wallPoint))
        {
            return false;
        }
        jumpBufferTimer = 0f;

        // jumping out of a grapple pull ends it first
        // that stores the pull's speed as momentum and leaves vertical velocity as the upward speed the boost had
        if (hookState == HookState.Pulling)
        {
            endHook(false);
        }

        Vector3 input = camYaw * new Vector3(MoveInput.x, 0f, MoveInput.y);
        bool hasInput = input.sqrMagnitude >= 0.01f;
        if (hasInput)
        {
            input.Normalize();
        }

        // the dot product with the wall normal is negative 1 holding into the wall and 1 holding straight away
        // adding 1 and halving turns that into a 0 to 1 number
        float away01 = 0.5f;
        if (hasInput)
        {
            away01 = (Vector3.Dot(input, wallNormal) + 1f) * 0.5f;
        }

        // fatigue lowers the jump height but not the push
        // it recovers over the fatigue time and landing clears it
        // recovered goes from 0 right after a wall jump to 1 when fully rested
        float recovered = Mathf.Clamp01((Time.time - lastWallJumpTime) / Mathf.Max(movement.wallJump.fatigueTime, 0.0001f));
        float heightMult = Mathf.Lerp(movement.wallJump.fatigueMultiplier, 1f, recovered);
        lastWallJumpTime = Time.time;

        // keep the momentum you had along the wall because the part driving you into it is lost
        // add a little sideways from your input and push off the wall
        // the push isn't part of your boost so braking and steering can't touch it
        // project on plane keeps only the part of a vector that lies flat along the wall
        Vector3 along = Vector3.ProjectOnPlane(boost, wallNormal);
        Vector3 sideways = Vector3.zero;
        if (hasInput)
        {
            sideways = Vector3.ProjectOnPlane(input, wallNormal) * movement.wallJump.sidewaysSpeed;
        }

        // the push blends from the min push when holding into the wall to the max push when holding away
        float push = Mathf.Lerp(movement.wallJump.minPush, movement.wallJump.maxPush, away01);

        // if you're already leaving the wall faster than that keep that speed
        // the dot product with the normal is the speed pointing away from the wall
        push = Mathf.Max(push, Vector3.Dot(boost, wallNormal));

        boost = along;
        wallPush = Vector3.ClampMagnitude(sideways + wallNormal * push, movement.wallJump.maxSpeed);

        // vertical is a fixed kick plus part of any upward speed a grapple boost left you with
        // that's a wavebounce and it's how you go up a wall and fatigue scales both
        // same rules as a wavejump so the boost has to count and what it adds has to beat the min wavejump speed
        // it's the amount actually carried that's checked and not just the speed you arrived with
        // so a fatigued or weak wall jump gets neither the bonus nor the effect
        float arrivedUp = Mathf.Max(0f, verticalVelocity);
        float carried = arrivedUp * movement.wallJump.bounceCarry * heightMult;
        bool bounce = grapple.keepMomentum && Time.time - lastBoostUpTime <= movement.wallJump.bounceWindow && carried > grapple.minWavejumpSpeed;

        // launch speed from height is the square root of height times negative 2 times gravity
        float kick = Mathf.Sqrt(movement.wallJump.jumpHeight * heightMult * -2f * movement.gravity);
        verticalVelocity = kick;
        if (bounce)
        {
            verticalVelocity += carried;
        }

        if (isDashing)
        {
            isDashing = false;
            realigning = true;
        }
        dashBonusLeft = 0f;

        // face the wall's normal for a moment like the air dash faces its direction
        wallJumpFaceDir = wallNormal;
        wallJumpFaceTimer = movement.wallJump.jumpFaceTime;

        endWallSlide();
        animator.ResetTrigger(jumpTrigger);
        animator.CrossFadeInFixedTime(wallJumpState, movement.wallJump.animCrossfade, 0);
        if (onWallJump != null)
        {
            onWallJump.raise(wallPoint, wallNormal);
        }

        // the wavejump particles and trail and camera kick turned to the wall
        if (bounce && onWallBounce != null)
        {
            onWallBounce.raise(wallPoint, wallNormal);
        }
        return true;
    }

    // hangs the body along the rope with the head toward the anchor
    // keeps it facing the way it started and leans back more the faster you go
    private void updateSwingPose(Quaternion camYaw, Vector3 pos)
    {
        // the body's up direction is toward the anchor
        Vector3 up = swingAnchor - pos;
        if (up.sqrMagnitude < 0.0001f)
        {
            transform.rotation = camYaw;
            return;
        }
        up.Normalize();

        // the facing direction flattened so it's at right angles to the rope
        Vector3 forward = Vector3.ProjectOnPlane(swingFacing, up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(transform.forward, up);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // lean amount goes from 0 at a standstill to 1 at the full lean speed
        float speed01 = Mathf.Clamp01(swingVelocity.magnitude / Mathf.Max(swing.leanFullSpeed, 0.01f));

        // multiplying rotations stacks them so the lean tilt and the yaw offset happen in the body's own frame
        // a negative tilt around the right axis tips the head back
        Quaternion target = Quaternion.LookRotation(forward, up)
                          * Quaternion.Euler(-swing.leanBackAngle * speed01, 0f, 0f)
                          * Quaternion.Euler(0f, swing.facingYawOffset, 0f);

        // eases into the pose over a moment
        // looking around doesn't turn the body and it turns back toward the camera once the swing is over
        // blend moves toward 1 by dt divided by the blend time so it takes that long to finish
        swingPoseBlend = Mathf.MoveTowards(swingPoseBlend, 1f, Time.deltaTime / Mathf.Max(swing.poseBlendTime, 0.0001f));
        transform.rotation = Quaternion.Slerp(swingPoseFrom, target, swingPoseBlend);
    }

    // what a pull would hand over if it ended right now
    // that's the carried horizontal momentum and the vertical speed you arc off with
    private void getHookHandoff(out Vector3 momentum, out float vertical)
    {
        // t goes from 0 to 1 over the pull and the speed slides from full to the end fraction of full
        float t = 1f - Mathf.Clamp01(hookTimer / grapple.pullDuration);

        // same speed curve the pull uses
        float speed = Mathf.Lerp(grapple.pullSpeed, grapple.pullSpeed * grapple.endSpeedFraction, t);
        Vector3 flat = new Vector3(hookDir.x, 0f, hookDir.z) * speed;

        momentum = Vector3.zero;
        if (grapple.keepMomentum)
        {
            momentum = Vector3.ClampMagnitude(flat * grapple.momentumScale, grapple.maxMomentum);
        }
        vertical = hookDir.y * grapple.pullSpeed * grapple.endSpeedFraction;
    }

    private void startSwing(Vector3 point)
    {
        // keep whatever speed you already have like a boost or a fall or a run
        Vector3 startVelocity = charController.velocity;
        if (hookState == HookState.Pulling)
        {
            // cancelling a pull into a swing
            // the pull itself is far faster than the momentum it normally leaves behind
            // so carry what the pull would have handed over plus your run and not the raw pull speed
            getHookHandoff(out Vector3 handoff, out float handoffVertical);
            Vector3 run = Vector3.ClampMagnitude(Quaternion.Euler(0f, yaw, 0f) * new Vector3(MoveInput.x, 0f, MoveInput.y), 1f) * movement.moveSpeed;
            startVelocity = handoff + run + Vector3.up * handoffVertical;
        }
        else if (isDashing)
        {
            // cancelling a dash into a swing is the same idea
            // carry the momentum the dash leaves behind plus your run and not the raw dash burst
            Vector3 run = Vector3.ClampMagnitude(Quaternion.Euler(0f, yaw, 0f) * new Vector3(MoveInput.x, 0f, MoveInput.y), 1f) * movement.moveSpeed;
            startVelocity = boost + run + Vector3.up * charController.velocity.y;
        }

        // already part of the controller velocity if you had one
        wallPush = Vector3.zero;
        wallJumpFaceTimer = 0f;

        // cancel any grapple in progress
        if (hookState != HookState.None)
        {
            endHook(false);
        }

        swingAnchor = point;
        swingVelocity = startVelocity;

        // the rope is as long as the distance to the anchor but kept between the min length and the range
        swingRopeLength = Mathf.Clamp(Vector3.Distance(charController.bounds.center, point), swing.minRopeLength, swing.range);
        isSwinging = true;
        swingTime = 0f;
        swingPoseBlend = 0f;
        swingPoseFrom = transform.rotation;
        swingFacing = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        setSwingEnergy(swingEnergy - swing.startCost);
        isDashing = false;

        // it's all in the swing velocity now
        boost = Vector3.zero;

        if (hasSwingEndTrigger)
        {
            animator.ResetTrigger(swingEndTrigger);
        }

        if (hasSwingInterruptTrigger)
        {
            animator.ResetTrigger(swingInterruptTrigger);
        }

        if (hasSwingStartTrigger)
        {
            animator.SetTrigger(swingStartTrigger);
        }

        if (onSwingStart != null)
        {
            onSwingStart.raise();
        }
    }

    private void endSwing(bool cutShort = false)
    {
        isSwinging = false;

        // ease the tilted body back upright toward the camera
        realigning = true;
        swingCooldownTimer = swing.cooldown;

        // the fling bonus is earned by actually swinging for a while
        // so tapping the button over and over can't farm speed
        // fling amount goes from 0 to 1 over the ramp time and blends between no bonus and the full release multiplier
        float flingAmount = Mathf.Clamp01(swingTime / Mathf.Max(swing.releaseBonusRampTime, 0.0001f));
        Vector3 v = swingVelocity * Mathf.Lerp(1f, swing.releaseSpeedMultiplier, flingAmount);
        verticalVelocity = v.y;

        // hand your speed back to the normal movement as momentum
        // normal movement adds your run input on top of boost every frame
        // and the speed you swung with already includes it so take it back out here
        // without this every swing would add a free burst of run speed
        // and chaining short swings would pile it up
        Vector3 runInput = Vector3.ClampMagnitude(Quaternion.Euler(0f, yaw, 0f) * new Vector3(MoveInput.x, 0f, MoveInput.y), 1f) * movement.moveSpeed;
        Vector3 momentum = new Vector3(v.x, 0f, v.z) - runInput;
        if (grapple.keepMomentum)
        {
            boost = Vector3.ClampMagnitude(momentum, swing.maxReleaseSpeed);
        }
        else
        {
            boost = Vector3.zero;
        }

        // the end flip animation looks wrong if you land mid swing or only swung for a moment so those fire an interrupt instead
        bool interrupt = cutShort || swingTime < swing.minFlipTime;
        if (hasSwingStartTrigger)
        {
            animator.ResetTrigger(swingStartTrigger);
        }

        if (interrupt)
        {
            if (hasSwingEndTrigger)
            {
                animator.ResetTrigger(swingEndTrigger);
            }

            if (hasSwingInterruptTrigger)
            {
                animator.SetTrigger(swingInterruptTrigger);
            }
        }
        else
        {
            if (hasSwingInterruptTrigger)
            {
                animator.ResetTrigger(swingInterruptTrigger);
            }

            if (hasSwingEndTrigger)
            {
                animator.SetTrigger(swingEndTrigger);
            }
        }

        if (onSwingEnd != null)
        {
            onSwingEnd.raise();
        }
    }

    // ray from the crosshair and the first thing it hits while ignoring yourself must carry the swing tag
    private bool tryFindSwingPoint(out Vector3 point)
    {
        point = default;

        Ray ray;
        Camera cam = getAimCamera();
        if (cam != null)
        {
            ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }
        else
        {
            ray = new Ray(transform.position + Vector3.up, transform.forward);
        }

        // the ray starts at the camera which is behind you so look a bit further than the range
        // then measure the range from the player's body because that's where the rope is attached
        Vector3 body = charController.bounds.center;
        float reach = swing.range + Vector3.Distance(ray.origin, body);
        int hitCount = castSphere(ray, swing.aimRadius, reach);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit h = castBuffer[i];

            // ignore self
            if (h.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            // something else is in the way
            if (!h.collider.CompareTag(swing.swingTag))
            {
                return false;
            }

            Vector3 hitPoint = safeHitPoint(h, ray.origin);

            // too far from you
            if (Vector3.Distance(body, hitPoint) > swing.range)
            {
                return false;
            }
            point = hitPoint;
            return true;
        }
        return false;
    }

    // resources and aim helpers

    private void refillResources(float dt)
    {
        if (swingEnergy < 1f)
        {
            setSwingEnergy(swingEnergy + swing.regenPerSecond * dt);
        }

        if (grappleCharges < grapple.airUses)
        {
            setGrappleCharges(grapple.airUses);
        }
    }

    private void setSwingEnergy(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(value, swingEnergy))
        {
            return;
        }

        swingEnergy = value;
        if (onSwingEnergyChanged != null)
        {
            onSwingEnergyChanged.raise(swingEnergy);
        }
    }

    private void setGrappleCharges(int value)
    {
        value = Mathf.Clamp(value, 0, grapple.airUses);
        if (value == grappleCharges)
        {
            return;
        }

        grappleCharges = value;
        if (onGrappleChargesChanged != null)
        {
            onGrappleChargesChanged.raise(grappleCharges);
        }
    }

    // keeps the aim marker up to date
    // where would a swing attach if you pressed the button now and it's only shown in the air
    private void updateSwingPreview(bool grounded)
    {
        swingPreviewValid = false;

        // can't swing from the ground so don't show a point
        if (swingAction == null || isSwinging || grounded)
        {
            return;
        }

        if (tryFindSwingPoint(out Vector3 point))
        {
            swingPreviewValid = true;
            swingPreviewPoint = point;
        }
    }

    // tells the ui whether the crosshair is on something you could grapple or swing from right now
    private void updateAimTarget()
    {
        bool valid = swingPreviewValid || hasHookableTarget();
        if (valid == aimTargetValid)
        {
            return;
        }

        aimTargetValid = valid;
        if (onAimTargetChanged != null)
        {
            onAimTargetChanged.raise(valid);
        }
    }

    // is there a hookable surface under the crosshair and in range
    // same ball sweep as a real cast but it doesn't touch the cast state
    private bool hasHookableTarget()
    {
        Camera cam = getAimCamera();
        if (cam == null)
        {
            return false;
        }
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        int hitCount = castSphere(ray, grapple.aimRadius, grapple.range);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit h = castBuffer[i];

            // ignore self
            if (h.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            // the first thing in the way decides like a real cast
            return h.collider.CompareTag(grapple.hookableTag);
        }
        return false;
    }

    // the aim camera if one is set and otherwise the main camera
    private Camera getAimCamera()
    {
        if (aimCamera != null)
        {
            return aimCamera;
        }
        return Camera.main;
    }

    // sweeps a ball along the ray instead of a thin line so you don't have to aim pixel perfectly
    // fills the cast buffer nearest first and returns how many hits there are
    private int castSphere(Ray ray, float radius, float distance)
    {
        int n = Physics.SphereCastNonAlloc(ray, Mathf.Max(0.01f, radius), castBuffer, distance, grapple.hookMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(castBuffer, 0, n, hitComparer);
        return n;
    }

    // a ball that already overlaps something at its start reports no usable point
    // so work out the point some other way
    private Vector3 safeHitPoint(RaycastHit h, Vector3 rayOrigin)
    {
        if (h.distance > 0f || h.point != Vector3.zero)
        {
            return h.point;
        }

        // closest point only works on some collider types so check first
        if (canUseClosestPoint(h.collider))
        {
            return h.collider.ClosestPoint(rayOrigin);
        }

        // for any other collider shoot a ray from the start toward its middle and use where it lands
        // the distance is long enough to cross the whole collider
        Vector3 toCenter = h.collider.bounds.center - rayOrigin;
        Ray ray = new Ray(rayOrigin, toCenter);
        if (h.collider.Raycast(ray, out RaycastHit surface, toCenter.magnitude + h.collider.bounds.size.magnitude))
        {
            return surface.point;
        }

        // last resort is the nearest point on the collider's bounding box
        return h.collider.bounds.ClosestPoint(rayOrigin);
    }

    // unity only allows closest point on boxes and spheres and capsules and convex mesh colliders
    private bool canUseClosestPoint(Collider c)
    {
        if (c is BoxCollider || c is SphereCollider || c is CapsuleCollider)
        {
            return true;
        }

        MeshCollider mesh = c as MeshCollider;
        return mesh != null && mesh.convex;
    }

    // this is named handle landed because the on landed name is already the event channel above
    private void handleLanded(float fallSpeed)
    {
        // ignore tiny drops and slope steps
        if (fallSpeed < movement.minLandSpeed)
        {
            return;
        }

        // harder landings squash deeper so scale the additive layer's weight by impact
        // impact goes from 0 at the min land speed to 1 at the hard land speed
        // and the weight blends from the min land weight up to full
        if (landLayerIndex >= 0)
        {
            float impact = Mathf.InverseLerp(movement.minLandSpeed, movement.hardLandSpeed, fallSpeed);
            animator.SetLayerWeight(landLayerIndex, Mathf.Lerp(movement.minLandWeight, 1f, impact));
        }

        if (hasLandTrigger)
        {
            animator.SetTrigger(landTrigger);
        }

        if (Landed != null)
        {
            Landed.Invoke(fallSpeed);
        }

        if (onLanded != null)
        {
            onLanded.raise(fallSpeed);
        }
    }

    private bool hasParameter(string paramName, AnimatorControllerParameterType type)
    {
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName && p.type == type)
            {
                return true;
            }
        }
        return false;
    }

    // grapple state machine goes from none to casting to pulling and back to none
    // casting is a short delay with gravity stalled
    // the ray is fired the instant you click so the target is whatever was under the crosshair then
    private void updateHook()
    {
        hookCooldownTimer -= Time.deltaTime;

        switch (hookState)
        {
            case HookState.None:
                if (grappleAction != null && grappleAction.WasPressedThisFrame()
                    && !isDashing && hookCooldownTimer <= 0f && grappleCharges > 0)
                {
                    hookHit = tryFindHook(out hookPoint);
                    hookState = HookState.Casting;
                    hookTimer = grapple.castTime;

                    // kill any rise or fall momentum when the cast starts
                    verticalVelocity = 0f;
                    animator.SetTrigger(hookCastTrigger);
                    if (onGrappleFired != null)
                    {
                        onGrappleFired.raise();
                    }
                }
                break;

            case HookState.Casting:
                hookTimer -= Time.deltaTime;
                if (hookTimer <= 0f)
                {
                    if (hookHit)
                    {
                        // aim from where the player is now because they may have drifted during the cast
                        hookDir = (hookPoint - charController.bounds.center).normalized;
                        hookState = HookState.Pulling;
                        hookTimer = grapple.pullDuration;

                        // the boost is what costs a use and a miss is free
                        setGrappleCharges(grappleCharges - 1);
                        if (grapple.refreshesAirDash)
                        {
                            airDashUsed = false;
                        }
                        animator.SetTrigger(hookPullTrigger);
                        if (onGrappleHit != null)
                        {
                            onGrappleHit.raise();
                        }
                    }
                    else
                    {
                        if (onGrappleMiss != null)
                        {
                            onGrappleMiss.raise();
                        }

                        // missed so just resume normal gravity
                        endHook();
                    }
                }
                break;

            case HookState.Pulling:
                hookTimer -= Time.deltaTime;
                float dist = Vector3.Distance(charController.bounds.center, hookPoint);
                if (hookTimer <= 0f || dist <= grapple.stopDistance)
                {
                    endHook();
                }
                break;
        }
    }

    // land if grounded matters when the pull ends while you're still standing on something like grappling at the floor
    // you never leave the ground so the normal airborne to grounded landing never fires
    // in that case treat the end of the pull as the landing
    // pass false when you're about to jump out of the pull
    private void endHook(bool landIfGrounded = true)
    {
        bool wasPulling = hookState == HookState.Pulling;

        // keep the vertical part of the pull so you arc off the end instead of stopping dead
        // and optionally keep the horizontal part as carried momentum
        if (wasPulling)
        {
            getHookHandoff(out Vector3 handoff, out float handoffVertical);
            verticalVelocity = handoffVertical;

            // upward boost means a wall jump soon after can carry part of it
            if (handoffVertical > 0f)
            {
                lastBoostUpTime = Time.time;
            }

            if (grapple.keepMomentum)
            {
                boost = handoff;
            }
        }

        hookState = HookState.None;
        hookCooldownTimer = grapple.cooldown;
        animator.ResetTrigger(hookCastTrigger);
        animator.ResetTrigger(hookPullTrigger);

        if (wasPulling && landIfGrounded && charController.isGrounded)
        {
            // always at least a soft landing
            handleLanded(Mathf.Max(-verticalVelocity, movement.minLandSpeed));
        }

        if (onGrappleEnded != null)
        {
            onGrappleEnded.raise();
        }
    }

    // ray from the center of the screen which is the crosshair
    // it skips the player's own colliders and then the first thing hit decides it
    // tagged hookable is a hit and anything else is blocked or a miss
    private bool tryFindHook(out Vector3 point)
    {
        point = default;

        Ray ray;
        Camera cam = getAimCamera();
        if (cam != null)
        {
            ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }
        else if (cameraTarget != null)
        {
            ray = new Ray(cameraTarget.position, cameraTarget.forward);
        }
        else
        {
            ray = new Ray(transform.position + Vector3.up, transform.forward);
        }

        lastRayOrigin = ray.origin;

        // remembered so a missed cast can still turn you toward where you aimed
        hookAimDir = ray.direction;

        // the end of the ray is the start plus the direction times the range
        lastRayEnd = ray.origin + ray.direction * grapple.range;

        // overwritten below if something is hit
        lastHitNormal = -ray.direction;

        // a ball swept along the aim line rather than a thin ray so near misses still count
        int hitCount = castSphere(ray, grapple.aimRadius, grapple.range);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit h = castBuffer[i];

            // ignore self
            if (h.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            Vector3 hitPoint = safeHitPoint(h, ray.origin);
            lastHitPoint = hitPoint;

            // the drawn ray stops where it hit
            lastRayEnd = hitPoint;
            lastHitNormal = h.normal;

            if (h.collider.CompareTag(grapple.hookableTag))
            {
                lastResult = CastResult.Hookable;
                if (logHookResults)
                {
                    Debug.Log($"[Grapple] HIT!! hookable: '{h.collider.name}' at {h.distance:F1}m", h.collider.gameObject);
                }
                point = hitPoint;
                return true;
            }

            // something non hookable is in the way
            lastResult = CastResult.Blocked;
            if (logHookResults)
            {
                Debug.Log($"[Grapple] BLOCKED!!! by non-hookable: '{h.collider.name}' (tag: {h.collider.tag}) at {h.distance:F1}m", h.collider.gameObject);
            }
            return false;
        }

        lastResult = CastResult.Miss;
        if (logHookResults)
        {
            Debug.Log("[Grapple] MISSED!! nothing within range...");
        }
        return false;
    }

    // scene view and the game view with gizmos toggled on
    // green is a hookable hit and yellow is blocked by a non hookable and red is nothing hit
    // the white wire sphere around the hit is where the pull stops early
    // the cyan line is the active pull
    private void OnDrawGizmos()
    {
        if (!drawHookGizmos || lastResult == CastResult.None)
        {
            return;
        }

        if (lastResult == CastResult.Hookable)
        {
            Gizmos.color = Color.green;
        }
        else if (lastResult == CastResult.Blocked)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        Gizmos.DrawLine(lastRayOrigin, lastRayEnd);

        if (lastResult != CastResult.Miss)
        {
            Gizmos.DrawWireSphere(lastHitPoint, 0.25f);

            if (lastResult == CastResult.Hookable)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(lastHitPoint, grapple.stopDistance);
            }
        }

        if (hookState == HookState.Pulling && charController != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(charController.bounds.center, hookPoint);
        }
    }

    // world space rotation so the camera ignores the body turning during a dash
    private void applyCameraRotation()
    {
        if (cameraTarget != null)
        {
            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
