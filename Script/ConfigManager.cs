using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using LitJson;
using Bean;
using System.Text.RegularExpressions;

public class ConfigManager:Singleton<ConfigManager>
{

    private Dictionary<string, object> _configs = new Dictionary<string, object>();

    private Dictionary<string, Dictionary<string, FileInfo>> _fileInfo;

    /// <summary>
    /// 读取配置
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="folder">配置分类</param>
    /// <param name="name">配置名</param>
    /// <returns>配置字典</returns>
    public Dictionary<int, T> GetConfig<T>(string folder, string name) where T : ConfigBaseData
    {
        object obj = null;
        string path = folder + "_" + name;
        _configs.TryGetValue(path, out obj);
        if (obj != null)
        {
            Debug.Log("读取配置缓存【" + name + "】");
            return (Dictionary<int, T>)obj;
        }
        else
        {
            string config = FileUtils.ReadFile(Application.dataPath + "/Configs/" + (folder != null ? folder + "/" : "") + name + ".json");

            config = Regex.Replace(config, "(\\|).*(?=\")", "");

            Debug.Log("配置读取路径 - " + Application.dataPath + "/Configs/" + (folder != null ? folder + "/" : "") + name + ".json");
            Debug.Log("读取内容  = " + config);

            if (!string.IsNullOrEmpty(config))
            {
                T[] json = JsonMapper.ToObject<T[]>(config);
                obj = json.ToDictionary(key => key.id, value => value);
                _configs.Add(path, obj);
                return (Dictionary<int, T>)obj;
            }
            else
            {
                Debug.LogWarning("不存在配置【" + name + "】");
                return null;
            }
        }
    }

    /// <summary>
    /// 清除目标缓存
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="name"></param>
    public void Clear(string folder, string name)
    {
        string path = folder + "_" + name;
        if (_configs.ContainsKey(path))
        {
            _configs.Remove(path);
        }
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void ClearAll()
    {
        _configs.Clear();
    }
}


