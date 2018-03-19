using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(Tilemap))]
public class TilemapEditor : Editor
{
    bool mouseDown;

    private void OnSceneGUI()
    {
        // Simulate click without dragging
        if (Event.current.button == 0)
        {
            if (Event.current.type == EventType.MouseDown)
                mouseDown = true;

            if (Event.current.type == EventType.MouseDrag)
                // Cancel the click
                mouseDown = false;

            if (Event.current.type == EventType.MouseUp && mouseDown)
            {
                //Debug.Log("Mouse clicked!");
                OnClick();
            }
        }

    }

    private void OnClick()
    {
        Tilemap src = target as Tilemap;

        Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        Vector3Int cellPos = src.WorldToCell(worldRay.origin);

        if (cellPos.x >= 0 && cellPos.y >= 0)
        {
            // Select Corresponding tile
            //Debug.Log($"Cell {cellPos.x},{cellPos.y}");

            // Open window to select pattern
            SelectPatternEditorWindow window = EditorWindow.GetWindow<SelectPatternEditorWindow>();
            window.Init(src, src.GetComponentInParent<PathOverlapController>().GetModel(), new Vector2Int(cellPos.x, cellPos.y));
        }

        
    }

}
