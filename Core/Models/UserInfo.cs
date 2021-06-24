using System;
using System.Text;

namespace MyFS.Core.Models
{
    internal class UserInfo
    {
        private static readonly byte[] emptyPassword = new byte[16];
        public string UserName = string.Empty;
        public byte[] PasswordMD5 = emptyPassword;
        public ushort Root;

        private readonly SystemInfo sys;

        public UserInfo(SystemInfo sys)
        {
            this.sys = sys;
        }
        public UserInfo(SystemInfo sys, byte[] data)
        {
            if (data.Length != 32)
            {
                throw new ArgumentException("Data length must be 32.", nameof(data));
            }
            this.sys = sys;
            UserName = Encoding.ASCII.GetString(data[..14]).Replace("\0", "");
            PasswordMD5 = data[14..30];
            Root = BitConverter.ToUInt16(data.AsSpan()[30..32]);
        }

        public FolderInfo GetUserFolder()
        {
            return new(sys, null, sys.disk.ReadPage(Root) ?? throw new ObjectDisposedException("Disk is not loaded."));
        }

        public void Delete()
        {
            GetUserFolder().Free();
            UserName = string.Empty;
            PasswordMD5 = emptyPassword;
            Root = 0;
        }

        public byte[] Serialize()
        {
            byte[] data = new byte[32];
            Encoding.ASCII.GetBytes(UserName).CopyTo(data, 0);
            PasswordMD5.CopyTo(data, 14);
            BitConverter.GetBytes(Root).CopyTo(data, 30);
            return data;
        }
    }
}
