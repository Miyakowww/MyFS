using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyFS.HIAL;

namespace MyFS.VirtualDriver
{
    public class Page : PageData, IPage
    {
        private readonly IDisk disk;

        internal readonly ushort pageId;

        public ushort PageId => pageId;

        internal Page(IDisk disk, ushort pageId) : base(disk.ReadPageData(pageId)?.ToArray() ?? new byte[256])
        {
            this.disk = disk;
            this.pageId = pageId;
        }

        public void Flush()
        {
            disk.WritePage(pageId, this);
        }
    }

    public class PageData : IPageData
    {
        private readonly byte[] data;

        public byte[] this[Range range] => data[range];

        public byte this[byte offset] { get => data[offset]; set => data[offset] = value; }

        byte IReadonlyPageData.this[byte offset] => data[offset];

        public PageData()
        {
            data = new byte[256];
        }

        public PageData(byte[] data)
        {
            this.data = new byte[256];
            data.CopyTo(this.data, 0);
        }

        public IEnumerator<byte> GetEnumerator()
            => data.AsEnumerable().GetEnumerator();

        public byte ReadByte(byte offset)
            => data[offset];

        public void WriteByte(byte offset, byte data)
            => this.data[offset] = data;

        public void WriteData(byte offset, byte[] data)
            => data.CopyTo(this.data, offset);

        public void SetData(byte[] data)
        {
            Array.Fill<byte>(this.data, 0);
            data.CopyTo(this.data, 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
            => data.GetEnumerator();
    }
}
