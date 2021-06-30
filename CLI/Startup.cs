using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MyFS.API;

namespace MyFS.CLI
{
    public static class Startup
    {
        public static bool Start(string username, IFolder root)
        {
            IFolder current = root;
            IFile? opened = null;
            while (true)
            {
                Console.Write($"{username}:{current.Path}$ ");
                string? input = Console.ReadLine()?.Trim();
                string[]? splited = input?.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

                if (input is null || splited is null || splited.Length < 1)
                {
                    continue;
                }

                switch (splited[0])
                {
                    case "help":
                        Console.WriteLine("MyFS CLI, version 1.0.0");
                        Console.WriteLine();
                        Console.WriteLine("dir/ls <args>");
                        Console.WriteLine("    -a  Show all files");
                        Console.WriteLine("    -i  Show info");
                        Console.WriteLine("create [name]");
                        Console.WriteLine("rm <args> [name]");
                        Console.WriteLine("    -r  Remove directory");
                        Console.WriteLine("    -f  force");
                        Console.WriteLine("mkdir [name]");
                        Console.WriteLine("cd [dir]");
                        Console.WriteLine("open [name]");
                        Console.WriteLine("close");
                        Console.WriteLine("read");
                        Console.WriteLine("edit");
                        Console.WriteLine("cat [name]");
                        Console.WriteLine("chmod [oper] [name]");
                        Console.WriteLine("logout");
                        Console.WriteLine("exit");
                        break;
                    case "exit":
                        return false;
                    case "logout":
                        return true;
                    case "dir":
                    case "ls":
                        Dir(current, splited);
                        break;
                    case "create":
                        Create(current, splited);
                        break;
                    case "rm":
                        Rm(current, splited);
                        break;
                    case "mkdir":
                        Mkdir(current, splited);
                        break;
                    case "cd":
                        current = Cd(root, current, splited);
                        break;
                    case "open":
                        opened = Open(current, splited);
                        break;
                    case "close":
                        opened = null;
                        break;
                    case "read":
                        Read(opened);
                        break;
                    case "edit":
                        Edit(opened);
                        break;
                    case "import":
                        Import(opened, splited);
                        break;
                    case "export":
                        Export(opened, splited);
                        break;
                    case "cat":
                        Cat(current, splited);
                        break;
                    case "chmod":
                        Chmod(current, splited);
                        break;
                    case "clear":
                        Console.Clear();
                        break;
                    default:
                        Console.WriteLine($"{splited[0]}: command not found.");
                        break;
                }
            }
        }

