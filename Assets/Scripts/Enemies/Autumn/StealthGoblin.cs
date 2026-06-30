using UnityEngine;

namespace AutumnLevel
{
    /// <summary>
    /// Goblin đặc trưng Mùa Thu (Tàn Tích).
    /// Hành vi: Có khả năng tàng hình khi ở xa. Khi player lại gần sẽ lộ hình.
    /// Kế thừa toàn bộ hành vi của GoblinEnemy.
    /// </summary>
    public class StealthGoblin : GoblinEnemy
    {
        [Header("Stealth Settings")]
        public float fadeAlpha = 0.15f;         // Độ mờ khi tàng hình (còn 15% mờ mờ)
        public float revealDistance = 4f;       // Khoảng cách bị lộ hoàn toàn
        public float fadeSpeed = 3f;            // Tốc độ chuyển từ mờ sang rõ

        private bool isVisible = false;

        protected override void EnemyBehavior()
        {
            float dist = DistanceToPlayer();

            // 1. Cập nhật trạng thái tàng hình
            bool shouldReveal = dist <= revealDistance;

            if (shouldReveal && !isVisible)
            {
                isVisible = true;
                // Có thể thêm particle effect/âm thanh khi bị lộ
            }
            else if (!shouldReveal && dist > aggroRange)
            {
                // Chỉ tàng hình lại nếu ra khỏi tầm aggro
                isVisible = false;
            }

            // 2. Thay đổi độ mờ (Alpha) dần dần
            if (spriteRenderer != null)
            {
                float targetAlpha = isVisible ? 1f : fadeAlpha;
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * fadeSpeed);
                spriteRenderer.color = c;
            }

            // 3. Thực hiện AI gốc của Goblin (lao vào đánh)
            // Nếu tàng hình nhưng bị aggro (vd: do bị tấn công), vẫn đuổi theo
            base.EnemyBehavior();
        }
    }
}
