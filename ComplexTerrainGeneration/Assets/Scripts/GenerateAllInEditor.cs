using System.Collections.Generic;

using UnityEngine;

public class GenerateAllInEditor : MonoBehaviour
{
	[SerializeField] private HeightMapGen[] editorChunks;

	public void GenerateAll()
	{
		foreach(HeightMapGen heightMapGen in editorChunks)
		{
			heightMapGen.DrawMapInEditor();
		}
	}
}
