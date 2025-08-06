using UnityEngine;
using TMPro;

public class UIMessage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float displayTime = 2f;

    private float timer;

    private void Awake()
    {
        messageText.gameObject.SetActive(false);
    }

    public void ShowMessage(string msg)
    {
        messageText.text = msg;
        messageText.gameObject.SetActive(true);
        timer = displayTime;
    }

    private void Update()
    {
        if (messageText.gameObject.activeSelf)
        {
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
            {
                messageText.gameObject.SetActive(false);
            }
        }
    }
}
