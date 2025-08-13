using UnityEngine;

public static class ServiceRunner
{
    /// <summary>
    /// Kører en shop-service. Lige nu: RemoveCard.
    /// Udvid her, hvis du senere vil have Heal, Reroll, osv.
    /// </summary>
    public static void Run(ShopItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("[ServiceRunner] item == null");
            return;
        }

        switch (item.serviceType)
        {
            case ShopServiceType.RemoveCard:
                RemoveCardModalUI.Show();
                break;

            default:
                Debug.LogWarning("[ServiceRunner] Unhandled service: " + item.serviceType);
                break;
        }
    }
}
