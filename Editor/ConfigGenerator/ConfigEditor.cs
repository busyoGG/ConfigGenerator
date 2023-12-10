using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using LitJson;

public class ConfigEditor : EditorWindow
{
    /// <summary>
    /// 配置文件路径
    /// </summary>
    private string _jsonUrl;
    /// <summary>
    /// 配置类输出路径
    /// </summary>
    private string _outputUrl;

    private Dictionary<string, List<FileInfo>> _allConfigs = new Dictionary<string, List<FileInfo>>();

    private int _configsNum = 0;

    private bool _foldConfigs = true;

    private Vector2 _scrollRoot;

    ConfigEditor()
    {
        titleContent = new GUIContent("配置编辑器");
    }

    [MenuItem("PreUtils/EditorEditor")]
    static public void ShowEditor()
    {
        // Debug.Log("启动配置编辑器");
        EditorWindow.GetWindow(typeof(ConfigEditor));
    }

    private void OnEnable()
    {
        _jsonUrl = Application.dataPath + "/Configs/";
        _outputUrl = Application.dataPath + "/Script/Loader/Config/";
        //获取所有配置
        GetAllConfigs();
        //监听配置文件夹变化
        Debug.Log("启动文件夹监听");
        FileSystemWatcher watcher = new FileSystemWatcher();
        watcher.IncludeSubdirectories = true;
        watcher.Path = _jsonUrl;
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Filter = "*.json";
        FileSystemEventHandler changeHandle = new FileSystemEventHandler(OnJsonFileChanged);
        watcher.Changed += changeHandle;
        //watcher.Deleted += changeHandle;
        watcher.Created += changeHandle;
        watcher.EnableRaisingEvents = true;
        watcher.InternalBufferSize = 10240;
    }

