using UnityEngine;

// a plain straight rope from the hand to the swing anchor shown while swinging
// width comes from the swing config
public class SwingRope : MonoBehaviour
{
    [SerializeField] private SwingConfig config;
    [SerializeField] private PlayerMovementInput player;
    [SerializeField] private Transform hand;
    [SerializeField] private LineRenderer line;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"{name}: No SwingConfig assigned. Using defaults.", this);
            config = ScriptableObject.CreateInstance<SwingConfig>();
        }

        if (player == null)
        {
            player = GetComponentInParent<PlayerMovementInput>();
        }

        if (line == null)
        {
            line = GetComponent<LineRenderer>();
        }

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.enabled = false;
    }

    private void LateUpdate()
    {
        bool on = player != null && player.IsSwinging;
        if (line.enabled != on)
        {
            line.enabled = on;
        }

        if (!on)
        {
            return;
        }

        line.widthMultiplier = config.ropeWidth;

        // the rope starts at the hand or at the player if there is no hand and ends at the anchor
        Vector3 start = player.transform.position;
        if (hand != null)
        {
            start = hand.position;
        }
        line.SetPosition(0, start);
        line.SetPosition(1, player.SwingAnchor);
    }
}
