using UnityEngine;

public class HealthPotion : MonoBehaviour
{
    [Header("Potion Settings")]
    public int healAmount; 

    private void OnTriggerEnter2D(Collider2D other)
    {       
        if (other.CompareTag("Player"))
        {
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null)
            {              
                playerHealth.changeHealth(healAmount);                             
                Destroy(gameObject);
            }
        }
    }
}
