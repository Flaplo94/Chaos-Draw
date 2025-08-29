public interface IMovementLock
{
    void LockMovement(float seconds);
    bool IsMovementLocked();
}
