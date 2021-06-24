namespace MyFS.Core
{
    public static class Extensions
    {
        public static bool CheckByte(this byte data, int offset)
        {
            return ((data >> offset) & 1) > 0;
        }
        public static byte SetByte(this byte data, int offset, bool status)
        {
            return status ? (byte)(data | (1 << offset)) : (byte)(data & (~(1 << offset)));
        }
    }
}
