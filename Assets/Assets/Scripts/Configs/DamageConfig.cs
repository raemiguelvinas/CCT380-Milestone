using UnityEngine;

// how much damage a hazard deals
// make one per kind of hazard like spikes or a laser
// create it from the asset create menu under config
[CreateAssetMenu(menuName = "Config/Damage Config", fileName = "DamageConfig")]
public class DamageConfig : ScriptableObject
{
    public float damage = 10f;
}
