using System;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 扩充unity 一些方法方便测试
/// </summary>
public static class MyU
{
    #region Debug Log

    private static StringBuilder sb;
    private const string spiltStr = "   ";

    public static void Log(params object[] objArr)
    {
        if (sb == null)
        {
            sb = new StringBuilder();
        }

        for (int i = 0; i < objArr.Length; i++)
        {
            sb.Append(objArr[i] == null ? "Null" : objArr[i].ToString());
            if (i != objArr.Length - 1)
            {
                sb.Append(spiltStr);
            }
        }

        Debug.Log(sb);
        sb.Clear();
    }

    #endregion

    #region Get Component

    /*
     * 别用GetCom 寻找Gameobject 用 GetGo
     * GetGoG GetComG 是总Gameobject 为parent 开始find
     */

    private static Transform root;

    public static void BeginParent(Transform parent)
    {
        root = parent;
    }

    public static void BeginParent(Component parent)
    {
        root = parent.transform;
    }

    public static void BeginParent(GameObject parent)
    {
        root = parent.transform;
    }

    public static void EndParent()
    {
        root = null;
    }

    public static void GetGo(out GameObject obj, string path, Component parent)
    {
        GetGo(out obj, path, parent ? parent.transform : root);
    }

    public static void GetGo(out GameObject obj, string path, Transform parent = null)
    {
        parent = parent ?? root;
        obj = parent.Find(path).gameObject;
    }

    public static void GetGo(out GameObject obj, Transform parent = null)
    {
        parent = parent ?? root;
        obj = parent.gameObject;
    }

    public static void GetGoG(out GameObject obj, string path)
    {
        obj = GameObject.Find(path);
    }

    public static void GetCom<T>(out T obj, string path, Component parent)
    {
        GetCom(out obj, path, parent ? parent.transform : root);
    }

    public static void GetCom<T>(out T obj, string path, Transform parent = null)
    {
        parent = parent ?? root;
        obj = parent.Find(path).GetComponent<T>();
    }

    public static void GetCom<T>(out T obj, Transform parent = null)
    {
        parent = parent ?? root;
        obj = parent.GetComponent<T>();
    }

    public static void GetComG<T>(out T obj, string path)
    {
        obj = GameObject.Find(path).GetComponent<T>();
    }

    #endregion

    #region Add Button Click

    public static void AddClick(Transform btn, UnityAction act)
    {
        AddClick(btn.GetComponent<Button>(), act);
    }

    public static void AddClick(Button btn, UnityAction act)
    {
        btn.onClick.AddListener(act);
    }

    public static void AddClick<T>(Transform btn, Action<T> act, T arg)
    {
        AddClick(btn.GetComponent<Button>(), act, arg);
    }

    public static void AddClick<T>(Button btn, Action<T> act, T arg)
    {
        btn.onClick.AddListener(() => act(arg));
    }

    #endregion

    #region Add Slider Toggle ValueChange

    public static void AddValChange(Slider slider, UnityAction<float> val)
    {
        slider.onValueChanged.AddListener(val);
    }

    public static void AddValChange(Toggle toggle, UnityAction<bool> val)
    {
        toggle.onValueChanged.AddListener(val);
    }

    #endregion
}