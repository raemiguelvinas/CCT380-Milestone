using UnityEngine;

// a small marker that sits on the point you'd swing from if you pressed the swing button right now
// that means a swingable surface under the crosshair and within range
// it is hidden otherwise and while swinging
// it also shrinks away as the camera gets close to it so it can never fill the screen
// size and pulse and the near camera fade live in the swing config asset
public class SwingPointIndicator : MonoBehaviour
{
    [SerializeField] private SwingConfig config;
    [SerializeField] private PlayerMovementInput player;
    [Tooltip("The visual to show/hide: a CHILD object, so this script keeps running while it's hidden. Leave empty to use the first child.")]
    [SerializeField] private GameObject visual;

    private Camera cam;

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

        if (visual == null && transform.childCount > 0)
        {
            visual = transform.GetChild(0).gameObject;
        }

        if (visual == null)
        {
            Debug.LogError($"{name}: SwingPointIndicator needs a child object to show.", this);
        }

        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (visual == null)
        {
            return;
        }

        bool show = player != null && player.SwingPreviewValid && !player.IsSwinging;

        // shrink away as the camera gets close to the point and hide it inside the hide distance
        float nearScale = 1f;
        if (show)
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam != null)
            {
                float dist = Vector3.Distance(cam.transform.position, player.SwingPreviewPoint);

                // the full distance must always be a little past the hide distance so the range below isn't empty
                float full = Mathf.Max(config.indicatorFullDistance, config.indicatorHideDistance + 0.01f);

                // inverse lerp turns the distance into a 0 to 1 scale
                // 0 at the hide distance and 1 at the full distance
                nearScale = Mathf.InverseLerp(config.indicatorHideDistance, full, dist);
                if (nearScale <= 0f)
                {
                    show = false;
                }
            }
        }

        if (visual.activeSelf != show)
        {
            visual.SetActive(show);
        }

        if (!show)
        {
            return;
        }

        // sit on the point
        transform.position = player.SwingPreviewPoint;

        // the pulse is a sine wave around 1 so the size swings a little above and below normal
        // time times the pulse speed sets how fast and the pulse amount sets how far
        float pulse = 1f + Mathf.Sin(Time.time * config.indicatorPulseSpeed) * config.indicatorPulse;
        transform.localScale = Vector3.one * (config.indicatorSize * pulse * nearScale);
    }
}
