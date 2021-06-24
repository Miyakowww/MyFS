using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Maila.Utils
{
    public class Command
    {
        public readonly ImmutableArray<string> name;
        public readonly ImmutableDictionary<string, string> param;

        public string? this[string name] { get => param.GetValueOrDefault(name.ToLower()); }
        public bool HaveParam(string name) => param.ContainsKey(name);

        public Command(string cmd)
        {
            List<string> name = new();
            Dictionary<string, string> param = new();
            bool onName = true;
            bool onParam = false;
            string tmpParam = string.Empty;
            foreach (var item in cmd.Split(' '))
            {
                if (item == "")
                {
                    continue;
                }
                if (item.StartsWith('-'))
                {
                    if (name.Count == 0 || item.Length == 1)
                    {
                        throw new ArgumentException("Invalid Command");
                    }
                    onName = false;
                    if (onParam)
                    {
                        if (param.ContainsKey(tmpParam.ToLower()))
                        {
                            throw new ArgumentException("Duplicated param");
                        }
                        param.Add(tmpParam.ToLower(), "");
                    }
                    onParam = false;
                    if (item[1] == '-')
                    {
                        if (item.Length == 2)
                        {
                            throw new ArgumentException("Invalid Command");
                        }
                        tmpParam = item[2..];
                        onParam = true;
                    }
                    else
                    {
                        if (item.Length == 2)
                        {
                            tmpParam = item[1].ToString();
                            onParam = true;
                        }
                        else
                        {
                            if (param.ContainsKey(item[1].ToString().ToLower()))
                            {
                                throw new ArgumentException("Duplicated param");
                            }
                            param.Add(item[1].ToString().ToLower(), item[2..]);
                        }
                    }
                }
                else if (onParam)
                {
                    if (param.ContainsKey(tmpParam.ToLower()))
                    {
                        throw new ArgumentException("Duplicated param");
                    }
                    param.Add(tmpParam.ToLower(), item);
                    onParam = false;
                }
                else if (onName)
                {
                    name.Add(item);
                }
                else
                {
                    throw new ArgumentException("Invalid Command");
                }
            }
            if (onParam)
            {
                if (param.ContainsKey(tmpParam.ToLower()))
                {
                    throw new ArgumentException("Duplicated param");
                }
                param.Add(tmpParam.ToLower(), "");
            }
            if (name.Count == 0)
            {
                throw new ArgumentException("Invalid Command");
            }
            this.name = ImmutableArray.CreateRange(name);
            this.param = ImmutableDictionary.CreateRange(param);
        }
        public static Command? Parse(string? cmd)
        {
            if (cmd is null)
            {
                return null;
            }
            try
            {
                return new(cmd);
            }
            catch
            {
                return null;
            }
        }
    }
}
