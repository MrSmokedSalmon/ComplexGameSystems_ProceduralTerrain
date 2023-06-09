using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(GenerateAllInEditor))]
	public class GenerateAllInEditor_CE : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			
			GenerateAllInEditor gen = (GenerateAllInEditor)target;

			if (GUILayout.Button("Generate All"))
			{
				gen.GenerateAll();
			}
			
			if (GUILayout.Button("Generate Vegetation"))
			{
				gen.GenVegetation();
			}
			if (GUILayout.Button("Clear Vegetation"))
			{
				gen.ClearVegetation();
			}
            
		}
	}
}