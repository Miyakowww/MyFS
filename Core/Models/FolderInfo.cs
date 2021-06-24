using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyFS.HIAL;

namespace MyFS.Core.Models
{
    internal class FolderInfo
    {
        public string Name = string.Empty;
        public DateTime DateModified;
        public bool CanRead;
        public bool CanWrite;
        public ushort[] Filelist;
        public ushort NextPage;

        private int listLength;

        private readonly SystemInfo sys;
        private readonly IPage page;
        public FolderInfo? parent;

        public ushort PageId => page.PageId;
        public bool IsEmpty => listLength < 1;

        public FolderInfo(SystemInfo sys, FolderInfo? parent, IPage page)
        {
            this.sys = sys;
            this.page = page;
            this.parent = parent;
            Name = Encoding.ASCII.GetString(page[1..33]).Replace("\0", "");
            DateModified = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(page[33..41])).DateTime;
            byte protection = page[41];
            CanRead = protection.CheckByte(1);
            CanWrite = protection.CheckByte(0);
            Filelist = new ushort[106];
            for (int i = 0; i < 106; i++)
            {
                Filelist[i] = BitConverter.ToUInt16(page[(42 + i * 2)..(44 + i * 2)]);
                if (Filelist[i] != 0) listLength++;
            }
            NextPage = BitConverter.ToUInt16(page[254..256]);
            if (page[0] == 0)
            {
                DateModified = DateTime.Now;
                CanRead = CanWrite = true;
                Flush();
            }
        }

        public bool Add(ushort pageId)
        {
            if (Filelist.Length == listLength)
            {
                FilelistInfo? next = GetNext();
                if (next is null)
                {
                    IPage? newPage = sys.Allocate();
                    if (newPage is null)
                    {
                        return false;
                    }
                    if (!new FilelistInfo(sys, newPage).Add(pageId))
                    {
                        sys.Free(newPage.PageId);
                        return false;
                    }
                    NextPage = newPage.PageId;
                    DateModified = DateTime.Now;
                    Flush();
                    return true;
                }
                else if (!next.Add(pageId))
                {
                    return false;
                }
                DateModified = DateTime.Now;
                Flush();
                return true;
            }
            Filelist[listLength++] = pageId;
            DateModified = DateTime.Now;
            Flush();
            return true;
        }
        public bool Remove(ushort pageId)
        {
            for (int i = 0; i < Filelist.Length; i++)
            {
                if (Filelist[i] == 0)
                {
                    return false;
                }
                if (Filelist[i] == pageId)
                {
                    if ((i + 1 < Filelist.Length && Filelist[i + 1] == 0) ||
                        (i + 1 == Filelist.Length && NextPage == 0))
                    {
                        Filelist[i] = 0;
                        listLength--;
                        Flush();
                        return true;
                    }
                    if (NextPage == 0)
                    {
                        Filelist[i] = Filelist[--listLength];
                        Filelist[listLength] = 0;
                        Flush();
                        return true;
                    }

                    FilelistInfo? last = null;
                    FilelistInfo next = GetNext()!;
                    while (next.NextPage != 0)
                    {
                        last = next;
                        next = next.GetNext()!;
                    }
                    if (next.listLength == 1)
                    {
                        Filelist[i] = next.Filelist[0];
                        next.Free();
                        if (last is null)
                        {
                            NextPage = 0;
                        }
                        else
                        {
                            last.NextPage = 0;
                        }
                        Flush();
                        return true;
                    }
                    Filelist[i] = next.Filelist[--next.listLength];
                    next.Filelist[next.listLength] = 0;
                    next.Flush();
                    Flush();
                    return true;
                }
            }
            return false;
        }
        public FileInfo? CreateFile(string name)
        {
            IPage? newPage = sys.Allocate();
            if (newPage is null)
            {
                return null;
            }
            if (!Add(newPage.PageId))
            {
                sys.Free(newPage.PageId);
                return null;
            }
            FileInfo file = new(sys, this, newPage) { Name = name };
            file.Flush();
            return file;
        }
        public FolderInfo? CreateFolder(string name)
        {
            IPage? newPage = sys.Allocate();
            if (newPage is null)
            {
                return null;
            }
            if (!Add(newPage.PageId))
            {
                sys.Free(newPage.PageId);
                return null;
            }
            FolderInfo folder = new(sys, this, newPage) { Name = name };
            folder.Flush();
            return folder;
        }
        public FolderInfo[] GetAllFolders()
        {
            return GetAll()
                .Select(i => sys.disk.ReadPage(i))
                .Where(p => p is not null && p[0] == 2)
                .Select(p => new FolderInfo(sys, this, p!)).ToArray();
        }
        public FileInfo[] GetAllFiles()
        {
            return GetAll()
                .Select(i => sys.disk.ReadPage(i))
                .Where(p => p is not null && p[0] == 4)
                .Select(p => new FileInfo(sys, this, p!)).ToArray();
        }
        private ushort[] GetAll()
        {
            if (listLength <= Filelist.Length)
            {
                return Filelist[..listLength];
            }
            return Filelist.Concat(GetNext()!.GetAll()).ToArray();
        }

        public FilelistInfo? GetNext()
        {
            return NextPage == 0 ? null : new(sys, sys.disk.ReadPage(NextPage) ?? throw new ObjectDisposedException("Disk is not loaded."));
        }
        public string GetPath()
        {
            if (this.parent is null)
            {
                return "/";
            }
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
            data[0] = 2;
            Encoding.ASCII.GetBytes(Name).CopyTo(data, 1);
            BitConverter.GetBytes(new DateTimeOffset(DateModified).ToUnixTimeSeconds()).CopyTo(data, 33);
            data[41] = (byte)((CanRead ? 2 : 0) | (CanWrite ? 1 : 0));
            for (int i = 0; i < 106; i++)
            {
                BitConverter.GetBytes(Filelist[i]).CopyTo(data, 42 + i * 2);
            }
            BitConverter.GetBytes(NextPage).CopyTo(data, 254);
            page.SetData(data);
            page.Flush();
        }

        public void Free()
        {
            foreach (var item in GetAllFolders())
            {
                item.Free();
            }
            foreach (var item in GetAllFiles())
            {
                item.Free();
            }
            sys.Free(page.PageId);
            GetNext()?.Free();
        }
    }
}
