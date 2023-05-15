using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(HeightMapGen))]
    public class HeightMapGen_CE : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            HeightMapGen gen = (HeightMapGen)target;
            if (DrawDefaultInspector())
                if (gen.autoUpdate)
                    gen.DrawMapInEditor();

            if (GUILayout.Button("Generate"))
            {
                gen.DrawMapInEditor();
            }
            
        }
    }
}