using UnityEngine;

public class BuffUIManager : MonoBehaviour
{
    public GameObject buffIconPrefab;
    public Transform buffBarParent;

    public void UpdateBuffUI()
    {
        // Rens tidligere ikoner
        foreach (Transform child in buffBarParent)
            Destroy(child.gameObject);

        // Tilføj ikoner for aktive buffs
        foreach (BuffData buff in PlayerBuffManager.Instance.activeBuffs)
        {
            GameObject iconGO = Instantiate(buffIconPrefab, buffBarParent);
            BuffIcon icon = iconGO.GetComponent<BuffIcon>();
            icon.Setup(buff);
        }
    }
}
