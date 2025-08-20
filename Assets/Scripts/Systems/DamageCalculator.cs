using UnityEngine;

public static class DamageCalculator
{
    public static int ComputeFinalDamage(float baseDamage, DamageElement element)
    {
        float mult = 1f;

        if (PlayerBuffManager.Instance != null)
        {
            // Generisk dmg inkl. Adaptive Power
            mult *= PlayerBuffManager.Instance.GetGenericDamageMult();

            // Element-multiplikator
            switch (element)
            {
                case DamageElement.Fire:
                    mult *= PlayerBuffManager.Instance.GetFireDamageMult();
                    break;
                case DamageElement.Lightning:
                    mult *= PlayerBuffManager.Instance.GetLightningDamageMult();
                    break;
                case DamageElement.Burn:
                    mult *= PlayerBuffManager.Instance.GetBurnDamageMult();
                    break;
            }
        }

        float final = baseDamage * mult;
        int intFinal = Mathf.Max(0, Mathf.RoundToInt(final));
        return intFinal;
    }
}
