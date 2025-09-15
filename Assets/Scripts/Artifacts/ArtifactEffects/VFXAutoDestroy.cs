using UnityEngine;

public class VFXAutoDestroy : MonoBehaviour
{
    // Call this from the last frame of your Explosion animation via an Animation Event.
    public void OnImpactFinished()
    {
        Destroy(gameObject);
    }
}
