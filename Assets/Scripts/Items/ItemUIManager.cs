using UnityEngine;

public class ItemUIManager : MonoBehaviour
{
    public GameObject itemIconPrefab;   // prefab with ItemIcon component + Image
    public Transform itemBarParent;     // a horizontal layout group, etc.

    public void UpdateItemUI()
    {
        foreach (Transform child in itemBarParent)
            Destroy(child.gameObject);

        var seen = new System.Collections.Generic.HashSet<string>();
        foreach (var item in PlayerItemManager.Instance.ownedItems)
        {
            if (item == null) continue;
            string id = Normalize(item.internalID);
            if (seen.Contains(id)) continue;
            seen.Add(id);

            var iconGO = Instantiate(itemIconPrefab, itemBarParent);
            var icon = iconGO.GetComponent<ItemIcon>();
            icon.Setup(item);
        }

        static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().ToLowerInvariant();
            s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
            return s;
        }
    }
}
