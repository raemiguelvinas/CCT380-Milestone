using UnityEngine;

// the hit flash timing and optionally its color
// create it from the asset create menu under config
[CreateAssetMenu(menuName = "Config/Flash Config", fileName = "FlashConfig")]
public class FlashConfig : ScriptableObject
{
    [Header("Shader Properties")]
    public string amountProperty = "_FlashAmount";
    public string colorProperty = "_FlashColor";

    [Header("Look")]
    [Tooltip("Off = use the Flash Color set on the material. On = use the color below instead.")]
    public bool overrideColor = false;
    [ColorUsage(true, true)] public Color flashColor = Color.white;

    [Header("Timing")]
    [Tooltip("Seconds the flash stays at full strength before fading.")]
    public float holdTime = 0.05f;
    [Tooltip("Seconds the flash takes to fade back to normal.")]
    public float flashDuration = 0.2f;
}
