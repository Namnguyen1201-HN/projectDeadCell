using UnityEngine;

public class Arrow : MonoBehaviour
{
    public int damage;
    public float lifetime = 3f;

    void Start()
    {
        // This script is legacy and should no longer be used. PlayerProjectile.cs handles everything.
        // Destroying this component to prevent any conflicts.
        Destroy(this);
    }

    // Removed OnTriggerEnter2D to ensure it NEVER intercepts collisions.
}
