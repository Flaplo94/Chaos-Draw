using UnityEngine;

public static class InputShuffleSystem
{
    // key index 0..3 -> slot index 0..3
    static int[] map = { 0, 1, 2, 3 };
    static bool isShuffled = false;

    /// Returns which hand slot should be used when key index (0..3) is pressed.
    public static int Map(int keyIndex)
    {
        keyIndex = Mathf.Clamp(keyIndex, 0, 3);
        return map[keyIndex];
    }

    /// Make a random permutation with NO fixed points (derangement),
    /// so key 1 never triggers slot 1, etc.
    public static void ShuffleKeys()
    {
        // Start with identity
        map[0] = 0; map[1] = 1; map[2] = 2; map[3] = 3;

        // Fisher–Yates until we get a derangement (4 items => quick)
        int attempts = 0;
        do
        {
            for (int i = 0; i < 4; i++)
            {
                int r = Random.Range(i, 4);
                (map[i], map[r]) = (map[r], map[i]);
            }
            attempts++;
            // Ensure no map[i] == i
        } while ((map[0] == 0 || map[1] == 1 || map[2] == 2 || map[3] == 3) && attempts < 20);

        isShuffled = true;
        Debug.Log($"[BrainDamage] Key Slot map: 1{map[0] + 1}, 2{map[1] + 1}, 3{map[2] + 1}, 4{map[3] + 1}");
    }

    public static bool IsShuffled() => isShuffled;
}
