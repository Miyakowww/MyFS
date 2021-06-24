using System;
using System.Text;
using MyFS.API;
using MyFS.Core.Models;

namespace MyFS.Core
{
    public class File : IFile
    {
        internal readonly FileInfo file;

        internal Folder parent;

        internal File(FileInfo fileInfo, Folder parent)
        {
            this.parent = parent;
            file = fileInfo;

            Path = file.GetPath();
        }

        public string Name => file.Name;

        public DateTime DateModified => file.DateModified;

        public bool CanRead { get => file.CanRead; set { file.CanRead = value; file.Flush(); } }
        public bool CanWrite { get => file.CanWrite; set { file.CanWrite = value; file.Flush(); } }

        public long FileSize => file.FileSize;

        public string Path { get; private set; }

        public IFolder Parent => parent;

        public byte[]? ReadAllBytes()
        {
            if (!file.CanRead)
            {
                return null;
            }
            return file.ReadAllBytes();
        }
        public string? ReadAllText()
        {
            if (!file.CanRead)
            {
                return null;
            }
            return Encoding.UTF8.GetString(file.ReadAllBytes());
        }

        public bool AppendAllBytes(byte[] data)
        {
            if (!file.CanWrite)
            {
                return false;
            }
            return file.AppendAllBytes(data);
        }
        public bool AppendAllText(string text)
        {
            if (!file.CanWrite)
            {
                return false;
            }
            return file.AppendAllBytes(Encoding.UTF8.GetBytes(text));
        }
        public bool WriteAllBytes(byte[] data)
        {
            if (!file.CanWrite)
            {
                return false;
            }
            return file.WriteAllBytes(data);
        }
        public bool WriteAllText(string text)
        {
            if (!file.CanWrite)
            {
                return false;
            }
            return file.WriteAllBytes(Encoding.UTF8.GetBytes(text));
        }

        public void Rename(string newName)
        {
            file.Name = newName;
            file.Flush();
        }

        public bool Delete()
        {
            return parent.DeleteFile(Name);
        }
    }
}
