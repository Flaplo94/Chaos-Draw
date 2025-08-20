using UnityEngine;

[DisallowMultipleComponent]
public class EnemyBounty : MonoBehaviour
{
    [Min(0)] public int baseGold = 1;

    [Tooltip("Ekstra procent pr. wave. 0.1 = +10% pr. wave")]
    public float perWaveScale = 0f;

    public int GetBounty(int currentWave)
    {
        if (currentWave < 1) currentWave = 1;
        float scale = 1f + (currentWave - 1) * Mathf.Max(0f, perWaveScale);
        return Mathf.Max(0, Mathf.RoundToInt(baseGold * scale));
    }
}
