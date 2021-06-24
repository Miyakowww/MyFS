using System;
using System.Linq;
using MyFS.CLI;
using MyFS.Core.Models;
using MyFS.VirtualDriver;

namespace Entry
{
    class Program
    {
        static void Main()
        {
            //Disk disk = Disk.LoadDisk("./data.myvd");
            MDisk disk = new();
            SystemInfo system = new(disk);
            while (true)
            {
                Console.Write($"?$ ");
                string? input = Console.ReadLine()?.Trim();
                string[]? splited = input?.Split(' ').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

                if (input is null || splited is null || splited.Length < 1)
                {
                    continue;
                }

                switch (splited[0])
                {
                    case "help":
                        Console.WriteLine("login [username] [password]");
                        Console.WriteLine("regist [username] [password]");
                        Console.WriteLine("remove [username]");
                        Console.WriteLine("exit");
                        break;
                    case "login":
                        if (splited.Length < 3)
                        {
                            Console.WriteLine("Missing operand.");
                            break;
                        }
                        var root = system.Login(splited[1], splited[2]);
                        if (root is null)
                        {
                            Console.WriteLine("Wrong user name or password");
                            break;
                        }
                        if (!Startup.Start(splited[1], root))
                        {
                            disk.Unload();
                            return;
                        }
                        break;
                    case "regist":
                        if (splited.Length < 3)
                        {
                            Console.WriteLine("Missing operand.");
                            break;
                        }
                        if (splited[1].Length > 14)
                        {
                            Console.WriteLine("The user name supports up to 14 half-width characters.");
                            break;
                        }
                        switch (system.Regist(splited[1], splited[2]))
                        {
                            case 1:
                                Console.WriteLine("Username is already in use.");
                                break;
                            case 2:
                                Console.WriteLine("User is full.");
                                break;
                            case 3:
                                Console.WriteLine("Disk is full.");
                                break;
                        }
                        break;
                    case "remove":
                        if (splited.Length < 2)
                        {
                            Console.WriteLine("Missing operand.");
                            break;
                        }
                        if (!system.RemoveUser(splited[1]))
                        {
                            Console.WriteLine("No such user.");
                        }
                        break;
                    case "clear":
                        Console.Clear();
                        break;
                    case "exit":
                        disk.Unload();
                        return;
                    default:
                        Console.WriteLine($"{splited[0]}: command not found.");
                        break;
                }
            }
        }
    }
}
