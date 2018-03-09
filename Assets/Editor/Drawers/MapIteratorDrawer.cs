using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(MapIteratorAttribute))]
public class MapIteratorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var mapiter = (MapIteratorAttribute)attribute;

        var selectedMap = property.FindPropertyRelative("SelectedMap");
        selectedMap.intValue = EditorGUI.Popup(position, label.text, selectedMap.intValue, mapiter.filenames);

        var filename = property.FindPropertyRelative("FileName");
        filename.stringValue = mapiter.maps[selectedMap.intValue].FullName;
    }
}
