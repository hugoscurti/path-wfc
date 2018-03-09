using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


public class MapIteratorAttribute : PropertyAttribute
{
    public List<FileInfo> maps;
    public string[] filenames;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="relativePath"></param>
    public MapIteratorAttribute(string relativePath)
    {
        maps = FileUtils.GetData(Path.Combine(Application.dataPath, relativePath));
        filenames = maps.Select(FileUtils.GetNameWithoutExtension).ToArray();
    }
}