using UnityEngine;

public class Spike : MonoBehaviour
{
    [Header("Spike Settings")]
    public int damage;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        
        if (collision.CompareTag("Player"))
        {          
            PlayerRespawn respawn = collision.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                respawn.TakeHazardDamageAndRespawn(damage);
            }
        }
        
        else if (collision.GetComponent<EnemyController>() != null)
        {
            Health enemyHealth = collision.GetComponent<Health>();
            if (enemyHealth != null)
            {               
                enemyHealth.changeHealth(-9999);
            }
        }
    }
}
