using UnityEngine;

public class EnemyAnimationEvents : MonoBehaviour
{
    private EnemyDmg damage;
    private EnemyHealth health;

    void Awake()
    {
        if (damage == null) damage = GetComponentInChildren<EnemyDmg>(true);
        if (health == null) health = GetComponentInParent<EnemyHealth>();
    }

    // Attack animation events
    public void AE_HitboxOn() { if (damage != null) damage.AE_HitboxOn(); }
    public void AE_HitboxOff() { if (damage != null) damage.AE_HitboxOff(); }
    public void AE_AttackFinished() { if (damage != null) damage.AE_AttackFinished(); }
}
