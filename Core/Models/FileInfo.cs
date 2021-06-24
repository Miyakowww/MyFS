using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyFS.HIAL;

namespace MyFS.Core.Models
{
    internal class FileInfo
    {
        public string Name = string.Empty;
        public DateTime DateModified;
        public bool CanRead;
        public bool CanWrite;
        public int FileSize;
        public byte[] contents;
        public ushort NextPage;

        private readonly SystemInfo sys;
        private readonly IPage page;
        public FolderInfo parent;

        public ushort PageId => page.PageId;

        public FileInfo(SystemInfo sys, FolderInfo parent, IPage page)
        {
            this.sys = sys;
            this.page = page;
            this.parent = parent;
            Name = Encoding.ASCII.GetString(page[1..33]).Replace("\0", "");
            DateModified = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(page[33..41])).DateTime;
            byte protection = page[41];
            CanRead = protection.CheckByte(1);
            CanWrite = protection.CheckByte(0);
            FileSize = BitConverter.ToInt32(page[42..46]);
            contents = page[46..254];
            NextPage = BitConverter.ToUInt16(page[254..256]);
            if (page[0] == 0)
            {
                DateModified = DateTime.Now;
                CanRead = CanWrite = true;
                Flush();
            }
        }

        public FileContentInfo? GetNext()
        {
            return NextPage == 0 ? null : new(sys, sys.disk.ReadPage(NextPage) ?? throw new ObjectDisposedException("Disk is not loaded."));
        }

        public byte[] ReadAllBytes()
        {
            if (FileSize <= contents.Length)
            {
                return contents[..FileSize];
            }
            List<byte> data = new(contents);
            var next = GetNext();
            while (next is not null)
            {
                data.AddRange(next.contents);
                next = next.GetNext();
            }
            return data.ToArray();
        }
        public bool WriteAllBytes(byte[] data)
        {
            if (data.Length <= contents.Length)
            {
                Array.Fill<byte>(contents, 0);
                data.CopyTo(contents, 0);
                FileSize = data.Length;
                GetNext()?.Free();
                NextPage = 0;
                DateModified = DateTime.Now;
                Flush();
                return true;
            }

            FileContentInfo? next = GetNext();
            if (next is null)
            {
                IPage? newPage = sys.Allocate();
                if (newPage is null)
                {
                    return false;
                }
                if (!new FileContentInfo(sys, newPage).WriteAllBytes(data, contents.Length))
                {
                    sys.Free(newPage.PageId);
                    return false;
                }
                NextPage = newPage.PageId;
            }
            else if (!next.WriteAllBytes(data, contents.Length))
            {
                return false;
            }
            data[..contents.Length].CopyTo(contents, 0);
            FileSize = data.Length;
            DateModified = DateTime.Now;
            Flush();
            return true;
        }
        public bool AppendAllBytes(byte[] data)
        {
            if (data.Length + FileSize <= contents.Length)
            {
                data.CopyTo(contents, FileSize);
                FileSize += data.Length;
                DateModified = DateTime.Now;
                Flush();
                return true;
            }
            if (FileSize > contents.Length)
            {
                if (GetNext()!.AppendAllBytes(data, 0))
                {
                    FileSize += data.Length;
                    DateModified = DateTime.Now;
                    Flush();
                    return true;
                }
                return false;
            }

            IPage? newPage = sys.Allocate();
            if (newPage is null)
            {
                return false;
            }
            if (!new FileContentInfo(sys, newPage).AppendAllBytes(data, contents.Length - FileSize))
            {
                sys.Free(newPage.PageId);
                return false;
            }
            NextPage = newPage.PageId;
            data[..(contents.Length - FileSize)].CopyTo(contents, FileSize);
            FileSize += data.Length;
            DateModified = DateTime.Now;
            Flush();
            return true;
        }

        public string GetPath()
        {
            List<char> pathArr = new();
            pathArr.AddRange(Name.Reverse());
            FolderInfo? parent = this.parent;
            while (parent is not null)
            {
                pathArr.Add('/');
                pathArr.AddRange(parent.Name.Reverse());
                parent = parent.parent;
            }
            pathArr.Reverse();
            return new(pathArr.ToArray());
        }

        public void Flush()
        {
            byte[] data = new byte[256];
            data[0] = 4;
            Encoding.ASCII.GetBytes(Name).CopyTo(data, 1);
            BitConverter.GetBytes(new DateTimeOffset(DateModified).ToUnixTimeSeconds()).CopyTo(data, 33);
            data[41] = (byte)((CanRead ? 2 : 0) | (CanWrite ? 1 : 0));
            BitConverter.GetBytes(FileSize).CopyTo(data, 42);
            contents.CopyTo(data, 46);
            BitConverter.GetBytes(NextPage).CopyTo(data, 254);
            page.SetData(data);
            page.Flush();
        }

        public void Free()
        {
            sys.Free(page.PageId);
            GetNext()?.Free();
        }
    }
}
