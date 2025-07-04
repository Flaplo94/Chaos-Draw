using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Abilities/Ability")]
public class Ability : ScriptableObject
{
    public string abilityName;
    public Sprite icon;
    public float cooldown = 3f;
    public GameObject effectPrefab; // Optional — for ability VFX
    public bool spawnAtMousePosition;
}
