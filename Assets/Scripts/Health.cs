using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 1;
    public int currentHealth = 1;

    public int MaxHealth
    {
        get => maxHealth;
        set
        {
            maxHealth = Mathf.Max(1, value);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }
    }

    public int CurrentHealth
    {
        get => currentHealth;
        set => currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }
}
