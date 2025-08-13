using UnityEngine;

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "ChaosDraw/Shop/Catalog")]
public class ShopCatalog : ScriptableObject
{
    public ShopItem[] items; // træk ShopItem-assets herind
}
