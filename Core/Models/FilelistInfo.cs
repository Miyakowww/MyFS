using System;
using System.Linq;
using MyFS.HIAL;

namespace MyFS.Core.Models
{
    internal class FilelistInfo
    {
        public ushort[] Filelist;
        public ushort NextPage;

        public int listLength;

        private readonly SystemInfo sys;
        private readonly IPage page;

        public ushort PageId => page.PageId;

        public FilelistInfo(SystemInfo sys, IPage page)
        {
            this.sys = sys;
            this.page = page;
            Filelist = new ushort[126];
            for (int i = 0; i < 126; i++)
            {
                Filelist[i] = BitConverter.ToUInt16(page[(1 + i * 2)..(3 + i * 2)]);
                if (Filelist[i] != 0) listLength++;
            }
            NextPage = BitConverter.ToUInt16(page[253..255]);
        }

        public FilelistInfo? GetNext()
        {
            return NextPage == 0 ? null : new(sys, sys.disk.ReadPage(NextPage) ?? throw new ObjectDisposedException("Disk is not loaded."));
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
                    Flush();
                    return true;
                }
                else if (!next.Add(pageId))
                {
                    return false;
                }
                Flush();
                return true;
            }
            Filelist[listLength++] = pageId;
            Flush();
            return true;
        }
        public ushort[] GetAll()
        {
            if (listLength <= Filelist.Length)
            {
                return Filelist[..listLength];
            }
            return Filelist.Concat(GetNext()!.GetAll()).ToArray();
        }

        public void Flush()
        {
            byte[] data = new byte[256];
            data[0] = 3;
            for (int i = 0; i < 126; i++)
            {
                BitConverter.GetBytes(Filelist[i]).CopyTo(data, 1 + i * 2);
            }
            BitConverter.GetBytes(NextPage).CopyTo(data, 253);
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
