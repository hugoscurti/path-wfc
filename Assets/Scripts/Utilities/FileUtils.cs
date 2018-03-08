using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class FileUtils
{

    /// <summary>
    /// Get .png and .map data files from one of the folders
    /// Absolute folder is recommended
    /// </summary>
    public static List<FileInfo> GetData(string folder)
    {
        DirectoryInfo folderinfo = new DirectoryInfo(folder);

        var fileinfos = folderinfo.GetFiles("*.png").Union(folderinfo.GetFiles("*.map"));

        return fileinfos.ToList();
    }

    public static string GetNameWithoutExtension(FileInfo fi)
    {
        return fi.Name.Remove(fi.Name.IndexOf(fi.Extension), fi.Extension.Length);
    }
}