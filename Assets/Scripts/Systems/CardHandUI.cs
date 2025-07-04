using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class CardHandUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image[] cardSlots;

    [Header("Ability Settings")]
    [SerializeField] private Ability[] availableAbilities;
    [SerializeField] private float defaultCooldown = 3f;

    [Header("Discard UI")]
    [SerializeField] private TextMeshProUGUI discardCounterText;
    [SerializeField] private Image discardSlotIcon;

    private Ability[] equippedAbilities = new Ability[4];
    private float[] cooldownTimers = new float[4];
    private int discardCount = 0;
    [SerializeField] private Transform playerTransform;

    private void Start()
    {
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            equippedAbilities[i] = availableAbilities[Random.Range(0, availableAbilities.Length)];
            cardSlots[i].sprite = equippedAbilities[i].icon;
            cardSlots[i].color = Color.white;
        }

        UpdateDiscardText();
    }

    private void Update()
    {
        HandleKeyboardInput();

        for (int i = 0; i < cooldownTimers.Length; i++)
        {
            if (cooldownTimers[i] <= 0f) continue;

            cooldownTimers[i] -= Time.deltaTime;
            // Removed cooldownOverlays[i].fillAmount = fill;

            if (cooldownTimers[i] <= 0f)
                RedrawAbility(i);
        }
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            TryUseAbility(0);
            Debug.Log("Key 1 pressed");
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame) TryUseAbility(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) TryUseAbility(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) TryUseAbility(3);
    }

    private void TryUseAbility(int index)
    {
        if (cooldownTimers[index] > 0f || equippedAbilities[index] == null) return;
        UseAbility(index);
    }

    private void UseAbility(int index)
    {
        Ability ability = equippedAbilities[index];

        if (ability.effectPrefab != null)
        {
            Vector3 spawnPos = transform.position;

            if (ability.spawnAtMousePosition && Mouse.current != null)
            {
                Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
                spawnPos = new Vector3(worldPos.x, worldPos.y, 0f);
            }
            GameObject instance = Instantiate(ability.effectPrefab, spawnPos, Quaternion.identity);
            Debug.Log("Instantiated: " + instance.name);
            Fireball fireball = instance.GetComponent<Fireball>();
            if (fireball != null)
            {
                // Get mouse position in world space
                Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
                mouseWorldPos.z = playerTransform.position.z; // Keep on same Z plane

                // Calculate direction
                Vector2 direction = (mouseWorldPos - playerTransform.position).normalized;

                fireball.SetDirection(direction);
                Debug.Log("Fireball component found and direction set.");
            }
        }
        else
        {
            Debug.LogWarning("No effectPrefab assigned to this ability!");
        }

        cooldownTimers[index] = GetCooldown(index);
        
        cardSlots[index].color = new Color(1, 1, 1, 0);

        

        discardCount++;
        UpdateDiscardText();
        equippedAbilities[index] = null;
        cardSlots[index].sprite = null;
    }

    private float GetCooldown(int index)
    {
        return equippedAbilities[index] != null ? equippedAbilities[index].cooldown : defaultCooldown;
    }

    private void RedrawAbility(int index)
    {
        Ability newAbility = availableAbilities[Random.Range(0, availableAbilities.Length)];
        equippedAbilities[index] = newAbility;
        cardSlots[index].sprite = newAbility.icon;
        cardSlots[index].color = Color.white;
    }

    // Removed FlashSlot coroutine

    private void UpdateDiscardText()
    {
        discardCounterText.text = discardCount.ToString();
    }
}
