using System;
using System.Collections.Generic;
using System.Linq;
using MyFS.HIAL;

namespace MyFS.Core.Models
{
    internal class FileContentInfo
    {
        public int RemainingSize;
        public byte[] contents;
        public ushort NextPage;

        private readonly SystemInfo sys;
        private readonly IPage page;

        public ushort PageId => page.PageId;

        public FileContentInfo(SystemInfo sys, IPage page)
        {
            this.sys = sys;
            this.page = page;
            RemainingSize = BitConverter.ToInt32(page[1..5]);
            contents = page[5..254];
            NextPage = BitConverter.ToUInt16(page[254..256]);
        }

        public FileContentInfo? GetNext()
        {
            return NextPage == 0 ? null : new(sys, sys.disk.ReadPage(NextPage) ?? throw new ObjectDisposedException("Disk is not loaded."));
        }

        public bool WriteAllBytes(byte[] data, int offset)
        {
            if (data.Length - offset <= contents.Length)
            {
                Array.Fill<byte>(contents, 0);
                for (int i = offset; i < data.Length; i++)
                {
                    contents[i - offset] = data[i];
                }
                GetNext()?.Free();
                RemainingSize = data.Length - offset;
                NextPage = 0;
                Flush();
                return true;
            }

            FileContentInfo? last;
            FileContentInfo? next = this;
            List<FileContentInfo> tmp = new();
            while (data.Length - offset > contents.Length)
            {
                for (int i = 0; i < contents.Length; i++)
                {
                    next.contents[i] = data[offset + i];
                }
                next.RemainingSize = data.Length - offset;
                next.Flush();
                offset += contents.Length;

                last = next;
                next = next.GetNext();
                if (next is null)
                {
                    IPage? newPage = sys.Allocate();
                    if (newPage is null)
                    {
                        foreach (var item in tmp)
                        {
                            sys.Free(item.PageId);
                        }
                        return false;
                    }
                    next = new FileContentInfo(sys, newPage);
                    tmp.Add(next);
                    last.NextPage = newPage.PageId;
                    last.Flush();
                }
            }
            Array.Fill<byte>(next.contents, 0);
            for (int i = offset; i < data.Length; i++)
            {
                next.contents[i - offset] = data[i];
            }
            next.GetNext()?.Free();
            next.RemainingSize = data.Length - offset;
            next.NextPage = 0;
            next.Flush();
            return true;
        }
        public bool AppendAllBytes(byte[] data, int offset)
        {
            if (data.Length - offset + RemainingSize <= contents.Length)
            {
                for (int i = offset; i < data.Length; i++)
                {
                    contents[RemainingSize + i - offset] = data[i];
                }
                RemainingSize += data.Length - offset;
                Flush();
                return true;
            }

            FileContentInfo? next = this;
            List<FileContentInfo> tmp = new();
            while (next!.RemainingSize > contents.Length)
            {
                next = next.GetNext();
            }
            return next.WriteAllBytes(next.contents[..next.RemainingSize].Concat(data[offset..]).ToArray(), 0);
        }

        public void Flush()
        {
            byte[] data = new byte[256];
            data[0] = 5;
            BitConverter.GetBytes(RemainingSize).CopyTo(data, 1);
            contents.CopyTo(data, 5);
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
