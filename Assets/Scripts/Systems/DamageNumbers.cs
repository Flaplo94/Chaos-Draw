using UnityEngine;
using TMPro;

public class DamageNumbers : MonoBehaviour
{
    public static DamageNumbers Instance;

    [Header("Setup")]
    public RectTransform container;       // det tomme "DamageNumbers" under UI
    public GameObject numberPrefab;       // dit DamageNumberTemplate prefab

    void Awake()
    {
        Instance = this;
    }

    public void Show(Vector3 worldPos, int damage)
    {
        // 1. Find hvor på skærmen worldPos er
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // 2. Lav et nyt tal
        GameObject go = Instantiate(numberPrefab, container);

        // 3. Sæt teksten
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = damage.ToString();

        // 4. Placér det
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.position = screenPos;

        // 5. Fjern det efter 1 sekund
        Destroy(go, 1f);
    }
}
