using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using UnityEngine;

namespace GoogleSheetsToUnity.Editor
{
    public struct BuildAction
    {
        public Delegate m_Action;
        public object[] m_Params;
    }

    public class GoogleSheetsToUnityEditorWindow : EditorWindow
    {
        private static readonly string exportFolder = "GSTU_Export";

        private readonly string _folderPath = $"Assets/{exportFolder}";
        private readonly string _sheetSettingAsset = "SheetSetting";
        private readonly string _gstuAPIsConfig = "GSTU_Config";
        private readonly string _resourcePath = $"Assets/{exportFolder}/Resources";

        private GoogleSheetsToUnityConfig config;
        private SheetSetting _sheetSetting;
        private Queue<BuildAction> _queue;
        private bool showSecret = false;
        private Vector2 scrollPosition;
        private bool expandAll;
        private float process = 0;
        private bool isBuildText;

        private bool isBuilding;
        private static string _output;
        private bool showTextTemplate;
        private bool isUpperText;
        private const string StarCheckKey = "<key=";
        private const string EndCheckKey = "/>";

        [MenuItem("3Q-Tool/Google Sheet To Unity")]
        public static void Open()
        {
            var win = GetWindow<GoogleSheetsToUnityEditorWindow>("GSTU Build Connection");
            ServicePointManager.ServerCertificateValidationCallback = Validator;

            win.Init();
        }

        private static bool Validator(object in_sender, X509Certificate in_certificate, X509Chain in_chain, SslPolicyErrors in_sslPolicyErrors)
        {
            return true;
        }

        public void Init()
        {
            config = (GoogleSheetsToUnityConfig) Resources.Load(_gstuAPIsConfig);
            var finds = AssetDatabase.FindAssets($"t:{_sheetSettingAsset}", null);
            foreach (var item in finds)
            {
                var path = AssetDatabase.GUIDToAssetPath(item);
                _sheetSetting = AssetDatabase.LoadAssetAtPath<SheetSetting>(path);
            }

            if (_queue == null)
            {
                _queue = new Queue<BuildAction>();
            }

            isBuilding = false;
        }

        private void OnEnable()
        {
            if (_queue == null)
            {
                _queue = new Queue<BuildAction>();
            }
        }

        public void AddQueue(Delegate method, object[] param)
        {
            if (_queue == null)
            {
                _queue = new Queue<BuildAction>();
            }

            _queue.Enqueue(new BuildAction {m_Action = method, m_Params = param});
        }

        public void Execute()
        {
            if (_queue != null && _queue.Count > 0)
            {
                var method = _queue.Dequeue();
                method.m_Action.DynamicInvoke(method.m_Params);
            }
        }

        public void OnGUI()
        {
            if (isBuilding)
            {
                GUILayout.Label("Exporting data", EditorStyles.boldLabel);
            }
            else
            {
                BuildConnection();

                DrawLocalization();
            }
        }

