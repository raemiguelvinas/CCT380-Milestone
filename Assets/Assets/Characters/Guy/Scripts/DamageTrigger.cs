using UnityEngine;
using UnityEngine.Events;

public class DamageTrigger : MonoBehaviour
{
    public GameEventSO health;
    [Tooltip("How much damage this hazard deals. Make one DamageConfig per kind of hazard.")]
    public DamageConfig config;

    public UnityEvent onHit;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (config == null)
            {
                Debug.LogError($"{name}: No DamageConfig assigned, so this deals no damage.", this);
                return;
            }
            health.subtract(config.damage);
            onHit.Invoke();
        }
    }
}
