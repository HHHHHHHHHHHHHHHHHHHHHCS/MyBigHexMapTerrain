using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 地形数据的编辑面板显示
/// </summary>
[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexcoordinatesDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        HexCoordinates hexCoordinates = new HexCoordinates(
            property.FindPropertyRelative("x").intValue,
            property.FindPropertyRelative("y").intValue,
            property.FindPropertyRelative("z").intValue
            );
        position = EditorGUI.PrefixLabel(position, label);
        GUI.Label(position, hexCoordinates.ToString());
    }
}