        private static void Dir(IFolder folder, string[] splitedCmd)
        {
            bool all = splitedCmd.Contains("-a") || splitedCmd.Contains("-ai");
            bool info = splitedCmd.Contains("-i") || splitedCmd.Contains("-ai");

            var subFolders = folder.GetAllFolders()?.Where(f => all || !f.Name.StartsWith('.'));
            if (subFolders is null)
            {
                Console.WriteLine("Permission denied.");
                return;
            }
            foreach (var item in subFolders)
            {
                var orig = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                if (info)
                {
                    Console.WriteLine($"{(item.CanRead ? "r" : "-")}{(item.CanWrite ? "w" : "-")}\t{item.Name}\t\t{item.DateModified:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    Console.WriteLine(item.Name);
                }
                Console.ForegroundColor = orig;
            }

            var subFiles = folder.GetAllFiles()!.Where(f => all || !f.Name.StartsWith('.'));
            foreach (var item in subFiles)
            {
                if (info)
                {
                    Console.WriteLine($"{(item.CanRead ? "r" : "-")}{(item.CanWrite ? "w" : "-")}\t{item.Name}\t\t{item.DateModified:yyyy-MM-dd HH:mm:ss}\t{SizeToString(item.FileSize)}");
                }
                else
                {
                    Console.WriteLine(item.Name);
                }
            }
            Console.WriteLine($"\nTotals {subFolders.Count() + subFiles.Count()}.");
        }
        private static string SizeToString(long size)
        {
            int level = 0;
            double fsize = size;
            while (fsize > 1024)
            {
                level++;
                fsize /= 1024;
            }
            return level switch
            {
                0 => $"{size}B",
                1 => $"{fsize:f2}KB",
                2 => $"{fsize:f2}MB",
                3 => $"{fsize:f2}GB",
                4 => $"{fsize:f2}TB",
                _ => "Error"
            };
        }

        private static void Create(IFolder folder, string[] splitedCmd)
        {
            if (splitedCmd.Length < 2)
            {
                Console.WriteLine("Missing file operand.");
                return;
            }
            if (splitedCmd[1].Contains('/') || splitedCmd[1] == "." || splitedCmd[1] == "..")
            {
                Console.WriteLine("Invalid file name.");
                return;
            }
            if (folder.ContainsFile(splitedCmd[1]))
            {
                Console.WriteLine("File already exists.");
                return;
            }
            if (folder.CreateFile(splitedCmd[1]) is null)
            {
                Console.WriteLine("Disk is full.");
            }
        }

        private static void Rm(IFolder folder, string[] splitedCmd)
        {
            bool dir = splitedCmd.Contains("-r") || splitedCmd.Contains("-rf");
            bool force = splitedCmd.Contains("-f") || splitedCmd.Contains("-rf");
            string? name = splitedCmd[1..].FirstOrDefault(n => !n.StartsWith("-"));
            if (name is null)
            {
                Console.WriteLine("No file removed.");
                return;
            }
            if (dir)
            {
                IFolder? subFolder = folder.GetFolder(name);
                if (subFolder is null)
                {
                    Console.WriteLine("No such folder.");
                    return;
                }
                if (!subFolder.IsEmpty && !force)
                {
                    Console.WriteLine("Folder is not empty.");
                    return;
                }
                if (!subFolder.Delete())
                {
                    Console.WriteLine("Permission denied.");
                }
                return;
            }
            IFile? file = folder.GetFile(name);
            if (file is null)
            {
                Console.WriteLine("No such file.");
                return;
            }
            if (!file.Delete())
            {
                Console.WriteLine("Permission denied.");
            }
        }

        private static void Mkdir(IFolder folder, string[] splitedCmd)
        {
            if (splitedCmd.Length < 2)
            {
                Console.WriteLine("Missing operand.");
                return;
            }
            if (splitedCmd[1].Contains('/') || splitedCmd[1] == "." || splitedCmd[1] == "..")
            {
                Console.WriteLine("Invalid folder name.");
                return;
            }
            if (folder.ContainsFolder(splitedCmd[1]))
            {
                Console.WriteLine("Folder already exists.");
                return;
            }
            if (folder.CreateSubFolder(splitedCmd[1]) is null)
            {
                Console.WriteLine("Disk is full.");
            }
        }

        private static IFolder Cd(IFolder root, IFolder origin, string[] splitedCmd)
        {
            if (splitedCmd.Length < 2)
            {
                return origin;
            }
            string[] dirs = splitedCmd[1].Split('/');
            IFolder current = origin;
            if (dirs[0] == "")
            {
                current = root;
                dirs = dirs[1..];
            }
            foreach (var dir in dirs)
            {
                if (dir == "." || dir == "")
                {
                    continue;
                }
                if (dir == "..")
                {
                    current = current.Parent ?? root;
                    continue;
                }
                if (current.ContainsFolder(dir))
                {
                    IFolder? folder = current.GetFolder(dir);
                    if (folder is null)
                    {
                        Console.WriteLine("Permission denied.");
                        return origin;
                    }
                    current = folder;
                    continue;
                }
                Console.WriteLine("No such directory.");
                return origin;
            }
            return current;
        }

        private static IFile? Open(IFolder folder, string[] splitedCmd)
        {
            if (splitedCmd.Length < 2)
            {
                Console.WriteLine("Missing file operand.");
                return null;
            }
            if (!folder.ContainsFile(splitedCmd[1]))
            {
                Console.WriteLine("No such file.");
                return null;
            }
            IFile? file = folder.GetFile(splitedCmd[1]);
            if (file is null)
            {
                Console.WriteLine("Permission denied.");
                return null;
            }
            return file;
        }

        private static void Read(IFile? file)
        {
            if (file is null)
            {
                Console.WriteLine("No opened files.");
                return;
            }
            string? data = file.ReadAllText();
            if (data is null)
            {
                Console.WriteLine("Permission denied.");
                return;
            }
            Console.WriteLine(data);
        }

        private static void Edit(IFile? file)
        {
            if (file is null)
            {
                Console.WriteLine("No opened files.");
                return;
            }
            byte[]? data = file.ReadAllBytes();
            if (data is null)
            {
                Console.WriteLine("Permission denied.");
                return;
            }

            File.WriteAllBytes("tmp", data);
            Process.Start("notepad", "tmp").WaitForExit();
            if (!file.WriteAllBytes(File.ReadAllBytes("tmp")))
            {
                Console.WriteLine("Permission denied.");
            }
            File.Delete("tmp");
        }
        private static void Import(IFile? file, string[] splitedCmd)
        {
            if (file is null)
            {
                Console.WriteLine("No opened files.");
                return;
            }
            if (splitedCmd.Length < 2)
            {
                Console.WriteLine("Missing file operand.");
                return;
            }
            if (File.Exists(splitedCmd[1]))
            {
                if (!file.WriteAllBytes(File.ReadAllBytes(splitedCmd[1])))
                {
                    Console.WriteLine("Permission denied.");
                }
            }
        }
        private static void Export(IFile? file, string[] splitedCmd)
        {
            if (file is null)
            {
                Console.WriteLine("No opened files.");
                return;
            }
            if (splitedCmd.Length < 2)
            {
                Console.WriteLine("Missing file operand.");
                return;
            }
            var data = file.ReadAllBytes();
            if (data is null)
            {
                Console.WriteLine("Permission denied.");
                return;
            }
            File.WriteAllBytes(splitedCmd[1], data);
        }

        private static void Cat(IFolder folder, string[] splitedCmd)
        {
            if (splitedCmd.Length < 2)
            {
                Console.WriteLine("Missing file operand.");
                return;
            }
            if (!folder.ContainsFile(splitedCmd[1]))
            {
                Console.WriteLine("No such file.");
                return;
            }
            IFile? file = folder.GetFile(splitedCmd[1]);
            if (file is null)
            {
                Console.WriteLine("Permission denied.");
                return;
            }
            string? text = file.ReadAllText();
            if (text is null)
            {
                Console.WriteLine("Permission denied.");
                return;
            }
            Console.WriteLine(text);
        }

        private static void Chmod(IFolder folder, string[] splitedCmd)
        {
            if (splitedCmd.Length < 3)
            {
                Console.WriteLine("Missing operand.");
                return;
            }
            if (!folder.ContainsFile(splitedCmd[2]))
            {
                Console.WriteLine("No such file.");
                return;
            }
            IFile? file = folder.GetFile(splitedCmd[2]);
            if (file is null)
            {
                Console.WriteLine("Permission denied.");
                return;
            }
            var (r, w) = splitedCmd[1] switch
            {
                "+r" => (1, 0),
                "+w" => (0, 1),
                "+rw" => (1, 1),
                "-r" => (2, 0),
                "-w" => (0, 2),
                "-rw" => (2, 2),
                _ => (0, 0)
            };
            switch (r)
            {
                case 1:
                    file.CanRead = true;
                    break;
                case 2:
                    file.CanRead = false;
                    break;
            }
            switch (w)
            {
                case 1:
                    file.CanWrite = true;
                    break;
                case 2:
                    file.CanWrite = false;
                    break;
            }
        }
    }
}
