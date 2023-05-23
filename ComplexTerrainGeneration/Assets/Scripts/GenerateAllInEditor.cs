using System.Collections.Generic;

using UnityEngine;

public class GenerateAllInEditor : MonoBehaviour
{
	[SerializeField] private HeightMapGen[] editorChunks;
	[SerializeField] private VegetationSystem vegeSystem;
	
	public void GenerateAll()
	{
		foreach(HeightMapGen heightMapGen in editorChunks)
		{
			heightMapGen.DrawMapInEditor();
		}
	}

	public void GenVegetation()
	{
		foreach (HeightMapGen chunk in editorChunks)
		{
			Vector3 position = chunk.gameObject.transform.position;
			vegeSystem.Vegetate(chunk.chunkData.heightMap, chunk.heightMulti, position, 1f,800);
		}
	}

	public void ClearVegetation()
	{
		vegeSystem.Devegetate();
	}
}
