using UnityEngine;

public class ArtifactUIManager : MonoBehaviour
{
    public GameObject artifactIconPrefab;
    public Transform artifactBarParent;

    public void UpdateArtifactUI()
    {
        foreach (Transform child in artifactBarParent)
            Destroy(child.gameObject);

        foreach (ArtifactData artifact in PlayerArtifactManager.Instance.ownedArtifacts)
        {
            GameObject iconGO = Instantiate(artifactIconPrefab, artifactBarParent);
            iconGO.GetComponent<ArtifactIcon>().Setup(artifact);
        }
    }
}
