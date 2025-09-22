using UnityEngine;

public class ArtifactUIManager : MonoBehaviour
{
    public GameObject artifactIconPrefab;
    public Transform artifactBarParent;

    public void UpdateArtifactUI()
    {
        foreach (Transform child in artifactBarParent)
            Destroy(child.gameObject);

        var seen = new System.Collections.Generic.HashSet<string>();
        foreach (ArtifactData artifact in PlayerArtifactManager.Instance.ownedArtifacts)
        {
            if (artifact == null) continue;
            string id = Normalize(artifact.internalID);
            if (seen.Contains(id)) continue;
            seen.Add(id);

            GameObject iconGO = Instantiate(artifactIconPrefab, artifactBarParent);
            iconGO.GetComponent<ArtifactIcon>().Setup(artifact);
        }

        string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().ToLowerInvariant();
            s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
            return s;
        }
    }
}
