using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;

/// <summary>
/// 在2018.3.0b6有BUG 不能创建
/// </summary>
public class TextureArrayWizard : ScriptableWizard
{
    public Texture2D[] textures;

    [MenuItem("Assets/Create/Texture Array")]
    private static void CreateWizard()
    {
        DisplayWizard<TextureArrayWizard>(
            "Create Texture Array", "Create");
    }

    private void OnWizardCreate()
    {
        if (textures.Length == 0)
        {
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Texture Array", "Texture Array"
            , "asset", "Save Texture Array"
        );
        if (path.Length == 0)
        {
            return;
        }

        Texture2D t = textures[0];
        Texture2DArray textureArray = new Texture2DArray(
            t.width, t.height, textures.Length
            , t.format, t.mipmapCount > 1, true)
        {
            anisoLevel = t.anisoLevel,
            filterMode = t.filterMode,
            wrapMode = t.wrapMode
        };

        for (int i = 0; i < textures.Length; i++)
        {
            for (int m = 0; m < t.mipmapCount; m++)
            {
                Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
            }
        }

        AssetDatabase.CreateAsset(textureArray, path);
    }
}