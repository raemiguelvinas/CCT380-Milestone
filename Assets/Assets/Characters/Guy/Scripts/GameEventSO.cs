using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHealth", menuName = "Scriptable Objects/GameEventSO")]
public class GameEventSO : ScriptableObject
{
    public float maxHealth = 100f;
    public float currentHealth;

    void OnEnable()
    {
        currentHealth = maxHealth;
    }
    public void subtract(float dmg)
    {
        currentHealth -= dmg;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
        else if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        
        Debug.Log("Damage taken, health is at " + currentHealth);
    }
}
