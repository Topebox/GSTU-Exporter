using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SheetSetting", menuName = "3Q/Sheet Setting", order = 1)]
public class SheetSetting : ScriptableObject
{
    /*public string spreadSheetKey = "";
    public string startCell = "A1";
    public string endCell = "Z100";
    public List<string> SheetNames = new List<string>();*/

    public List<SheetConfig> GoogleSheets;
}

[System.Serializable]
public class SheetConfig
{
    public string findName = "";
    public string spreadSheetKey = "";
    [HideInInspector] public bool isExpand = false;
    public List<SheetName> sheetNames = new List<SheetName>();
}

[System.Serializable]
public class SheetName
{
    public string startCell = "A1";
    public string endCell = "Z100";
    public bool buildText;
    public string name;
}