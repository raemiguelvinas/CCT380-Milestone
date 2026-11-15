using UnityEngine;

// puffs of dust while running on the ground
// this is a continuous effect and not a one shot event
// so it watches the character itself instead of listening to an event channel
// tuning lives in the movement config asset
public class RunDust : MonoBehaviour
{
    [SerializeField] private MovementConfig config;
    [SerializeField] private CharacterController source; // leave empty to use the one on this object
    [SerializeField] private Transform feet;

    private float timer;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"{name}: No MovementConfig assigned, so there will be no run dust.", this);
            config = ScriptableObject.CreateInstance<MovementConfig>();
        }

        if (source == null)
        {
            source = GetComponentInParent<CharacterController>();
        }
    }

    private void Update()
    {
        if (source == null || config.runDust.dustPrefab == null || feet == null)
        {
            return;
        }

        timer -= Time.deltaTime;

        // flat speed is the length of the velocity with the up and down part thrown away
        Vector3 v = source.velocity;
        float flatSpeed = new Vector3(v.x, 0f, v.z).magnitude;

        if (source.isGrounded && flatSpeed > config.runDust.moveThreshold && timer <= 0f)
        {
            Instantiate(config.runDust.dustPrefab, feet.position, config.runDust.dustPrefab.transform.rotation);
            timer = config.runDust.interval;
        }
    }
}
