using UnityEngine;

public interface IAbilityBehavior
{
    // Return true to allow activation; false to veto (card should not be consumed)
    bool Initialize(Vector2 direction, Rarity rarity);
}