using UnityEngine;

namespace AutumnLevel
{
    /// <summary>
    /// Dơi đặc trưng Mùa Thu (Tàn Tích).
    /// Hành vi: Bay bình thường theo sin wave, nhưng khi player trong tầm sẽ thỉnh thoảng lao tới rất nhanh (Dash).
    /// </summary>
    public class SwiftBat : BatEnemy
    {
        [Header("Swift Settings")]
        public float dashSpeed = 12f;
        public float dashCooldown = 3f;
        public float dashDuration = 0.5f;

        private float nextDashTime = 0f;
        private bool isDashing = false;

        protected override void EnemyBehavior()
        {
            if (isDashing) return; // Nếu đang dash thì ngắt AI thường

            float dist = DistanceToPlayer();

            // Lao nhanh về phía player nếu đủ đk
            if (dist <= aggroRange && dist > attackRange && Time.time >= nextDashTime)
            {
                StartCoroutine(DashAttackRoutine());
            }
            else
            {
                base.EnemyBehavior();
            }
        }

        private System.Collections.IEnumerator DashAttackRoutine()
        {
            isDashing = true;
            nextDashTime = Time.time + dashCooldown;

            if (HasAnimatorParameter("isDashing")) anim.SetTrigger("isDashing");

            Vector2 dashDir = (player.position - transform.position).normalized;
            rb.velocity = dashDir * dashSpeed;

            yield return new WaitForSeconds(dashDuration);

            // Xong dash, trở về bay bình thường
            rb.velocity = Vector2.zero;
            isDashing = false;
        }
    }
}
