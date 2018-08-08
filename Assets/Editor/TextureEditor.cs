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

            // This event is eaten by the editor if the active tool is the Move tool
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

        var hit = Physics2D.GetRayIntersection(worldRay);
        if (hit && hit.transform == src.transform)
        {
            Vector2Int gridPos = Vector2Int.FloorToInt(hit.transform.InverseTransformPoint(hit.point));

            // Open window to select pattern
            SelectPatternEditorWindow window = EditorWindow.GetWindow<SelectPatternEditorWindow>();
            window.Init(
                src.GetComponentInParent<PathOverlapController>().GetModel(),
                gridPos,
                src.sprite.texture
                );
        }
    }

}
