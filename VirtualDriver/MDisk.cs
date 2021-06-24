using System.Linq;
using MyFS.HIAL;

namespace MyFS.VirtualDriver
{
    public class MDisk : IDisk
    {
        public bool Loaded => true;

        private readonly byte[] data = new byte[16777216];

        public byte? ReadByte(ushort pageId, byte offset)
        {
            return data[pageId * 256 + offset];
        }

        public IPage? ReadPage(ushort pageId)
        {
            return new Page(this, pageId);
        }

        public IPageData? ReadPageData(ushort pageId)
        {
            return new PageData(data[(pageId * 256)..(pageId * 256 + 256)]);
        }

        public void Unload()
        {
        }

        public bool WriteByte(ushort pageId, byte offset, byte data)
        {
            this.data[pageId * 256 + offset] = data;
            return true;
        }

        public bool WriteData(ushort pageId, byte[] data)
        {
            data.CopyTo(this.data, pageId * 256);
            return true;
        }

        public bool WritePage(ushort pageId, IPageData page)
        {
            page.ToArray().CopyTo(this.data, pageId * 256);
            return true;
        }
    }
}
