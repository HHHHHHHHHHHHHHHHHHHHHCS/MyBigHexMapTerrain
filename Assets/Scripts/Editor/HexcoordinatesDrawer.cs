using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 编辑面板,地形数据的显示
/// </summary>
[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexcoordinatesDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        HexCoordinates hexCoordinates = new HexCoordinates(
            property.FindPropertyRelative("x").intValue,
            property.FindPropertyRelative("z").intValue);
        position = EditorGUI.PrefixLabel(position, label);
        GUI.Label(position, hexCoordinates.ToString());
    }
}
