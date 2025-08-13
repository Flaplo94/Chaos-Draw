using UnityEngine;
public class DebugTimeScale : MonoBehaviour
{
    void Start() { Debug.Log("[TimeScale at Start] " + Time.timeScale); }
    void Update() { if (Input.GetKeyDown(KeyCode.T)) Debug.Log("[TimeScale now] " + Time.timeScale); }

    public void Open()
    {
        Debug.Log("[Shop] Open called");
        // ...
    }
    public void Close()
    {
        Debug.Log("[Shop] Close called");
        // ...
    }

}
