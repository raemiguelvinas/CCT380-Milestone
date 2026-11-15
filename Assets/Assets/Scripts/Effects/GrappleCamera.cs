using Unity.Cinemachine;
using UnityEngine;

// moves the camera's distance and field of view with the grapple
// cast moves the camera in a bit and tightens the fov slightly
// boost opens the fov right up
// swing slides the camera toward the middle and pulls back a little
// end eases everything back to normal
// there is also a short fov kick on a wavejump
// when a wall is on the camera's shoulder side the camera slides toward the middle
// just far enough to stay clear of it because it collapses onto you against a wall
// everything is smoothed so nothing snaps and all the numbers live in the grapple config asset
// wire game event listeners to the public methods
// grapple fired calls on cast and grapple hit calls on boost and grapple ended calls on end
// wavejump calls kick and swing start calls on swing and swing end calls on end
// put this on the cinemachine camera object
public class GrappleCamera : MonoBehaviour
{
    [SerializeField] private GrappleConfig config;
    [SerializeField] private CinemachineCamera vcam;
    [SerializeField] private CinemachineThirdPersonFollow follow;

    private enum Mode { Normal, Cast, Boost, Swing }
    private Mode mode = Mode.Normal;

    private float baseFov;
    private float baseDistance;
    private float baseSide;

    // current smoothed values
    private float distanceMultiplier = 1f;
    private float cameraSide;
    private float fovChange;
    private float kickAmount;
    private float kickTarget;
    private float kickTimer;

