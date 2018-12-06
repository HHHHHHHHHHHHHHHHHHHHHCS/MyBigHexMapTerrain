using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadUI : MonoBehaviour
{
    public Color oddColor = Color.white, evenColor = Color.cyan;
    public Transform fileItemPrefab;

    private HexGrid hexGrid;
    private Transform saveLoadMap;
    private Transform content;
    private Scrollbar scrollBar;
    private InputField fileNameInput;
    private Text deleteBtnText;

    private bool isConfirmDelete;

    private bool IsConfirmDelete
    {
        get => isConfirmDelete;
        set
        {
            isConfirmDelete = value;
            deleteBtnText.text = value ? "Confirm!?" : "Delete";
        }
    }


    public void Init(HexGrid _hexGrid)
    {
        hexGrid = _hexGrid;

        MyU.GetCom(out Button saveLoadButton, "Button_SaveLoad", transform);
        MyU.GetCom(out saveLoadMap, "Bg_SaveLoadMAP", transform);
        MyU.BeginParent(saveLoadMap);
        MyU.GetCom(out Button updateButton, "Button_Update");
        MyU.GetCom(out Button closeButton, "Button_Close");
        MyU.GetCom(out Button saveButton, "Button_Save");
        MyU.GetCom(out Button loadButton, "Button_Load");
        MyU.GetCom(out Button deleteButton, "Button_Delete");
        MyU.GetCom(out deleteBtnText, "Text", deleteButton);
        MyU.GetCom(out fileNameInput, "Input_FileName");
        MyU.GetCom(out Transform scrollView, "ScrollView");
        MyU.GetCom(out content, "Viewport/Content", scrollView);
        MyU.GetCom(out scrollBar, "Scrollbar_Vertical", scrollView);

        MyU.AddClick(saveLoadButton, ShowHideMapBg, true);
        MyU.AddClick(closeButton, ShowHideMapBg, false);
        MyU.AddClick(updateButton, RefreshFiles);
        MyU.AddClick(saveButton, Save);
        MyU.AddClick(loadButton, Load);
        MyU.AddClick(deleteButton, Delete);
    }

    private void RefreshFiles()
    {
        IsConfirmDelete = false;

        foreach (Transform item in content)
        {
            Destroy(item.gameObject);
        }

        var files = SaveLoadModule.GetAllFile();
        for (var i = 0; i < files.Count; i++)
        {
            var index = i; //这里重写一个int  是因为下面有闭包
            var isOdd = (index & 1) == 1;
            var fileItem = Instantiate(fileItemPrefab, content);
            fileItem.GetComponent<Image>().color = isOdd ? oddColor : evenColor;

            fileItem.GetChild(0).GetComponent<Text>().text = files[index];

            MyU.AddClick(fileItem, () =>
            {
                var str = files[index];
                fileNameInput.text = str;
            });
        }
    }

    public void ShowHideMapBg(bool isShow)
    {
        saveLoadMap.gameObject.SetActive(isShow);
        HexMapCamera.Instance.Locked = isShow;
        if (isShow)
        {
            RefreshFiles();
        }
    }

    public void Save()
    {
        SaveLoadModule.Save(fileNameInput.text, hexGrid);
        RefreshFiles();
    }

    public void Load()
    {
        SaveLoadModule.Load(fileNameInput.text, hexGrid);
    }

    public void Delete()
    {
        if (IsConfirmDelete)
        {
            SaveLoadModule.Delete(fileNameInput.text);
            RefreshFiles();
        }
        else
        {
            IsConfirmDelete = true;
        }
    }
}