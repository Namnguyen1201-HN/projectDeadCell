using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Animator anim;
    public Health health;

    [Header("DeathFX")]
    [SerializeField] private GameObject[] deathParts; 
    [SerializeField] private float spawnForce = 5f;
    [SerializeField] private float torque = 5;
    [SerializeField] private float lifeTime = 2f;

    [Header("Drop Item")]
    public GameObject keyPrefab;

    private void OnEnable(){
        health.onDamaged += HandleDamage;     
        health.onDeath += HandleDeath;
    }

    private void OnDisable(){
        health.onDamaged -= HandleDamage;     
        health.onDeath -= HandleDeath;
    }

    private void HandleDamage(){
        anim.SetTrigger("isDamaged");
    }   

    private void HandleDeath(){
        if (keyPrefab != null) {
            Instantiate(keyPrefab, transform.position, Quaternion.identity);
        }
        
        foreach (GameObject prefab in deathParts) {
            Quaternion rotation = Quaternion.Euler(0,0,Random.Range(0.5f,1)).normalized;
            GameObject part = Instantiate(prefab,transform.position,rotation);
            
            Rigidbody2D rb = part.GetComponent<Rigidbody2D>();

            Vector2 randomDirection = new Vector2(Random.Range(-1f,1f), Random.Range(0f,1f)).normalized;
            rb.velocity = randomDirection * spawnForce;
            rb.AddTorque(Random.Range(-torque,torque), ForceMode2D.Impulse);

            Destroy(part, lifeTime);
        }
        Destroy(gameObject);
    }
}
