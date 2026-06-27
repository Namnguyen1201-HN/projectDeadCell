using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerRespawn respawn = collision.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                // Cập nhật điểm hồi sinh thành vị trí của checkpoint này
                respawn.UpdateRespawnPosition(transform.position);
            }
        }
    }
}