    public void OnGUI()
    {
        GUILayout.Space(10);

        //获取所有配置
        _foldConfigs = EditorGUILayout.BeginFoldoutHeaderGroup(_foldConfigs, "所有配置");

        if (_foldConfigs)
        {
            GUILayout.Label("总配置数量：" + _configsNum);

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            _scrollRoot = EditorGUILayout.BeginScrollView(_scrollRoot, GUILayout.Height(400), GUILayout.Width(position.width - 20));

            GUIStyle configButtonStyle = new GUIStyle(GUI.skin.button);
            configButtonStyle.fixedHeight = 22;
            configButtonStyle.fontSize = 16;
            configButtonStyle.alignment = TextAnchor.MiddleLeft;

            GUIStyle configLabelStyle = new GUIStyle(GUI.skin.label);
            configLabelStyle.fontSize = 18;
            configLabelStyle.alignment = TextAnchor.MiddleLeft;
            configLabelStyle.fixedHeight = 20;

            foreach (var fileInfo in _allConfigs)
            {
                EditorGUILayout.LabelField(fileInfo.Key + "【" + fileInfo.Value.Count + "个配置文件】:", configLabelStyle);
                GUILayout.Space(5);
                for (int i = 0, len = fileInfo.Value.Count; i < len; i++)
                {
                    FileInfo file = fileInfo.Value[i];
                    //EditorGUILayout.LabelField("测试");
                    GUIContent name = new GUIContent((i + 1) + "." + file.Name.Replace(".json", ""));
                    if (GUILayout.Button(name, configButtonStyle))
                    {
                        //EditorWindow.GetWindow(typeof(ConfigDetailEditor));
                    }
                    GUILayout.Space(2);
                }
                GUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        GUILayout.Space(10);

        if (GUILayout.Button("一键生成所有配置文件数据类"))
        {
            GenerateDataClass();
            GenerateConfigPath();
        }
    }

    /// <summary>
    /// 创建数据类
    /// </summary>
    private void GenerateDataClass()
    {
        //生成基类
        string ns = "Bean";
        string baseClass = "namespace " + ns + "\r\n{\r\n" +
            "\tpublic class ConfigBaseData\r\n" +
            "\t{\r\n" +
            "\t\tpublic int id;\r\n" +
            "\t}\r\n}";
        string output = _outputUrl + "ConfigBaseData.cs";
        GeneratorUtils.WriteFile(output, baseClass);

        //创建所有数据类
        foreach (var fileInfo in _allConfigs)
        {
            //生成数据类
            for (int i = 0, len = fileInfo.Value.Count; i < len; i++)
            {
                FileInfo file = fileInfo.Value[i];
                string content = GeneratorUtils.ReadFile(file.FullName);
                JsonData jsonData = JsonMapper.ToObject(content);
                GenerateClass(GeneratorUtils.UpperCaseFirstChar(file.Name.Split('.')[0]) + "Data", jsonData, fileInfo.Key);
            }
        }
    }

    /// <summary>
    /// 创建类
    /// </summary>
    /// <param name="clsName"></param>
    /// <param name="json"></param>
    /// <param name="folder"></param>
    private void GenerateClass(string clsName, JsonData json, string folder)
    {
        List<string> listClass = new List<string>();

        List<string> listProps = new List<string>();

        string ns = "Bean";
        //创建命名空间
        listClass.Add("using System;\r\n" +
            "using System.Collections.Generic;\r\n" +
            "namespace " + ns + "{\r\n");

        //创建类头
        listClass.Add("\tpublic class " + clsName +
            ": ConfigBaseData, ICloneable{\r\n");

        //创建构造函数
        string strConstructor = "\t\tpublic " + clsName + "(){\r\n";

        //遍历属性
        IDictionary props = json[0];
        foreach (var key in props.Keys)
        {
            Dictionary<string, bool> dicClass = new Dictionary<string, bool>();
            string propName = key.ToString();
            if (propName == "id") continue;

            JsonData child = json[0][propName];
            object type = child == null ? JsonType.None : child.GetJsonType();

            if (type.Equals(JsonType.None))
            {
                listClass.Add("\t\tpublic " + clsName + " " + propName + ";\r\n");
            }
            else if (type.Equals(JsonType.Array))
            {
                //属性为数组
                if (child.Count == 0)
                {
                    //最终类型非对象，该属性为纯数组
                    string strList = "";
                    string strListEnd = "";

                    strList += "List<";
                    strListEnd += ">";

                    string[] propSplit = propName.Split('|');
                    string strType;

                    if (propSplit.Length > 1)
                    {
                        //strList += propSplit[1] + strListEnd;
                        strType = propSplit[1];
                    }
                    else
                    {
                        strType = clsName;
                    }
                    strList += strType + strListEnd;

                    Debug.Log("list数组 ==== " + strList);

                    //cls += "\t\tpublic " + strList + " " + propName + ";\r\n";
                    listClass.Add("\t\tpublic " + strList + " " + propSplit[0] + ";\r\n");

                    string strConstructorChild = "\t\t\t" + propSplit[0] + " = new List<" + strType + ">();\r\n";
                    strConstructor += strConstructorChild;
                }
                else
                {
                    //获取最终类型
                    JsonData tempData = child[0];
                    JsonType tempType = tempData.GetJsonType();
                    int loop = 1;
                    while (tempData.GetJsonType().Equals(JsonType.Array))
                    {
                        tempData = tempData[0];
                        tempType = tempData.GetJsonType();
                        loop++;
                    }

                    if (tempType.Equals(JsonType.Object))
                    {
                        //最终类型为对象
                        //创建附属类
                        string strListClassName = GeneratorUtils.UpperCaseFirstChar(propName) + "Data";
                        if (!dicClass.ContainsKey(strListClassName))
                        {
                            GeneratorUtils.GenerateSubClass(child, strListClassName, listClass, dicClass);
                            dicClass.Add(strListClassName, true);
                        }

                        //创建附属类列表
                        string strList = "";
                        string strListEnd = "";
                        for (int i = 0; i < loop; i++)
                        {
                            strList += "List<";
                            strListEnd += ">";
                            if (i == loop - 1)
                            {
                                strList += strListClassName + strListEnd;
                            }
                        }

                        //strListClass += "\t\tpublic " + strList + " " + propName + ";\r\n";
                        listClass.Add("\t\tpublic " + strList + " " + propName + ";\r\n");

                        string strConstructorChild = "\t\t\t" + propName + " = new List<" + strListClassName + ">();\r\n";
                        strConstructor += strConstructorChild;
                    }
                    else
                    {
                        //最终类型非对象，该属性为纯数组
                        string strList = "";
                        string strListEnd = "";
                        for (int i = 0; i < loop; i++)
                        {
                            strList += "List<";
                            strListEnd += ">";
                            if (i == loop - 1)
                            {
                                strList += GeneratorUtils.GetType(tempType) + strListEnd;
                            }
                        }
                        Debug.Log("list数组 ==== " + strList);

                        //cls += "\t\tpublic " + strList + " " + propName + ";\r\n";
                        listClass.Add("\t\tpublic " + strList + " " + propName + ";\r\n");

                        string strConstructorChild = "\t\t\t" + propName + " = new List<" + GeneratorUtils.GetType(tempType) + ">();\r\n";
                        strConstructor += strConstructorChild;
                    }
                }
            }
            else if (type.Equals(JsonType.Object))
            {
                //属性为对象
                string[] propSplits = propName.Split("|");
                if (propSplits.Length > 1)
                {
                    listClass.Add("\t\tpublic Dictionary<" + propSplits[1] + "," + propSplits[2] + "> " + propSplits[0] + ";\r\n");

                    string strConstructorChild = "\t\t\t" + propSplits[0] + " = new Dictionary<" + propSplits[1] + "," + propSplits[2] + ">();\r\n";
                    strConstructor += strConstructorChild;
                }
                else
                {
                    //创建附属类
                    string strListClassName = GeneratorUtils.UpperCaseFirstChar(propName) + "Data";
                    if (!dicClass.ContainsKey(strListClassName))
                    {
                        GeneratorUtils.GenerateSubClass(child, strListClassName, listClass, dicClass);
                        dicClass.Add(strListClassName, true);
                    }
                    listClass.Add("\t\tpublic " + strListClassName + " " + propName + ";\r\n");
                }
            }
            else
            {
                //属性为基本类型
                //Debug.Log("属性名" + propName + "," + type + "," + json[0][propName]);
                //cls += "\t\tpublic " + GetType(type) + " " + propName + ";\r\n";

                if (type == null)
                {
                    listClass.Add("\t\tpublic " + clsName + " " + propName + ";\r\n");
                }
                else
                {
                    string[] propSplit = propName.Split('|');
                    if (propSplit.Length > 1)
                    {
                        listClass.Add("\t\tpublic " + propSplit[1] + " " + propSplit[0] + ";\r\n");
                    }
                    else
                    {
                        listClass.Add("\t\tpublic " + GeneratorUtils.GetType((JsonType)type) + " " + propName + ";\r\n");
                    }
                }
            }

            listProps.Add(propName);
        }

        strConstructor += "\t\t}\r\n";

        listClass.Add(strConstructor);

        string strGenerator = "";
        strGenerator += "\t\tpublic " + clsName + "(" + clsName + " obj){\r\n";
        for (int i = 0, len = listProps.Count; i < len; i++)
        {
            string[] propSplit = listProps[i].Split('|');
            if (propSplit.Length > 1)
            {
                strGenerator += "\t\t\t" + propSplit[0] + " = obj." + propSplit[0] + ";\r\n";
            }
            else
            {
                strGenerator += "\t\t\t" + listProps[i] + " = obj." + listProps[i] + ";\r\n";
            }
        }
        strGenerator += "\t\t}\r\n";
        listClass.Add(strGenerator);

        //创建克隆函数
        listClass.Add("\t\tpublic object Clone()\r\n" +
            "\t\t{\r\n" +
            "\t\t\treturn new " + clsName + "(this);\r\n" +
            "\t\t}\r\n");

        //补充文件结构
        listClass.Add("\t}" +
            "\r\n}");

        //拼装数据类内容
        string cls = "";
        for (int i = 0, len = listClass.Count; i < len; i++)
        {
            cls += listClass[i];
        }

        //保存数据类
        Debug.Log("生成类" + cls);
        string strFolder;
        if (folder == "Configs")
        {
            strFolder = "";
        }
        else
        {
            strFolder = folder + "/";
        }
        string output = _outputUrl + "Bean/" + strFolder + clsName + ".cs";
        GeneratorUtils.WriteFile(output, cls);
    }

    private void GenerateConfigPath()
    {
        //string ns = "Bean";
        string cls = "namespace Bean{\r\n" +
            "\tpublic class ConfigsFolderConfig\r\n" +
            "\t{\r\n";

        string folders = "\t\tpublic const string Null = null;\r\n";
        string names = "";
        //配置名字典，防止重名配置重复添加
        Dictionary<string, bool> dicNames = new Dictionary<string, bool>();
        //创建所有数据类
        foreach (var fileInfo in _allConfigs)
        {
            if (fileInfo.Key != "Configs")
            {
                folders += "\t\tpublic const string " + fileInfo.Key + " = \"" + fileInfo.Key + "\";\r\n";
            }
            //生成数据类
            for (int i = 0, len = fileInfo.Value.Count; i < len; i++)
            {
                FileInfo file = fileInfo.Value[i];
                string fileName = file.Name.Replace(".json", "");
                if (!dicNames.ContainsKey(fileName))
                {
                    names += "\t\tpublic const string " + GeneratorUtils.UpperCaseFirstChar(fileName) + " = \"" + fileName + "\";\r\n";
                    dicNames.Add(fileName, true);
                }
            }
        }

        cls += folders;

        cls += "\t}\r\n\r\n" +
            "\tpublic class ConfigsNameConfig\r\n" +
            "\t{\r\n";

        cls += names;

        cls += "\t}\r\n" +
            "}";

        Debug.Log("生成配置表路径配置\n" + cls);
        GeneratorUtils.WriteFile(_outputUrl + "ConfigsPathConfig.cs", cls);
    }

    //-----自动函数-----

    /// <summary>
    /// 配置文件发生变化后的响应事件
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="args"></param>
    private void OnJsonFileChanged(object obj, FileSystemEventArgs args)
    {
        Debug.Log("配置文件发生变化，重载配置文件");
        _allConfigs.Clear();
        GetAllConfigs();
    }

    /// <summary>
    /// 获取所有配置
    /// </summary>
    private void GetAllConfigs()
    {
        Debug.Log("获取所有配置1:" + _jsonUrl);
        DirectoryInfo directoryInfo = new DirectoryInfo(_jsonUrl);
        FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        _configsNum = 0;

        for (int i = 0, len = files.Length; i < len; i++)
        {
            FileInfo file = files[i];
            if (file.Name.EndsWith(".meta")) continue;

            string[] folders = file.DirectoryName.Split('\\');
            string parent = folders[folders.Length - 1];
            //_allConfigs.Add(file);
            if (!_allConfigs.ContainsKey(parent))
            {
                _allConfigs.Add(parent, new List<FileInfo>());
            }
            _allConfigs[parent].Add(file);
            //ConsoleUtils.Log("添加路径" , (file.DirectoryName + "/"), _jsonUrl);
            _configsNum++;
        }
    }
}
