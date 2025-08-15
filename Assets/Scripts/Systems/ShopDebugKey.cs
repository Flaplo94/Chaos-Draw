using UnityEngine;

public class ShopDebugKey : MonoBehaviour
{
    public KeyCode openKey = KeyCode.O;
    public bool closeWithEscape = true;

    void Update()
    {
        if (Input.GetKeyDown(openKey)) ShopManager.Instance?.Open();
        if (closeWithEscape && Input.GetKeyDown(KeyCode.Escape)) ShopManager.Instance?.Close();
    }
}
