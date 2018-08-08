using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(SpriteRenderer))]
public class TextureEditor : Editor
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
                OnClick();
            }
        }

    }

    private void OnClick()
    {
        SpriteRenderer src = target as SpriteRenderer;

        Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        //todo: Make it work with Texture2d
        

        //if (cellPos.x >= 0 && cellPos.y >= 0)
        //{
        //    // Open window to select pattern
        //    SelectPatternEditorWindow window = EditorWindow.GetWindow<SelectPatternEditorWindow>();
        //    window.Init(
        //        src.GetComponentInParent<PathOverlapController>().GetModel(), 
        //        new Vector2Int(cellPos.x, cellPos.y),
        //        src.sprite.texture
        //        );
        //}

        
    }

}