    // wall centring memory
    private float wallHoldTimer; // counts down after a wall stops pushing the camera in
    private float heldSide; // how far in the camera was being pushed
    private readonly RaycastHit[] wallHits = new RaycastHit[16];

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"{name}: No GrappleConfig assigned. Using defaults.", this);
            config = ScriptableObject.CreateInstance<GrappleConfig>();
        }

        if (vcam == null)
        {
            vcam = GetComponent<CinemachineCamera>();
        }

        if (follow == null)
        {
            follow = GetComponent<CinemachineThirdPersonFollow>();
        }

        // remember how the camera was set up because everything is relative to this
        baseFov = vcam.Lens.FieldOfView;
        if (follow != null)
        {
            baseDistance = follow.CameraDistance;
            baseSide = follow.CameraSide;
            cameraSide = baseSide;
        }
    }

    // call these from game event listeners
    public void onCast()
    {
        mode = Mode.Cast;
    }

    public void onBoost()
    {
        mode = Mode.Boost;
    }

    public void onSwing()
    {
        mode = Mode.Swing;
    }

    public void onEnd()
    {
        mode = Mode.Normal;
    }

    public void kick()
    {
        kickTarget = config.camera.wavejumpFovKick;
        kickTimer = config.camera.kickHoldTime;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        float targetDistance = 1f;
        float targetFov = 0f;
        float speed = config.camera.returnSmoothing;
        float targetSide = baseSide;

        switch (mode)
        {
            case Mode.Cast:
                targetDistance = config.camera.castDistanceMultiplier;
                targetFov = config.camera.castFovChange;
                speed = config.camera.castSmoothing;
                break;
            case Mode.Boost:
                targetDistance = config.camera.boostDistanceMultiplier;
                targetFov = config.camera.boostFovChange;
                speed = config.camera.boostSmoothing;
                break;
            case Mode.Swing:
                targetDistance = config.camera.swingDistanceMultiplier;
                targetSide = config.camera.swingCameraSide;
                speed = config.camera.swingSmoothing;
                break;
        }

        // slide toward the middle if a wall is on the camera's shoulder side
        // this is quick because the camera is squashed until it does
        float sideSpeed = speed;
        if (config.camera.centerNearWalls)
        {
            // the side the wall allows right now and it's the same as the target when no wall is near
            float limited = centerAwayFromWall(targetSide);

            // distance from half is how far out on a shoulder the camera is
            // the wall is pushing in when it allows less than the camera mode wants
            bool wallActive = Mathf.Abs(limited - 0.5f) < Mathf.Abs(targetSide - 0.5f) - 0.001f;

            if (wallActive)
            {
                // move in quickly and remember how far in it needs to be and restart the hold timer
                heldSide = limited;
                wallHoldTimer = config.camera.wallReleaseDelay;
                targetSide = limited;
                sideSpeed = Mathf.Max(speed, config.camera.wallCenterSmoothing);
            }
            else if (wallHoldTimer > 0f)
            {
                // the wall just stopped pushing so wait a moment before letting the camera back out
                // this stops it flicking in and out when the wall check flickers on and off
                wallHoldTimer -= dt;
                if (Mathf.Abs(heldSide - 0.5f) < Mathf.Abs(targetSide - 0.5f))
                {
                    targetSide = heldSide;
                }
            }
        }

        // frame rate independent smoothing
        // 1 minus e to the power of negative speed times dt is the fraction of the gap to close this frame
        // it gives the same feel at any frame rate unlike a plain lerp with a fixed amount
        float t = 1f - Mathf.Exp(-speed * dt);
        distanceMultiplier = Mathf.Lerp(distanceMultiplier, targetDistance, t);
        fovChange = Mathf.Lerp(fovChange, targetFov, t);
        cameraSide = Mathf.Lerp(cameraSide, targetSide, 1f - Mathf.Exp(-sideSpeed * dt));

        // the kick pops in quickly then holds a moment then fades
        kickTimer -= dt;
        if (kickTimer <= 0f)
        {
            kickTarget = 0f;
        }

        // rise fast when going up to the kick and fall slowly when coming back down
        float kickSpeed = config.camera.kickFallSmoothing;
        if (kickTarget > kickAmount)
        {
            kickSpeed = config.camera.kickRiseSmoothing;
        }
        kickAmount = Mathf.Lerp(kickAmount, kickTarget, 1f - Mathf.Exp(-kickSpeed * dt));

        // apply
        var lens = vcam.Lens;
        lens.FieldOfView = baseFov + fovChange + kickAmount;
        vcam.Lens = lens;

        if (follow != null)
        {
            follow.CameraDistance = baseDistance * distanceMultiplier;
            follow.CameraSide = cameraSide;
        }
    }

    // the shoulder offset puts the camera beside you
    // with a wall on that side the camera gets pushed in until it's nearly on top of you
    // so look for a wall on the camera's side and shrink the offset to what fits
    // the closer the wall the closer to the middle
    // camera side is 0 for the left shoulder and half for centred and 1 for the right shoulder
    private float centerAwayFromWall(float side)
    {
        Transform target = vcam.Follow;
        if (target == null || follow == null)
        {
            return side;
        }

        float shoulder = Mathf.Abs(follow.ShoulderOffset.x);
        if (shoulder < 0.0001f || Mathf.Approximately(side, 0.5f))
        {
            return side;
        }

        // which way the shoulder points
        float shoulderSign = -1f;
        if (side > 0.5f)
        {
            shoulderSign = 1f;
        }

        // the shoulder offset is measured along the target's heading so yaw only
        Vector3 toShoulder = Quaternion.Euler(0f, target.eulerAngles.y, 0f) * Vector3.right * shoulderSign;

        // side runs from 0 to 1 with half in the middle so twice side minus 1 runs from negative 1 to 1
        // its absolute value is how far from the middle we are as a fraction of the full shoulder offset
        float offset = shoulder * Mathf.Abs(2f * side - 1f);

        // the gap to leave between the camera and the wall
        float clearance = config.camera.wallClearance;

        // sweep a ball the size of the camera and not a thin ray so seams and corners and small gaps give a smooth distance
        // the distance the ball travels before touching is already the gap between the camera's edge and the wall
        // so it only needs the clearance added to how far we look
        int count = Physics.SphereCastNonAlloc(target.position, follow.AvoidObstacles.CameraRadius, toShoulder, wallHits, offset + clearance, follow.AvoidObstacles.CollisionFilter, QueryTriggerInteraction.Ignore);

        float nearest = float.MaxValue;
        string ignoreTag = follow.AvoidObstacles.IgnoreTag;
        for (int i = 0; i < count; i++)
        {
            Collider hitCollider = wallHits[i].collider;

            // the player's own colliders are not walls
            if (hitCollider.transform.IsChildOf(target.root))
            {
                continue;
            }

            // anything with the ignore tag is skipped like the cinemachine avoidance does
            if (!string.IsNullOrEmpty(ignoreTag) && hitCollider.CompareTag(ignoreTag))
            {
                continue;
            }

            if (wallHits[i].distance < nearest)
            {
                nearest = wallHits[i].distance;
            }
        }

        if (nearest == float.MaxValue)
        {
            return side;
        }

        // how far out the camera is allowed to go before it hits the clearance gap
        float allowed = Mathf.Clamp(nearest - clearance, 0f, offset);

        // turn the allowed distance back into a camera side value
        // allowed divided by shoulder is how far out as a fraction of the full offset
        // half of that on the correct side of the middle gives the camera side
        return 0.5f + Mathf.Sign(side - 0.5f) * 0.5f * (allowed / shoulder);
    }

    private void OnDisable()
    {
        // put the camera back how it was set up
        if (vcam != null)
        {
            var lens = vcam.Lens;
            lens.FieldOfView = baseFov;
            vcam.Lens = lens;
        }

        if (follow != null)
        {
            follow.CameraDistance = baseDistance;
            follow.CameraSide = baseSide;
        }
    }
}
