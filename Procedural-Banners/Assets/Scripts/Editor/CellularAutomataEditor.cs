using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CellularAutomata))]
public class CellularAutomataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CellularAutomata cell = FindObjectOfType<CellularAutomata>();

        if (GUILayout.Button("Wow"))
        {
            cell.CreateBanner();
        }
    }
}
