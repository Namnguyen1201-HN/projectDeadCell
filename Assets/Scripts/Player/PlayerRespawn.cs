using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 currentRespawnPosition;
    private Health playerHealth;
    private Rigidbody2D rb;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        currentRespawnPosition = transform.position;
    }

    public void UpdateRespawnPosition(Vector3 newPosition)
    {
        currentRespawnPosition = newPosition;
    }

    //Spike
    public void TakeHazardDamageAndRespawn(int damageAmount)
    {
    
        if (playerHealth != null)
        {
            playerHealth.changeHealth(-damageAmount);
        }

        if (playerHealth != null && playerHealth.health > 0)
        {
            transform.position = currentRespawnPosition;
            
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
        }
    }

    public void ReviveAtCheckpoint()
    {
        
        if (playerHealth != null)
        {            
            playerHealth.changeHealth(playerHealth.maxHealth); 
        }

        transform.position = currentRespawnPosition;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        PlayerEffects effects = GetComponent<PlayerEffects>();
        if (effects != null)
        {
            effects.Revive();
        }
    }
}
