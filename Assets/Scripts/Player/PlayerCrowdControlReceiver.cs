using UnityEngine;

public class PlayerCrowdControlReceiver : MonoBehaviour, IMovementLock
{
    private bool locked;
    private float timer;

    public void LockMovement(float seconds)
    {
        locked = true;
        timer = Mathf.Max(timer, seconds);
    }

    public bool IsMovementLocked() => locked;

    private void Update()
    {
        if (!locked) return;
        timer -= Time.unscaledDeltaTime;
        if (timer <= 0f)
        {
            locked = false;
            timer = 0f;
        }
    }
}