        private void DrawLocalization()
        {
            if (_sheetSetting == null)
            {
                GUILayout.Label("Create GSTU Setting", EditorStyles.boldLabel);
                GUI.backgroundColor = Color.red;
                if (!GUILayout.Button("Create", GUILayout.Height(30))) return;

                if (AssetDatabase.IsValidFolder(_folderPath))
                {
                    _sheetSetting = CreateInstance<SheetSetting>();
                    AssetDatabase.CreateAsset(_sheetSetting, $"{_folderPath}/{_sheetSettingAsset}.asset");

                    config = CreateInstance<GoogleSheetsToUnityConfig>();
                    AssetDatabase.CreateAsset(config, $"{_resourcePath}/{_gstuAPIsConfig}.asset");
                }
                else
                {
                    //create export folder
                    AssetDatabase.CreateFolder("Assets", exportFolder);

                    //create export resource folder
                    AssetDatabase.CreateFolder(_folderPath, "Resources");

                    //create export scripts folder
                    AssetDatabase.CreateFolder(_folderPath, "Scripts");

                    _sheetSetting = CreateInstance<SheetSetting>();
                    _sheetSetting.GoogleSheets = new List<SheetConfig> {new SheetConfig()};
                    AssetDatabase.CreateAsset(_sheetSetting, $"{_folderPath}/{_sheetSettingAsset}.asset");

                    config = CreateInstance<GoogleSheetsToUnityConfig>();
                    AssetDatabase.CreateAsset(config, $"{_resourcePath}/{_gstuAPIsConfig}.asset");
                }

                AssetDatabase.SaveAssets();
            }
            else if (!showTextTemplate)
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                GUILayout.Label("");
                GUILayout.Label("Google Sheet Config", EditorStyles.boldLabel);
                for (int i = 0; i < _sheetSetting.GoogleSheets.Count; i++)
                {
                    DrawSheetConfig(i + 1, _sheetSetting.GoogleSheets[i]);
                    GUI.backgroundColor = Color.white;
                }

                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Add Google Sheet"))
                {
                    _sheetSetting.GoogleSheets.Add(new SheetConfig());
                }

                if (GUILayout.Button("Expand"))
                {
                    expandAll = !expandAll;
                    for (int i = 0; i < _sheetSetting.GoogleSheets.Count; i++)
                    {
                        _sheetSetting.GoogleSheets[i].isExpand = expandAll;
                    }
                }

                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Save Asset"))
                {
                    EditorUtility.SetDirty(config);
                    EditorUtility.SetDirty(_sheetSetting);
                    AssetDatabase.SaveAssets();
                }

                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.Label("");
                GUILayout.Label("Download & Build all data", EditorStyles.boldLabel);
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Build all", GUILayout.Height(30)))
                {
                    OnImportClicked();
                }

                GUILayout.Label("", GUILayout.Height(60));
            }
            else if (showTextTemplate)
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                GUILayout.Label("");
                GUILayout.Label("Build Text Template", EditorStyles.boldLabel);
                _sheetSetting.textTemplate = GUILayout.TextArea(_sheetSetting.textTemplate);

