using UnityEngine;

public class Chest : MonoBehaviour
{
    private bool isPlayerNearby = false;
    private Player playerInstance = null;
    public bool isOpened = false;
    private Animator anim;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            isPlayerNearby = true;
            playerInstance = player;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            isPlayerNearby = false;
            playerInstance = null;
        }
    }

    private void Update()
    {
        if (isPlayerNearby && !isOpened && playerInstance != null)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (playerInstance.keyCount > 0)
                {
                    playerInstance.keyCount--;
                    playerInstance.UnlockSkill("DoubleJump");
                    isOpened = true;
                    Debug.Log("Chest opened! Double jump acquired.");
                    
                    // Kích hoạt hoạt ảnh mở rương
                    if(anim == null) anim = GetComponent<Animator>();
                    if (anim != null) {
                        anim.SetTrigger("Open");
                    }
                }
                else
                {
                    Debug.Log("Need a key to open this chest!");
                }
            }
        }
    }
}
