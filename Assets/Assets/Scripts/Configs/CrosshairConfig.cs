using UnityEngine;

// the two crosshair looks
// created from the asset create menu under config
[CreateAssetMenu(menuName = "Config/Crosshair Config", fileName = "CrosshairConfig")]
public class CrosshairConfig : ScriptableObject
{
    [Tooltip("Shown while the crosshair is on a target you can grapple or swing from")]
    public Sprite targetingSprite;
    [Tooltip("Shown the rest of the time")]
    public Sprite idleSprite;
}
