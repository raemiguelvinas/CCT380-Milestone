using UnityEngine;

// a particle effect that follows the wall while you slide down it
// put this on an object and add a particle system on it or as a child
// and it moves to the point you're touching and turns to face away from the wall and emits only while you slide
// set the particle system to looping and turn off play on awake
// and use world simulation space so the trail stays on the wall as you drop
// tuning lives in the movement config asset and the references live here because they're in the scene
public class WallSlideEffect : MonoBehaviour
{
    [SerializeField] private MovementConfig config;
    [Tooltip("Leave empty to find it on a parent, or in the scene.")]
    [SerializeField] private PlayerMovementInput player;
    [Tooltip("Leave empty to use one on this object or a child.")]
    [SerializeField] private ParticleSystem particles;

    private CharacterController controller;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"{name}: No MovementConfig assigned. Using defaults; assign one in the Inspector.", this);
            config = ScriptableObject.CreateInstance<MovementConfig>();
        }

        if (player == null)
        {
            player = GetComponentInParent<PlayerMovementInput>();
        }

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerMovementInput>();
        }

        if (particles == null)
        {
            particles = GetComponentInChildren<ParticleSystem>();
        }

        if (player == null || particles == null)
        {
            Debug.LogWarning($"{name}: needs a PlayerMovementInput and a ParticleSystem (on this object or a child). Doing nothing until it has them.", this);
            enabled = false;
            return;
        }

        controller = player.GetComponent<CharacterController>();
    }

    // late update so we follow after the player has moved this frame
    private void LateUpdate()
    {
        // fall speed is the downward speed so it's the up velocity flipped to positive
        float fallSpeed = 0f;
        if (controller != null)
        {
            fallSpeed = -controller.velocity.y;
        }

        bool sliding = player.IsWallSliding && fallSpeed >= config.wallSlideEffect.minSlideSpeed;

        if (sliding)
        {
            Vector3 normal = player.WallNormal;
            if (normal.sqrMagnitude < 0.0001f)
            {
                return;
            }
            normal.Normalize();

            // turn the effect so its up or forward points away from the wall along the normal
            // from to rotation gives the turn that carries up onto the normal
            // look rotation does the same for the forward direction
            Quaternion align;
            if (config.wallSlideEffect.alignAxis == PrefabSpawnConfig.AlignAxis.Up)
            {
                align = Quaternion.FromToRotation(Vector3.up, normal);
            }
            else
            {
                align = Quaternion.LookRotation(normal);
            }

            // lift off the wall along the normal so it doesn't clip into it and then shift up or down by the height offset
            Vector3 position = player.WallPoint + normal * config.wallSlideEffect.surfaceOffset + Vector3.up * config.wallSlideEffect.heightOffset;
            transform.SetPositionAndRotation(position, align);

            if (!particles.isEmitting)
            {
                particles.Play();
            }
        }
        else if (particles.isEmitting)
        {
            // let the particles already out fade naturally instead of vanishing
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
