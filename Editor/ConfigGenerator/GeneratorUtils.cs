using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.IO;
using System.Text;

public class GeneratorUtils
{
    /// <summary>
    /// 生成辅助类
    /// </summary>
    /// <param name="json"></param>
    /// <param name="className"></param>
    /// <param name="list"></param>
    /// <param name="dic"></param>
    /// <returns></returns>
    public static string GenerateSubClass(JsonData json, string className, List<string> list, Dictionary<string, bool> dic)
    {
        string strListClass = "\t\tpublic class " + className + " {\r\n";
        Debug.Log("json数据 ===== " + json.ToJson());
        IDictionary classProps = json.GetJsonType().Equals(JsonType.Array) ? json[0] : json;
        foreach (var classKey in classProps.Keys)
        {
            string propName = classKey.ToString();
            //JsonData child = json[0][propName];
            //strListClass += "\t\tpublic " + GetType(type) + " " + propName + ";\r\n";
            JsonData child = (JsonData)classProps[classKey];
            JsonType type = child.GetJsonType();
            if (type.Equals(JsonType.Array))
            {
                string[] propSplit = propName.Split('|');
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
                    string strListClassName = GeneratorUtils.UpperCaseFirstChar(propName) + "Data";
                    if (!dic.ContainsKey(strListClassName))
                    {
                        GenerateSubClass(child, strListClassName, list, dic);
                        dic.Add(strListClassName, true);
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

                    strListClass += "\t\t\tpublic " + strList + " " + propName + ";\r\n";
                }
                else
                {
                    //非对象，该属性为纯数组
                    string strList = "";
                    string strListEnd = "";
                    for (int i = 0; i < loop; i++)
                    {
                        strList += "List<";
                        strListEnd += ">";
                        if (i == loop - 1)
                        {
                            strList += GetType(tempType) + strListEnd;
                        }
                    }

                    strListClass += "\t\t\tpublic " + strList + " " + propName + ";\r\n";
                }

            }
            else if (type.Equals(JsonType.Object))
            {
                string strListClassName = GeneratorUtils.UpperCaseFirstChar(propName) + "Data";
                if (!dic.ContainsKey(strListClassName))
                {
                    GenerateSubClass(child, strListClassName, list, dic);
                }
                strListClass += "\t\t\tpublic " + strListClassName + " " + propName + ";\r\n";
            }
            else
            {
                string[] propSplit = propName.Split('|');
                Debug.Log("分离:" + propSplit.Length);
                if (propSplit.Length > 1)
                {
                    strListClass += "\t\t\tpublic " + propSplit[1] + " " + propSplit[0] + ";\r\n";
                }
                else
                {
                    strListClass += "\t\t\tpublic " + GetType(type) + " " + propName + ";\r\n";
                }
            }
        }

        strListClass += "\t\t}\r\n";
        list.Insert(2, strListClass);

        return strListClass;
    }

    public static string GetType(JsonType name)
    {
        switch (name)
        {
            case JsonType.Int:
                return "int";
            case JsonType.Boolean:
                return "bool";
            case JsonType.String:
                return "string";
            case JsonType.Double:
                return "double";
            case JsonType.Long:
                return "long";
        }
        return "";
    }

    /// <summary>
    /// 首字母大写
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string UpperCaseFirstChar(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }
        char[] a = s.ToCharArray();
        a[0] = char.ToUpper(a[0]);
        return new string(a);
    }

    /// <summary>
    /// 创建文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="name"></param>
    /// <param name="info"></param>
    public static void WriteFile(string path, string content)
    {
        //if (File.Exists(path)) { 
        //}
        string folderPath = Path.GetDirectoryName(path);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        File.WriteAllText(path, content, Encoding.Default);
    }

    /// <summary>
    /// 读取文件
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string ReadFile(string path)
    {
        path = path.Replace("/", "\\");
        return File.ReadAllText(path);
    }
}
