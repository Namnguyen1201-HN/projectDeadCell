using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Animator anim;
    public Health health;

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
        // Kích hoạt hoạt ảnh chết (nếu có)
        // anim.SetBool("isDead", true); 
        
        // Tắt collider để không bị OverlapCircle quét trúng nữa
        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = false;
        
        // Tắt script Enemy (hoặc huỷ object tuỳ cơ chế game)
        this.enabled = false;
    }
}