                GUILayout.EndScrollView();
            }
        }

        private void BuildConnection()
        {
            if (config == null)
            {
                Debug.LogError("Error: no config file");
                return;
            }

            GUILayout.Label("API Config Setting", EditorStyles.boldLabel);

            config.CLIENT_ID = EditorGUILayout.TextField("Client ID", config.CLIENT_ID);

            GUILayout.BeginHorizontal();
            config.CLIENT_SECRET = showSecret ? EditorGUILayout.TextField("Client Secret Code", config.CLIENT_SECRET) : EditorGUILayout.PasswordField("Client Secret Code", config.CLIENT_SECRET);

            showSecret = GUILayout.Toggle(showSecret, "Show");
            GUILayout.EndHorizontal();

            config.PORT = EditorGUILayout.IntField("Port number", config.PORT);

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Build Connection", GUILayout.Height(30)))
            {
                GoogleAuthrisationHelper.BuildHttpListener();
            }

            GUI.backgroundColor = Color.green;
            var style = new GUIStyle(GUI.skin.toggle);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 15;
            showTextTemplate = GUILayout.Toggle(showTextTemplate, "Build Text Template");

            GUI.backgroundColor = Color.white;

            EditorUtility.SetDirty(config);
        }

        private void DrawSheetConfig(int index, SheetConfig sheetConfig)
        {
            GUI.backgroundColor = sheetConfig.isExpand ? index % 2 == 0 ? Color.cyan : Color.green : Color.white;
            var style = new GUIStyle(GUI.skin.toggle);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 15;
            sheetConfig.isExpand = GUILayout.Toggle(sheetConfig.isExpand, $"[{index}] {sheetConfig.findName}", style);
            GUI.backgroundColor = Color.white;
            if (sheetConfig.isExpand)
            {
                GUILayout.Label("File name", EditorStyles.helpBox);
                sheetConfig.findName = EditorGUILayout.TextField("File name", sheetConfig.findName);

                GUILayout.Label("Sheet Config Setting", EditorStyles.helpBox);
                sheetConfig.spreadSheetKey = EditorGUILayout.TextField("Spread sheet key", sheetConfig.spreadSheetKey);

                GUILayout.Label("Sheet Names:", EditorStyles.helpBox);

                int removeId = -1;
                for (int i = 0; i < sheetConfig.sheetNames.Count; i++)
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label($"Sheet {i}:", GUILayout.Width(60));
                    sheetConfig.sheetNames[i].name = EditorGUILayout.TextField(sheetConfig.sheetNames[i].name);

                    GUILayout.Label("Start Cell:", GUILayout.Width(65));
                    sheetConfig.sheetNames[i].startCell = EditorGUILayout.TextField(sheetConfig.sheetNames[i].startCell, GUILayout.Width(70));

                    GUILayout.Label("End Cell:", GUILayout.Width(65));
                    sheetConfig.sheetNames[i].endCell = EditorGUILayout.TextField(sheetConfig.sheetNames[i].endCell, GUILayout.Width(70));

                    GUILayout.Label("Build text:", GUILayout.Width(60));
                    sheetConfig.sheetNames[i].buildText = GUILayout.Toggle(sheetConfig.sheetNames[i].buildText, "", GUILayout.Width(20));

                    GUILayout.Label("Upper text:", GUILayout.Width(60));
                    sheetConfig.sheetNames[i].isUpper = GUILayout.Toggle(sheetConfig.sheetNames[i].isUpper, "", GUILayout.Width(20));

                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Build", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        ExportSheet(sheetConfig.spreadSheetKey, sheetConfig.sheetNames[i].name, sheetConfig.sheetNames[i].startCell, sheetConfig.sheetNames[i].endCell,
                            sheetConfig.sheetNames[i].buildText, sheetConfig.sheetNames[i].isUpper);
                    }

                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        if (EditorUtility.DisplayDialog("Note!", $"You will remove sheet {sheetConfig.sheetNames[i].name}. Are you sure?", "Oke", "No"))
                        {
                            removeId = i;
                        }
                    }

                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                }

                if (removeId >= 0) sheetConfig.sheetNames.RemoveAt(removeId);
                GUILayout.Label(sheetConfig.sheetNames.Count <= 0 ? "Download all sheets" : $"Download {sheetConfig.sheetNames.Count} sheets");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add sheet name", GUILayout.Width(150)))
                {
                    sheetConfig.sheetNames.Add(new SheetName {name = "", buildText = false});
                }

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Note!", $"You will remove google sheet. Are you sure?", "Oke", "No"))
                    {
                        _sheetSetting.GoogleSheets.Remove(sheetConfig);
                    }
                }

                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("");
        }

        float GetProcess()
        {
            return process;
        }

        void OnImportClicked()
        {
            _queue = new Queue<BuildAction>();
            if (!AssetDatabase.IsValidFolder(_folderPath + "/Resources"))
            {
                AssetDatabase.CreateFolder(_folderPath, "Resources");
            }

            if (!AssetDatabase.IsValidFolder(_folderPath + "/Resources/LocalizedText"))
            {
                AssetDatabase.CreateFolder(_folderPath + "/Resources", "LocalizedText");
            }

            if (!AssetDatabase.IsValidFolder(_folderPath + "/Resources/DesignJsonData"))
            {
                AssetDatabase.CreateFolder(_folderPath + "/Resources", "DesignJsonData");
            }

            if (!AssetDatabase.IsValidFolder(_folderPath + "/Scripts"))
            {
                AssetDatabase.CreateFolder(_folderPath, "Scripts");
            }

            EditorUtility.ClearProgressBar();

            foreach (var ggSheet in _sheetSetting.GoogleSheets)
            {
                foreach (var sheet in ggSheet.sheetNames)
                {
                    var sheetName = sheet.name;
                    var startCell = sheet.startCell;
                    var endCell = sheet.endCell;
                    var buildText = sheet.buildText;
                    var isUpper = sheet.isUpper;

                    AddQueue(new Action<string, string, string, string, bool, bool>(ExportSheet), new object[] {ggSheet.spreadSheetKey, sheetName, startCell, endCell, buildText, isUpper});
                }
            }

            Execute();
        }

        private void ExportSheet(string sheetId, string sheetName, string startCell, string endCell, bool buildText, bool isUpper)
        {
            isBuilding = true;
            isBuildText = buildText;
            isUpperText = isUpper;
            Debug.Log($"sheetName: {sheetName}");
            EditorUtility.DisplayProgressBar("Reading From Google Sheet ", $"Sheet: {sheetName}", GetProcess());
            var gstuSearch = new GSTU_Search(sheetId, sheetName, startCell, endCell);
            SpreadsheetManager.Read(gstuSearch, ReadSheetCallback);
        }

        void ReadSheetCallback(GstuSpreadSheet sheet)
        {
            EditorUtility.ClearProgressBar();
            isBuilding = false;
            process = 0;
            if (sheet != null)
            {
                Debug.Log($"Total Row Count: {sheet.rows.primaryDictionary.Count}");
                if (isBuildText)
                {
                    BuildText(sheet, isUpperText, OnCompleteRead);
                }
                else
                {
                    ExportData(sheet, OnCompleteRead);
                }
            }
        }

        private void OnCompleteRead()
        {
            EditorUtility.ClearProgressBar();
            Execute();
        }

        struct DataType
        {
            public string _type;
            public string _name;

            public DataType(string n, string t)
            {
                _name = n;
                _type = t;
            }
        }

        private void ExportData(GstuSpreadSheet sheet, Action complete)
        {
            var sheetName = sheet.sheetName;
            var jsonPath = $"{Application.dataPath}/{exportFolder}/Resources/DesignJsonData/{sheetName}.json";
            var jsonString = "";

            bool isBuildKey = false;
            var dataTypes = new List<DataType>();
            var listBuildKeys = new Dictionary<string, List<object>>();
            //var listBuildArr = new Dictionary<string, List<List<object>>>();
            var listKeys = new List<string>();

            foreach (var key in sheet.columns.secondaryKeyLink.Keys)
            {
                var listValue = sheet.columns.GetValueFromSecondary(key);

                if (key.ToLower().Contains("[key]") || key.ToLower().Contains("[arr]"))
                {
                    isBuildKey = key.ToLower().Contains("[key]");
                    foreach (var text in listValue)
                    {
                        var kk = text.value.Trim();
                        kk = kk.Replace(" ", "");
                        if (!kk.Equals(key))
                        {
                            listKeys.Add(kk);
                            if (!listBuildKeys.ContainsKey(kk))
                            {
                                listBuildKeys.Add(kk, new List<object>());
                            }
                        }
                    }
                }
                else
                {
                    var slip = key.Split(':');
                    var typeName = slip[0];
                    var type = slip[1];
                    dataTypes.Add(new DataType(typeName, type));

                    var countKey = 0;
                    foreach (var text in listValue)
                    {
                        if (!text.value.Equals(key))
                        {
                            var value = text.value;
                            var keyData = listKeys[countKey];
                            countKey++;
                            if (listBuildKeys.TryGetValue(keyData, out var listValues))
                            {
                                listValues.Add(value);
                            }
                        }
                    }
                }
            }

            jsonString = "{\n";
            if (isBuildKey)
            {
                foreach (var key in listBuildKeys)
                {
                    jsonString += "\t\"" + key.Key + "\": {\n";
                    for (var i = 0; i < dataTypes.Count; i++)
                    {
                        var dataType = dataTypes[i];
                        var txtValue = "";
                        switch (dataType._type.ToLower())
                        {
                            case "str":
                            case "string":
                                txtValue = "\"" + key.Value[i] + "\"";
                                break;
                            default:
                                txtValue = key.Value[i].ToString();
                                break;
                        }

                        jsonString += "\t\t\"" + dataType._name + "\": " + txtValue + ",\n";
                    }

                    jsonString = jsonString.Substring(0, jsonString.Length - 2);
                    jsonString += "\n\t},\n";
                }
            }
            else
            {
                foreach (var key in listBuildKeys)
                {
                    jsonString += "\t\"" + key.Key + "\": [\n";

                    var numRow = key.Value.Count / dataTypes.Count;
                    for (int r = 0; r < numRow; r++)
                    {
                        jsonString += "\t\t{\n";
                        for (var c = 0; c < dataTypes.Count; c++)
                        {
                            var dataType = dataTypes[c];
                            var txtValue = "";
                            switch (dataType._type.ToLower())
                            {
                                case "str":
                                case "string":
                                    txtValue = "\"" + key.Value[c * numRow + r] + "\"";
                                    break;
                                default:
                                    txtValue = key.Value[c * numRow + r].ToString();
                                    break;
                            }

                            jsonString += "\t\t\t\"" + dataType._name + "\": " + txtValue + ",\n";
                        }

                        jsonString = jsonString.Substring(0, jsonString.Length - 2);
                        jsonString += "\n\t\t},\n";
                    }

                    jsonString = jsonString.Substring(0, jsonString.Length - 2);
                    jsonString += "\n\t],\n";
                }
            }

            jsonString = jsonString.Substring(0, jsonString.Length - 2);
            jsonString += "\n}";
            File.WriteAllText(jsonPath, jsonString);
            complete?.Invoke();
        }

        private void BuildText(GstuSpreadSheet sheet, bool isUpper, Action complete)
        {
            var sheetName = sheet.sheetName;
            var listKeys = new List<string>();
            var listBuildKeys = new Dictionary<string, List<string>>();
            var dataTypes = new List<string>();
            var listLanguage = "";
            var ListKey = "";
            foreach (var key in sheet.columns.secondaryKeyLink.Keys)
            {
                if (!key.StartsWith("[") && !key.EndsWith("]"))
                {
                    var listValue = sheet.columns.GetValueFromSecondary(key);
                    if (key.ToLower().Contains("key"))
                    {
                        foreach (var text in listValue)
                        {
                            var kk = text.value.Trim();
                            kk = kk.Replace(" ", "");
                            if (!kk.Equals(key))
                            {
                                listKeys.Add(kk);
                                if (!listBuildKeys.ContainsKey(kk))
                                {
                                    listBuildKeys.Add(kk, new List<string>());
                                }
                            }
                        }
                    }
                    else
                    {
                        listLanguage += "case \"" + key + "\":\n\t\t\t\t";
                        listLanguage += "\tfileName = \"[" + key + "]" + sheetName + "\";\n\t\t\t\t";
                        listLanguage += "\tbreak;\n\n\t\t\t\t";

                        dataTypes.Add(key);
                        var countKey = 0;
                        foreach (var text in listValue)
                        {
                            if (!text.value.Equals(key))
                            {
                                var value = TrimFormatRickText(text.value);
                                var keyData = listKeys[countKey];
                                countKey++;
                                if (listBuildKeys.TryGetValue(keyData, out var listValues))
                                {
                                    listValues.Add(value);
                                }
                            }
                        }
                    }
                }
            }

            //Export class
            var classPath = $"{Application.dataPath}/{exportFolder}/Scripts/{sheetName}Container.cs";
            var classContent = ClassContent(sheetName);

            if (AssetDatabase.IsValidFolder($"{_folderPath}/Resources"))
            {
                if (!AssetDatabase.IsValidFolder($"{_folderPath}/Resources/LocalizedText"))
                {
                    AssetDatabase.CreateFolder($"{_folderPath}/Resources", "LocalizedText");      
                }
            }

            for (var i = 0; i < dataTypes.Count; i++)
            {
                var dataType = dataTypes[i];

                var jsonPath = $"{Application.dataPath}/{exportFolder}/Resources/LocalizedText/[{dataType}]{sheetName}.json";
                var jsonString = "{\n" + "\t\"" + dataType + "\": [\n";

                var textValue = "";
                foreach (var key in listBuildKeys)
                {
                    if (i < key.Value.Count)
                    {
                        var textBuilder = GetStringFormatKey(key.Value[i], listBuildKeys, i);
                        if (isUpper)
                        {
                            textBuilder = textBuilder.ToUpper();
                        }
                        textBuilder = TrimFormatRickText(textBuilder);
                        
                        textBuilder = ReplaceText(textBuilder, isUpper);
                        textValue += "\t\t\"" + textBuilder + "\",\n";
                    }
                }

                jsonString += textValue;
                jsonString += "\t]" + "\n}";
                File.WriteAllText(jsonPath, jsonString);
            }

            var count = 0;
            foreach (var key in listBuildKeys)
            {
                ListKey += key.Key + ",\t\t\t//" + key.Value[0] + "\n\t\t";
                process = count / (float) listBuildKeys.Count;
                EditorUtility.DisplayProgressBar("Reading From Google Sheet ", $"Sheet: {sheetName}/{key.Key} - {count}/{listBuildKeys.Count}", GetProcess());
            }

            ListKey = ListKey.Substring(0, ListKey.LastIndexOf("\n", StringComparison.Ordinal));
            listLanguage = listLanguage.Substring(0, listLanguage.LastIndexOf("\n\n", StringComparison.Ordinal));

            classContent = classContent.Replace("<ListKey>", ListKey);
            classContent = classContent.Replace("<ListLanguage>", listLanguage);
            File.WriteAllText(classPath, classContent);
            complete?.Invoke();
        }

        private string ReplaceText(string textValue, bool isUpper)
        {
            var text = textValue;
            if (isUpper)
            {
                text = text.Replace("<COLOR=", "<color=");
                text = text.Replace("< COLOR=", "<color=");
                text = text.Replace("<COLOR =", "<color=");
                text = text.Replace("< COLOR =", "<color=");
                text = text.Replace("</COLOR>", "</color>");
                text = text.Replace("< /COLOR>", "</color>");
                text = text.Replace("</ COLOR>", "</color>");
                text = text.Replace("</COLOR >", "</color>");
                text = text.Replace("< /COLOR >", "</color>");
                text = text.Replace("</ COLOR >", "</color>");

                text = text.Replace("<SIZE=", "<size=");
                text = text.Replace("< SIZE=", "<size=");
                text = text.Replace("<SIZE =", "<size=");
                text = text.Replace("< SIZE =", "<size=");
                text = text.Replace("</SIZE>", "</size>");
                text = text.Replace("< /SIZE>", "</size>");
                text = text.Replace("</ SIZE>", "</size>");
                text = text.Replace("</SIZE >", "</size>");
                text = text.Replace("< /SIZE >", "</size>");
                text = text.Replace("</ SIZE >", "</size>");

                text = text.Replace("<KEY=", "<key=");
                text = text.Replace("<KEY =", "<key=");
                text = text.Replace("< KEY =", "<key=");

                text = text.Replace("\\ N", "\\n");
                text = text.Replace("\\N", "\\n");
            }
            else
            {
                text = text.Replace("<color =", "<color=");
                text = text.Replace("< color =", "<color=");
                text = text.Replace("<Color =", "<color=");
                text = text.Replace("< Color =", "<color=");
                text = text.Replace("</ color>", "</color>");
                text = text.Replace("</ Color>", "</color>");

                text = text.Replace("<size =", "<size=");
                text = text.Replace("< size =", "<size=");
                text = text.Replace("<Size =", "<size=");
                text = text.Replace("< Size =", "<size=");
                text = text.Replace("</ size>", "</size>");
                text = text.Replace("</ Size>", "</size>");

                text = text.Replace("<key =", "<key=");
                text = text.Replace("<Key =", "<key=");
                text = text.Replace("< key =", "<key=");
                text = text.Replace("< Key =", "<key=");

                text = text.Replace("\\ N", "\\n");
                text = text.Replace("\\ n", "\\n");
            }

            return text;
        }

        string ClassContent(string sheetName)
        {
            var content = _sheetSetting.textTemplate;
            content = content.Replace("<SheetName>", sheetName);
            return content;
        }

        private static string GetStringFormatKey(string input, Dictionary<string, List<string>> dictionary, int index)
        {
            _output = input;
            if (_output.Contains(StarCheckKey) && _output.Contains(EndCheckKey))
            {
                try
                {
                    _output = "";
                    var start = input.Substring(0, input.IndexOf(StarCheckKey, StringComparison.Ordinal));
                    var end = input.Substring(input.IndexOf(StarCheckKey, StringComparison.Ordinal) + StarCheckKey.Length);
                    var key = end.Substring(0, end.IndexOf(EndCheckKey, StringComparison.Ordinal)).Trim();

                    var final = end.Substring(end.IndexOf(EndCheckKey, StringComparison.Ordinal) + EndCheckKey.Length);
                    if (final.Contains(StarCheckKey) && final.Contains(EndCheckKey))
                    {
                        final = GetStringFormatKey(final, dictionary, index);
                    }

                    _output = start + GetText(dictionary, key, index) + final;
                }
                catch (Exception e)
                {
                    Debug.LogError($"<qnt> GetStringFormatKey: {e}");
                }
            }

            return _output;
        }

        private static string TrimFormatRickText(string input)
        {
            _output = input;
            var starCheckKey = "<";
            var endCheckKey = ">";
            if (_output.Contains(starCheckKey) && _output.Contains(endCheckKey))
            {
                try
                {
                    _output = "";
                    var start = input.Substring(0, input.IndexOf(starCheckKey, StringComparison.Ordinal) + starCheckKey.Length);
                    var end = input.Substring(input.IndexOf(starCheckKey, StringComparison.Ordinal) + starCheckKey.Length);
                    var content = end.Substring(0, end.IndexOf(endCheckKey, StringComparison.Ordinal)).Trim();
                    content = content.Replace(" ", "").ToLower();

                    var final = end.Substring(end.IndexOf(endCheckKey, StringComparison.Ordinal));
                    if (final.Contains(starCheckKey) && final.Contains(endCheckKey))
                    {
                        final = TrimFormatRickText(final);
                    }

                    _output = start + content + final;
                }
                catch (Exception e)
                {
                    Debug.LogError($"<qnt> GetStringFormatKey: {e}");
                }
            }

            return _output;
        }

        static string GetText(Dictionary<string, List<string>> dictionary, string key, int index)
        {
            if (dictionary.TryGetValue(key, out var list))
            {
                if (index < list.Count)
                {
                    return list[index];
                }
            }

            return key;
        }
    }
}