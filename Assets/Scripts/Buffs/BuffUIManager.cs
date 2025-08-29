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
        foreach (var active in PlayerBuffManager.Instance.ActiveBuffs)
        {
            BuffData buff = active.data;   //  vi tager BuffData ud af ActiveBuff

            GameObject iconGO = Instantiate(buffIconPrefab, buffBarParent);
            BuffIcon icon = iconGO.GetComponent<BuffIcon>();
            icon.Setup(buff);
        }
    }
}
