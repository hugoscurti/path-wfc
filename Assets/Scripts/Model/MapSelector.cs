using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[Serializable]
public class MapSelector
{
    public int SelectedMap = 0;
    public string FileName = null;

    public FileInfo GetFile()
    {
        return new FileInfo(FileName);
    }
}
