using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class Health : MonoBehaviour
{
    public event Action onDamaged;
    public event Action onDeath; 
    public event Action<int, int> onHealthChanged;

    public int health;
    public int maxHealth;

    private void Start()
    {
        health = maxHealth;
        onHealthChanged?.Invoke(health, maxHealth);
    }


    public void changeHealth(int amount) 
    {
        health += amount;

        if (health > maxHealth)
        {
            health = maxHealth;
        }
        else if (health <= 0){
            health = 0;
            onDeath?.Invoke();
        }
        else if (amount < 0) {
            onDamaged?.Invoke();
        }
        
        onHealthChanged?.Invoke(health, maxHealth);
    }

}
