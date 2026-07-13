using System.Collections;
using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    private Health health;
    private SpriteRenderer spriteRenderer;
    private Player playerMovement;
    private Animator anim;
    private Rigidbody2D rb;

    [Header("Hurt Settings")]
    public Color hurtColor = Color.red;
    public float hurtDuration;
    public float stunDuration; 
    private Color originalColor;

    private void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerMovement = GetComponent<Player>();
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.onDamaged += HandleHurt;
            health.onDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onDamaged -= HandleHurt;
            health.onDeath -= HandleDeath;
        }
    }

    private void HandleHurt()
    {
        //Chớp đỏ
        if (spriteRenderer != null)
        {
            StopCoroutine("FlashHurtColor");
            StartCoroutine("FlashHurtColor");
        }

        //Chạy animation 
        if (anim != null)
        {
            anim.SetTrigger("hurt");
        }

        //Làm choáng 
        StopCoroutine("StunPlayerRoutine");
        StartCoroutine("StunPlayerRoutine");
    }

    private IEnumerator StunPlayerRoutine()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        yield return new WaitForSeconds(stunDuration);
        
        //Dead ?
        if (health != null && health.health > 0 && playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }

    private IEnumerator FlashHurtColor()
    {
        spriteRenderer.color = hurtColor;
        yield return new WaitForSeconds(hurtDuration);
        spriteRenderer.color = originalColor;
    }

    private void HandleDeath()
    {
        if (anim != null)
        {
            anim.SetBool("isDead", true);
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;          
        }
        
        Debug.Log("Player đã chết!");
    }

    public void Revive()
    {
        if (anim != null)
        {
            anim.SetBool("isDead", false);
            anim.Rebind();
            anim.Update(0f);
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            if (playerMovement.StateMachine != null && playerMovement.IdleState != null)
            {
                playerMovement.StateMachine.ChangeState(playerMovement.IdleState);
            }
        }
    }
}
