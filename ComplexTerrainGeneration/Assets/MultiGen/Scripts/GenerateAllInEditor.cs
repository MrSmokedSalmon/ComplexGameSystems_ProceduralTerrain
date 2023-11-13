using System.Collections.Generic;

using UnityEngine;

public class GenerateAllInEditor : MonoBehaviour
{
	[SerializeField] private HeightMapGen[] editorChunks;
	[SerializeField] private VegetationSystem vegeSystem;
	[SerializeField] private bool spawnVegetation;

	public void GenerateAll()
	{
		if (editorChunks == null || editorChunks.Length == 0)
			editorChunks = FindObjectsOfType<HeightMapGen>();

		foreach(HeightMapGen heightMapGen in editorChunks)
		{
			heightMapGen.DrawMapInEditor();
		}
	}

	public void GenVegetation()
	{
		if (vegeSystem == null)
			vegeSystem = FindObjectOfType<VegetationSystem>();
		
		foreach (HeightMapGen chunk in editorChunks)
		{
			vegeSystem.Vegetate(chunk);
			//if (spawnVegetation)
				
		}
	}

	public void ClearVegetation()
	{
		if (vegeSystem == null)
			vegeSystem = FindObjectOfType<VegetationSystem>();
		
		vegeSystem.Devegetate();
	}
}
