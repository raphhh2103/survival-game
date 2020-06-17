using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WorldGenerator mapGen = (WorldGenerator)target;

        if (DrawDefaultInspector())
        {
            if (mapGen.autoUpdate)
            {
                //mapGen.GenerateMapData(new Vector2(mapGen.gameObject.GetComponent<EndlessTerrain>().viewer.position.x, mapGen.gameObject.GetComponent<EndlessTerrain>().viewer.position.z));
            }
        }

        if (GUILayout.Button("generate"))
        {
            //mapGen.GenerateMapData();
        }

    }
}
