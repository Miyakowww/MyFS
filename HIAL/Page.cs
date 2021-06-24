using System;
using System.Collections;
using System.Collections.Generic;

namespace MyFS.HIAL
{
    public interface IPage : IPageData
    {
        public ushort PageId { get; }
        public void Flush();
    }

    public interface IPageData : IReadonlyPageData
    {
        public new byte this[byte offset] { get; set; }
        public new byte[] this[Range range] { get; }

        public void WriteByte(byte offset, byte data);
        public void WriteData(byte offset, byte[] data);
        public void SetData(byte[] data);
    }

    public interface IReadonlyPageData : IEnumerable<byte>, IEnumerable
    {
        public byte this[byte offset] { get; }
        public byte[] this[Range range] { get; }

        public byte ReadByte(byte offset);
    }
}
