using UnityEngine;

public class Key : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.keyCount++;
            Destroy(gameObject);
        }
    }
}
