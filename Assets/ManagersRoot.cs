using UnityEngine;
public class ManagersRoot : MonoBehaviour
{
    static ManagersRoot inst;
    void Awake()
    {
        if (inst != null && inst != this) { Destroy(gameObject); return; }
        inst = this;
        DontDestroyOnLoad(gameObject); // hele Managers + børn overlever
    }
}
