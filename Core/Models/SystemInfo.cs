using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MyFS.HIAL;

namespace MyFS.Core.Models
{
    public class SystemInfo
    {
        public byte Version = 1;
        public ushort FirstEmptyPage = 33;
        public byte[] PageInfo;

        private readonly UserInfo[] users = new UserInfo[7];

        public readonly IDisk disk;
        private readonly IPage page;
        private readonly IPage[] maps;

        public SystemInfo(IDisk disk)
        {
            this.disk = disk;
            page = disk.ReadPage(0) ?? throw new ArgumentException("Disk is not loaded.", nameof(disk));
            maps = new IPage[32];
            for (int i = 0; i < 32; i++)
            {
                maps[i] = disk.ReadPage((ushort)(i + 1)) ?? throw new ArgumentException("Disk is not loaded.", nameof(disk));
            }
            if (page[0] != 1)
            {
                PageInfo = new byte[4];
                for (int i = 0; i < 7; i++)
                {
                    users[i] = new(this);
                }
                IPage map = maps[0];
                for (byte i = 0; i < 4; i++)
                {
                    map[i] = 0xFF;
                }
                return;
            }
            Version = page[1];
            FirstEmptyPage = BitConverter.ToUInt16(page[2..4]);
            PageInfo = page[4..8];

            for (int i = 1; i < 8; i++)
            {
                users[i - 1] = new(this, page[(i * 32)..(i * 32 + 32)]);
            }
        }

        public Folder? Login(string username, string password)
        {
            UserInfo? user = users.Where(u => u.Root != 0).FirstOrDefault(u => u.UserName == username);
            if (user is null ||
                !user.PasswordMD5.SequenceEqual(
                MD5.HashData(Encoding.ASCII.GetBytes(password))))
            {
                return null;
            }
            return new Folder(user.GetUserFolder(), null);
        }
        public int Regist(string username, string password)
        {
            if (users.Where(u => u.Root != 0).Any(u => u.UserName == username))
            {
                return 1;
            }
            UserInfo? user = users.FirstOrDefault(u => u.Root == 0);
            if (user is null)
            {
                return 2;
            }

            IPage? newPage = Allocate();
            if (newPage is null)
            {
                return 3;
            }

            _ = new FolderInfo(this, null, newPage);
            user.UserName = username;
            user.PasswordMD5 = MD5.HashData(Encoding.ASCII.GetBytes(password));
            user.Root = newPage.PageId;
            Flush();
            return 0;
        }
        public bool RemoveUser(string username)
        {
            UserInfo? user = users.Where(u => u.Root != 0).FirstOrDefault(u => u.UserName == username);
            if (user is null)
            {
                return false;
            }
            user.Delete();
            Flush();
            return true;
        }

        private bool PageUsed(ushort pageId)
            => (maps[pageId / 2048][(byte)(pageId % 2048 / 8)]).CheckByte(pageId % 8);
        private void UsePage(ushort pageId)
        {
            IPage map = maps[pageId / 2048];
            map[(byte)(pageId % 2048 / 8)] |= (byte)(1 << (pageId % 8));
            map.Flush();
            PageInfo[pageId / 16384] = PageInfo[pageId / 16384].SetByte(FirstEmptyPage / 2048 % 8, map.All(b => b == 0xFF));
            Flush();
        }

        internal IPage? Allocate()
        {
            if (FirstEmptyPage == 0)
            {
                return null;
            }
            IPage page = disk.ReadPage(FirstEmptyPage) ?? throw new ObjectDisposedException("Disk is not loaded.");
            page.SetData(Array.Empty<byte>());
            UsePage(FirstEmptyPage);
            FirstEmptyPage++;
            if (PageUsed(FirstEmptyPage))
            {
                FirstEmptyPage++;
                int pageBlock = FirstEmptyPage / 16384;
                while (pageBlock < 4 && PageInfo[pageBlock] == 0xFF)
                {
                    pageBlock++;
                    FirstEmptyPage = (ushort)(pageBlock * 16384);
                }
                if (pageBlock >= 4)
                {
                    FirstEmptyPage = 0;
                    return page;
                }

                int pageVolume = FirstEmptyPage / 2048 % 8;
                byte pageInfo = PageInfo[pageBlock];
                while (pageVolume < 8 && pageInfo.CheckByte(pageVolume))
                {
                    pageVolume++;
                    FirstEmptyPage = (ushort)(pageBlock * 16384 + pageVolume * 2048);
                }
                if (pageVolume >= 8)
                {
                    FirstEmptyPage = 0;
                    return page;
                }

                byte pageRange = (byte)(FirstEmptyPage % 2048 / 8);
                IPage map = maps[pageBlock * 8 + pageVolume];
                while (pageRange < 255 && map[pageRange] == 0xFF)
                {
                    pageRange++;
                    FirstEmptyPage = (ushort)(pageBlock * 16384 + pageVolume * 2048 + pageRange * 8);
                }

                for (ushort i = FirstEmptyPage; i > 0; i++)
                {
                    if (!map[pageRange].CheckByte(i % 8))
                    {
                        break;
                    }
                }
            }
            return page;
        }
        internal void Free(ushort pageId)
        {
            if (!PageUsed(pageId))
            {
                return;
            }
            IPage map = maps[pageId / 2048];
            map[(byte)(pageId % 2048 / 8)] &= (byte)~(1 << (pageId % 8));
            map.Flush();
            PageInfo[pageId / 16384] = PageInfo[pageId / 16384].SetByte(FirstEmptyPage / 2048 % 8, false);
            if (FirstEmptyPage > pageId)
            {
                FirstEmptyPage = pageId;
            }
            Flush();
        }

        internal void Flush()
        {
            byte[] data = new byte[256];
            data[0] = 1;
            data[1] = Version;
            BitConverter.GetBytes(FirstEmptyPage).CopyTo(data, 2);
            PageInfo.CopyTo(data, 4);
            for (int i = 0; i < users.Length; i++)
            {
                users[i].Serialize().CopyTo(data, i * 32 + 32);
            }
            page.SetData(data);
            page.Flush();
        }
        internal void Close()
        {
            Flush();
            disk.Unload();
        }
    }
}
