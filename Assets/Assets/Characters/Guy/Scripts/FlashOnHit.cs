using System.Collections;
using UnityEngine;

// flashes the player or any character when on hit is called
// it drives the flash amount property of the flash player shader on every renderer in the list
// using material property blocks so the shared material isn't modified
// timing and color live in the flash config asset
//
// from another script call on hit on the flash on hit component
public class FlashOnHit : MonoBehaviour
{
    [SerializeField] private FlashConfig config;
    [Tooltip("Renderers to flash. If left empty, every SkinnedMeshRenderer under this object is used.")]
    public Renderer[] targetRenderers;

    private MaterialPropertyBlock propBlock;
    private int amountId;
    private int colorId;
    private Coroutine activeFlash;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"{name}: No FlashConfig assigned. Using defaults.", this);
            config = ScriptableObject.CreateInstance<FlashConfig>();
        }

        propBlock = new MaterialPropertyBlock();
        amountId = Shader.PropertyToID(config.amountProperty);
        colorId = Shader.PropertyToID(config.colorProperty);

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }
    }

    // call this when the character is hit
    // it is safe to spam because a new hit restarts the flash
    public void onHit()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            Debug.LogWarning($"{name}: FlashOnHit has no renderers to flash.");
            return;
        }

        if (activeFlash != null)
        {
            StopCoroutine(activeFlash);
        }

        activeFlash = StartCoroutine(flashRoutine());
    }

    private IEnumerator flashRoutine()
    {
        setAmount(1f);

        if (config.holdTime > 0f)
        {
            yield return new WaitForSeconds(config.holdTime);
        }

        float t = 0f;
        while (t < config.flashDuration)
        {
            t += Time.deltaTime;

            // t divided by the duration is how far through the fade we are from 0 to 1
            // flipping it with 1 minus gives a flash that starts full and fades to nothing
            setAmount(1f - Mathf.Clamp01(t / config.flashDuration));
            yield return null;
        }

        setAmount(0f);
        activeFlash = null;
    }

    private void setAmount(float amount)
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer r = targetRenderers[i];
            if (r == null)
            {
                continue;
            }

            r.GetPropertyBlock(propBlock);
            propBlock.SetFloat(amountId, amount);
            if (config.overrideColor)
            {
                propBlock.SetColor(colorId, config.flashColor);
            }
            r.SetPropertyBlock(propBlock);
        }
    }

    private void OnDisable()
    {
        // don't leave the character stuck flashing if this gets disabled mid flash
        if (activeFlash != null)
        {
            StopCoroutine(activeFlash);
            activeFlash = null;
        }

        if (targetRenderers != null && propBlock != null)
        {
            setAmount(0f);
        }
    }
}
