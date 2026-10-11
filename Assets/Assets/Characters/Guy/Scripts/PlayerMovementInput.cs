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

    [Header("Animator Parameters")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string dashTrigger = "AirDash";
    [SerializeField] private string groundedParam = "Grounded";
    [SerializeField] private float dampTime = 0.1f;

    [Header("Camera Look")]
    [SerializeField] private Transform cameraTarget; 
    [SerializeField] private float mouseSensitivity = 0.1f; // degrees per mouse count
    [SerializeField] private float gamepadSensitivity = 150f; // degrees per second
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float moveThreshold = 0.5f; // min speed for running dust

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Air Dash")]
    [SerializeField] private float dashSpeed = 18f; // starting speed, tapers to 0
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float dashUpVelocity = 3f; // small upward pop
    [SerializeField] private float dashTurnSpeed = 1080f; // deg/sec, used turning into AND out of the dash

    [Header("Dash Ring Particles")]
    [SerializeField] private GameObject dashRingPrefab;
    [SerializeField] private Transform dashRingPoint; // spawn position only

    [Header("Dust")]
    public Transform feet;
    public GameObject dustPrefab;
    public GameObject dustJumpPrefab;
    [SerializeField] private float dustInterval = 0.25f;

    public Vector2 MoveInput { get; private set; }

    private InputAction moveAction, lookAction, jumpAction, dashAction;
    private Animator animator;
    private CharacterController charController;

    private float yaw, pitch;
    private float verticalVelocity;
    private float dustTimer;

    private bool isDashing;
    private bool airDashUsed; // one dash per airtime
    private bool realigning; // turning back to face the camera after a dash
    private float dashTimer;
    private Vector3 dashDir; // world direction, locked at dash start

    private void Awake()
    {
        animator = GetComponent<Animator>();
        charController = GetComponent<CharacterController>();

        if (inputActions == null)
        {
            Debug.LogError($"{name}: No InputActionAsset assigned.");
            return;
        }

        var map = inputActions.FindActionMap(actionMapName, throwIfNotFound: false);

        moveAction = map.FindAction(moveActionName, false);
        lookAction = map.FindAction(lookActionName, false);
        jumpAction = map.FindAction(jumpActionName, false);
        dashAction = map.FindAction(dashActionName, false);



        yaw = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        jumpAction?.Enable();
        dashAction?.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        dashAction?.Disable();
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        ApplyCameraRotation(); // after body rotation, so the camera doesn't jerk
    }

    // Only updates yaw/pitch numbers; rotation is applied later.
    private void HandleLook()
    {
        if (lookAction == null) return;

        Vector2 look = lookAction.ReadValue<Vector2>();

        // Mouse delta is already per-frame; stick needs deltaTime.
        bool isGamepad = lookAction.activeControl?.device is Gamepad;
        float scale = isGamepad ? gamepadSensitivity * Time.deltaTime : mouseSensitivity;

        yaw += look.x * scale;
        pitch -= look.y * scale; // use += for inverted Y
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void HandleMovement()
    {
        MoveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        // Camera's left/right angle only; makes input camera-relative
        Quaternion camYaw = Quaternion.Euler(0f, yaw, 0f);

        // Ground check 
        bool grounded = charController.isGrounded;
        if (grounded)
        {
            if (isDashing) realigning = true; // landed mid-dash: turn back smoothly
            airDashUsed = false;
            isDashing = false;
            animator.ResetTrigger(dashTrigger); // avoid being stuck in the dash state
        }

        // Jump 
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // keeps isGrounded reliable

        if (grounded && jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            if (dustJumpPrefab != null)
                Instantiate(dustJumpPrefab, feet.position, dustJumpPrefab.transform.rotation);

            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger(jumpTrigger);
        }

        // Start air dash 
        if (!grounded && !isDashing && !airDashUsed && dashAction != null && dashAction.WasPressedThisFrame())
        {
            Vector3 input = new Vector3(MoveInput.x, 0f, MoveInput.y);
            if (input.sqrMagnitude < 0.01f) input = Vector3.forward; // shift alone = forward

            dashDir = camYaw * input.normalized;
            dashTimer = dashDuration;
            isDashing = true;
            airDashUsed = true;
            realigning = false; // a new dash cancels any realigning
            verticalVelocity = dashUpVelocity;

            animator.SetTrigger(dashTrigger);
            // If the anim is delayed after jump -> fall, try:
            // animator.CrossFadeInFixedTime("AirDash", 0.05f, 0);

            // Ring: position from the child, rotation from dashDir
            // (the body hasn't finished turning yet)
            if (dashRingPrefab != null && dashRingPoint != null)
            {
                var ring = Instantiate(dashRingPrefab, dashRingPoint.position, Quaternion.LookRotation(dashDir));
                Destroy(ring, 2f);
            }
        }

        // Horizontal velocity + body rotation 
        Vector3 horizontal;
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            // t: 0 -> 1 over the dash; speed tapers from dashSpeed to 0
            float t = 1f - Mathf.Clamp01(dashTimer / dashDuration);
            horizontal = dashDir * Mathf.Lerp(dashSpeed, 0f, t);

            // Dash anim leads with the character's right, hence the -90 offset
            Quaternion target = Quaternion.LookRotation(dashDir) * Quaternion.Euler(0f, -90f, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, dashTurnSpeed * Time.deltaTime);

            if (dashTimer <= 0f)
            {
                isDashing = false;
                realigning = true; // dash over: turn back toward camera
            }
        }
        else
        {
            horizontal = Vector3.ClampMagnitude(camYaw * new Vector3(MoveInput.x, 0f, MoveInput.y), 1f) * moveSpeed;

            if (realigning)
            {
                // Smoothly turn back to face the camera after a dash
                transform.rotation = Quaternion.RotateTowards(transform.rotation, camYaw, dashTurnSpeed * Time.deltaTime);

                if (Quaternion.Angle(transform.rotation, camYaw) < 0.5f)
                    realigning = false; // close enough, lock back on
            }
            else
            {
                transform.rotation = camYaw; // normal: face the camera direction
            }
        }

        // Gravity + move 
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = horizontal + Vector3.up * verticalVelocity;
        charController.Move(velocity * Time.deltaTime);

        // Running dust (grounded, moving, on a timer) 
        Vector3 flatVel = new Vector3(charController.velocity.x, 0f, charController.velocity.z);
        dustTimer -= Time.deltaTime;
        if (grounded && flatVel.magnitude > moveThreshold && dustTimer <= 0f && dustPrefab != null)
        {
            Instantiate(dustPrefab, feet.position, dustPrefab.transform.rotation);
            dustTimer = dustInterval;
        }

        //  Animator
        animator.SetBool(groundedParam, grounded);
        animator.SetFloat(moveXParam, MoveInput.x, dampTime, Time.deltaTime);
        animator.SetFloat(moveYParam, MoveInput.y, dampTime, Time.deltaTime);
    }

    // World-space rotation, so the camera ignores the body turning during a dash
    private void ApplyCameraRotation()
    {
        if (cameraTarget != null)
            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}