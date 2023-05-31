using UnityEngine;

[CreateAssetMenu(fileName = "Plant", menuName = "Vegetation/Plant", order = 0)] 
public class Plant : ScriptableObject
{
    public GameObject prefab;
    public float maxHeight;
    public float minHeight;
}
