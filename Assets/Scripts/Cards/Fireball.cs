using UnityEngine;

public class Fireball : MonoBehaviour, IAbilityBehavior
{
    [Header("Tuning")]
    public float speed = 10f;
    public int damage = 10;
    public float aoeRadius = 2f;

    [Header("Visuals")]
    [SerializeField] private GameObject aoeVisual;  
    [SerializeField] private Color aoeColor = Color.red;

    private Vector2 direction;
    private bool impacted;

   
    private Animator anim;
    private Collider2D col;

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        direction = dir.normalized;

        switch (rarity)
        {
            case Rarity.Uncommon: aoeRadius *= 1.2f; break;
            case Rarity.Rare: aoeRadius *= 1.4f; damage += 5; break;
            case Rarity.Epic: aoeRadius *= 1.6f; damage += 10; break;
            case Rarity.Legendary: aoeRadius *= 2f; damage += 20; break;
        }
        return true;
    }

    void Awake()
    {
        anim = GetComponent<Animator>();     
        col = GetComponent<Collider2D>();   
    }

    void Update()
    {
        if (impacted) return; 
        transform.position += (Vector3)direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted) return;

        if (!other.CompareTag("Enemy") && !other.CompareTag("Boss"))
            return;

        DoImpact();
    }

    private void DoImpact()
    {
        if (impacted) return;
        impacted = true;

        
        speed = 0f;
        if (col) col.enabled = false;

        
        var hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(damage);
            if (hit.TryGetComponent(out BossHealth bh)) bh.TakeDamage(damage);
        }

        
        if (aoeVisual != null)
        {
            GameObject vfx = Instantiate(aoeVisual, transform.position, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * aoeRadius * 2f;
            var sr = vfx.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = aoeColor;
            Destroy(vfx, 0.15f);
        }

        
        if (anim) anim.SetTrigger("Impact");
        else Destroy(gameObject); 
    }

    public void OnImpactFinished()
    {
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}
