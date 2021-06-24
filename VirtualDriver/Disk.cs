using System.IO;
using System.Linq;
using MyFS.HIAL;

namespace MyFS.VirtualDriver
{
    public class Disk : IDisk
    {
        private FileStream? stream;

        public bool Loaded => stream != null;

        public static Disk LoadDisk(string path)
        {
            FileStream fileStream = File.Open(path, FileMode.OpenOrCreate);
            Disk disk = new();
            disk.stream = fileStream;
            return disk;
        }

        public byte? ReadByte(ushort pageId, byte offset)
            => ReadPageData(pageId)?[offset];

        public bool WriteByte(ushort pageId, byte offset, byte data)
        {
            IPage? page = ReadPage(pageId);
            if (page is null)
            {
                return false;
            }
            page.WriteByte(offset, data);
            page.Flush();
            return true;
        }

        public IPage? ReadPage(ushort pageId)
        {
            if (stream is null)
            {
                return null;
            }
            return new Page(this, pageId);
        }
        public IPageData? ReadPageData(ushort pageId)
        {
            if (stream is null)
            {
                return null;
            }
            if (stream!.Length < pageId * 256 + 256)
            {
                return new PageData();
            }
            byte[] array = new byte[256];
            stream!.Seek(pageId * 256, SeekOrigin.Begin);
            stream!.Read(array, 0, 256);
            return new PageData(array);
        }

        public bool WritePage(ushort pageId, IPageData page)
        {
            if (stream is null)
            {
                return false;
            }
            stream!.Seek(pageId * 256, SeekOrigin.Begin);
            stream!.Write(page.ToArray(), 0, 256);
            return true;
        }
        public bool WriteData(ushort pageId, byte[] data)
        {
            if (stream is null)
            {
                return false;
            }
            stream!.Seek(pageId * 256, SeekOrigin.Begin);
            stream!.Write(data, 0, data.Length);
            return true;
        }

        public void Unload()
        {
            stream?.Flush();
            stream?.Close();
            stream?.Dispose();
            stream = null;
        }
    }
}