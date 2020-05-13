using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SheetSetting", menuName = "3Q/Sheet Setting", order = 1)]
public class SheetSetting : ScriptableObject
{
    public List<SheetConfig> GoogleSheets;

    [HideInInspector]
    public string textTemplate = "" +
        "/* * * * * *\n" +
        " * Author: QuocNT\n" +
        " * Email: ntq.quoc@gmail.com\n" +
        "* * * * * */\n\n" +
        "using System;\n" +
        "using QNT.SimpleJSON;\n" +
        "using UnityEngine;\n\n" +
        "namespace QNT.GSTU.Extension\n" +
        "{\n" +
        "    public enum <SheetName>Key\n" +
        "    {\n" +
        "        <ListKey>\n" +
        "    }\n\n" +
        "    public static class <SheetName>Containe" +
        "" +
      
        "    {\n" +
        "        private static string[] _listText;\n" +
        "        private static int _maxLength;\n\n" +
        "        public static void LoadText(string language)\n" +
        "        {\n" +
        "            var fileName = \"\";\n" +
        "            switch (language)\n" +
        "            {\n" +
        "                <ListLanguage>\n" +
        "            }\n\n" +
        "            try\n" +
        "            {\n" +
        "                var jsonString = (Resources.Load($\"LocalizedText/{fileName}\") as TextAsset)?.text;\n" +
        "                var nodes = JSON.Parse(jsonString);\n" +
        "                var texts = (JSONArray) nodes[language];\n" +
        "                _maxLength = texts.Count;\n" +
        "                _listText = new string[_maxLength];\n\n" +
        "                var i = 0;\n" +
        "                foreach (var text in texts)\n" +
        "                {\n" +
        "                    _listText[i] = text.Value;\n" +
        "                    i++;\n" +
        "                }\n" +
        "            }\n" +
        "            catch (Exception e)\n" +
        "            {\n" +
        "                Debug.LogError($\"<qnt> <SheetName> can not load json file: {fileName} => err: {e}\");n" +
        "            }\n" +
        "        }\n\n" +
        "        public static string GetText(<SheetName>Key key)\n" +
        "        {\n" +
        "            try\n" +
        "            {\n" +
        "                if ((int) key < _maxLength)\n" +
        "                {\n" +
        "                    return _listText[(int) key];\n" +
        "                }\n" +
        "            }\n" +
        "            catch (Exception e)\n" +
        "            {\n" +
        "                Debug.LogError($\"<qnt> <SheetName> not exits key: {key} => err: {e}\");\n" +
        "            }\n\n" +
        "            return key.ToString();\n" +
        "        }\n" +
        "    }\n" +
        "}";
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