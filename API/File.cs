using System;

namespace MyFS.API
{
    public interface IFile
    {
        public string Name { get; }
        public DateTime DateModified { get; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public long FileSize { get; }
        public string Path { get; }
        public IFolder? Parent { get; }

        public byte[]? ReadAllBytes();
        public string? ReadAllText();
        public bool WriteAllBytes(byte[] data);
        public bool WriteAllText(string text);
        public bool AppendAllBytes(byte[] data);
        public bool AppendAllText(string text);
        public void Rename(string newName);
        public bool Delete();
    }
}
