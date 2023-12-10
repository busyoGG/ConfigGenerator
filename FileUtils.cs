using System.IO;
using System.Text;

public class FileUtils
{
    // Start is called before the first frame update
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
