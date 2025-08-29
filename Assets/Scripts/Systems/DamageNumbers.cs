using UnityEngine;
using TMPro;

public class DamageNumbers : MonoBehaviour
{
    public static DamageNumbers Instance;

    [Header("Setup")]
    public RectTransform container;
    public GameObject numberPrefab;

    // Hardcoded settings for test
    private float verticalOffset = 1.5f;
    private float randomSpread = 0.75f;

    [Header("Colors")]
    public Color defaultColor = Color.white;
    public Color fireColor = Color.red;
    public Color lightningColor = Color.cyan;
    public Color burnColor = new Color(1f, 0.5f, 0f);

    void Awake()
    {
        Instance = this;
    }

    public void Show(Vector3 worldPos, int damage, DamageElement element)
    {
        if (numberPrefab == null || container == null) return;

        // Offset + spread
        Vector3 spawnPos = worldPos
            + Vector3.up * verticalOffset
            + new Vector3(Random.Range(-randomSpread, randomSpread),
                          Random.Range(-randomSpread, randomSpread), 0);

        // World  Screen
        Vector2 screenPos = Camera.main.WorldToScreenPoint(spawnPos);

        // Instantiate prefab
        GameObject go = Instantiate(numberPrefab, container);

        TMP_Text txt = go.GetComponent<TMP_Text>();
        if (txt != null)
        {
            txt.text = damage.ToString();

            // Vælg farve efter element
            switch (element)
            {
                case DamageElement.Fire: txt.color = fireColor; break;
                case DamageElement.Lightning: txt.color = lightningColor; break;
                case DamageElement.Burn: txt.color = burnColor; break;
                default: txt.color = defaultColor; break;
            }

            // Debug log for test
            Debug.Log($"[DamageNumbers] dmg={damage}, element={element}, color={txt.color}, pos={worldPos}");
        }

        // Placér i UI
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.position = screenPos;

        // Destroy efter 1 sek (eller lad DamageNumberEffect styre det)
        Destroy(go, 1f);
    }
}
