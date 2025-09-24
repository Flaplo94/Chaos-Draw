using UnityEngine;
using TMPro;

public class MetaCurrencyUI : MonoBehaviour
{
    [Header("Refs (optional)")]
    public TextMeshProUGUI shardsText;
    public TextMeshProUGUI xpText;

    [Header("Format")]
    public string shardsFormat = "{0}";
    public string xpFormat = "{0}";

    void OnEnable()
    {
        MetaProgressionManager.OnShardsChanged += Refresh;
        MetaProgressionManager.OnXPChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        MetaProgressionManager.OnShardsChanged -= Refresh;
        MetaProgressionManager.OnXPChanged -= Refresh;
    }

    public void Refresh()
    {
        var meta = MetaProgressionManager.Instance;
        if (!meta) return;

        if (shardsText) shardsText.text = string.Format(shardsFormat, meta.GetShards());
        if (xpText) xpText.text = string.Format(xpFormat, meta.GetXP());
    }
}
