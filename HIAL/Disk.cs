namespace MyFS.HIAL
{
    public interface IDisk
    {
        public bool Loaded { get; }
        public byte? ReadByte(ushort pageId, byte offset);
        public bool WriteByte(ushort pageId, byte offset, byte data);
        public IPage? ReadPage(ushort pageId);
        public IPageData? ReadPageData(ushort pageId);
        public bool WritePage(ushort pageId, IPageData page);
        public bool WriteData(ushort pageId, byte[] data);
        public void Unload();
    }
}
