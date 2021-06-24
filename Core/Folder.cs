using System;
using System.Linq;
using MyFS.API;
using MyFS.Core.Models;

namespace MyFS.Core
{
    public class Folder : IFolder
    {
        internal readonly FolderInfo folder;

        internal Folder? parent;

        internal Folder(FolderInfo folderInfo, Folder? parent)
        {
            this.parent = parent;
            folder = folderInfo;

            Path = folder.GetPath();
        }
        public string Name => folder.Name;

        public DateTime DateModified => folder.DateModified;

        public bool CanRead { get => folder.CanRead; set { folder.CanRead = value; folder.Flush(); } }
        public bool CanWrite { get => folder.CanWrite; set { folder.CanWrite = value; folder.Flush(); } }

        public string Path { get; private set; }

        public IFolder? Parent => parent;

        public bool IsRoot => parent is null;
        public bool IsEmpty => folder.IsEmpty;

        public bool ContainsFile(string name)
        {
            return folder.GetAllFiles().Any(f => f.Name == name);
        }
        public bool ContainsFolder(string name)
        {
            return folder.GetAllFolders().Any(f => f.Name == name);
        }

        public IFile? CreateFile(string name)
        {
            if (!folder.CanWrite)
            {
                return null;
            }
            FileInfo? file = folder.CreateFile(name);
            return file is null ? null : new File(file, this);
        }
        public IFolder? CreateSubFolder(string name)
        {
            if (!folder.CanWrite)
            {
                return null;
            }
            FolderInfo? subFolder = folder.CreateFolder(name);
            return subFolder is null ? null : new Folder(subFolder, this);
        }

        public IFile? GetFile(string name)
        {
            if (!folder.CanRead)
            {
                return null;
            }
            FileInfo? info = folder.GetAllFiles().FirstOrDefault(i => i.Name == name);
            return info is null ? null : new File(info, this);
        }
        public IFolder? GetFolder(string name)
        {
            if (!folder.CanRead)
            {
                return null;
            }
            FolderInfo? info = folder.GetAllFolders().FirstOrDefault(i => i.Name == name);
            return info is null ? null : new Folder(info, this);
        }
        public IFile[]? GetAllFiles()
        {
            if (!folder.CanRead)
            {
                return null;
            }
            return folder.GetAllFiles().Select(i => new File(i, this)).ToArray();
        }
        public IFolder[]? GetAllFolders()
        {
            if (!folder.CanRead)
            {
                return null;
            }
            return folder.GetAllFolders().Select(i => new Folder(i, this)).ToArray();
        }

        public bool DeleteFile(string name)
        {
            FileInfo? info = folder.GetAllFiles().FirstOrDefault(i => i.Name == name);
            if (info is null || !info.CanWrite)
            {
                return false;
            }
            folder.Remove(info.PageId);
            info.Free();
            return true;
        }
        public bool DeleteSubFolder(string name)
        {
            FolderInfo? info = folder.GetAllFolders().FirstOrDefault(i => i.Name == name);
            if (info is null || !info.CanWrite)
            {
                return false;
            }
            folder.Remove(info.PageId);
            info.Free();
            return true;
        }

        public void Rename(string newName)
        {
            folder.Name = newName;
            folder.Flush();
        }

        public bool Delete()
        {
            return parent?.DeleteSubFolder(Name) ?? false;
        }
    }
}
