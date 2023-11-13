using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
        
        if (meshCollider == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("No reference to meshCollider was found. Colliders have not generated");
            #endif
        }
        else
            meshCollider.sharedMesh = meshData.CreateMesh();
    }
}