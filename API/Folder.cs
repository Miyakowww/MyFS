using System;

namespace MyFS.API
{
    public interface IFolder
    {
        public string Name { get; }
        public DateTime DateModified { get; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public string Path { get; }
        public IFolder? Parent { get; }
        public bool IsRoot { get; }
        public bool IsEmpty { get; }

        public IFolder? CreateSubFolder(string name);
        public IFile? CreateFile(string name);
        public bool ContainsFolder(string name);
        public bool ContainsFile(string name);
        public IFile? GetFile(string name);
        public IFolder? GetFolder(string name);
        public IFile[]? GetAllFiles();
        public IFolder[]? GetAllFolders();
        public void Rename(string newName);
        public bool DeleteSubFolder(string name);
        public bool DeleteFile(string name);

        public bool Delete();
    }
}
