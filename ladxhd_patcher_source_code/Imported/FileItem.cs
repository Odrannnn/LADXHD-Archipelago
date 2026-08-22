using System;
using System.IO;

namespace LADXHD_Patcher
{
    /*===================================================================================================================================
     * Powershell has a useful function "Get-Item" which is very similar to "FileInfo" and "DirectoryInfo" in C#. Unfortunately, FileInfo 
     * does not have the "BaseName" property of a file as readily available as PowerShell's Get-Item. It also over complicates things by
     * needing to distinguish between "FileInfo" and "DirectoryInfo". This class "FileItem" seeks to remedy these issues.
     *---------------------------------------------------------------------------------------------------------------------------------*/

    public class FileItem
    {
        // Mark these as nullable because they might not exist depending on the path.
        public DirectoryInfo? Directory;
        public DirectoryInfo? Parent;
        public DirectoryInfo? Root;
    
        public FileAttributes Attributes;
        public string BaseName = "";
        public string DirectoryName = "";
        public bool Exists = false;
        public string Extension = "";
        public string FullName = "";
        public bool IsReadOnly = false;
        public DateTime LastAccessTime;
        public DateTime LastAccessTimeUtc;
        public DateTime LastWriteTime;
        public DateTime LastWriteTimeUtc;
        public long Length = 0;
        public string Name = "";

        public FileItem(string InputFile)
        {
            // Use the common base class 'FileSystemInfo'
            FileSystemInfo info;

            // Directory
            if (InputFile.TestPath(true))
            {
                var dirInfo = new DirectoryInfo(InputFile);
                info = dirInfo;
            
                this.Name = dirInfo.Name;
                this.BaseName = dirInfo.Name;
                this.DirectoryName = dirInfo.FullName;
                this.Parent = dirInfo.Parent;
                this.Root = dirInfo.Root;
            }
            // File (or non-existent)
            else
            {
                var fileInfo = new FileInfo(InputFile);
                info = fileInfo;
            
                this.Name = fileInfo.Name;
                this.BaseName = Path.GetFileNameWithoutExtension(this.Name);
                this.Directory = fileInfo.Directory;
                this.DirectoryName = fileInfo.DirectoryName ?? "";
                this.IsReadOnly = fileInfo.IsReadOnly;
                this.Length = fileInfo.Exists ? fileInfo.Length : 0;
            }

            // Common properties available on FileSystemInfo base class.
            this.Attributes = info.Attributes;
            this.Exists = info.Exists;
            this.Extension = info.Extension;
            this.FullName = info.FullName;
            this.LastAccessTime = info.LastAccessTime;
            this.LastAccessTimeUtc = info.LastAccessTimeUtc;
            this.LastWriteTime = info.LastWriteTime;
            this.LastWriteTimeUtc = info.LastWriteTimeUtc;
        }

        public bool IsInFolder(string folderName)
        {
            // If in a folder by a specific name return true.
            for (var dir = Directory; dir != null; dir = dir.Parent)
                if (dir.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase))
                    return true;

            // If not return false.
            return false;
        }
    }
}
