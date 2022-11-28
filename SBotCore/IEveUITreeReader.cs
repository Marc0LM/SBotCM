
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SBotCore
{
    public interface IEveUITreeReader
    {
        public ulong FindRootAddress(int pid);
        public UITreeNode ReadUITree(int depth);

        public object Stat();

    }
    public class EveUITreeReader
    {
        public static IEveUITreeReader Get()
        {
            if (!WinApi.CheckCkeck())
            {
                return null;
            }

            IEveUITreeReader EUITR=null;
            var backends = new List<string>();
            Directory.GetFiles(Assembly.GetExecutingAssembly().Location.Split("\\SBotCore.dll").First(), "*.dll").ToList().ForEach(c => backends.Add(c.Split("\\").Last()));

            backends.ForEach(backend =>
            {
                if (EUITR == null)
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(backend);
                        var types = assembly.GetTypes();
                        var type=types.FirstOrDefault(t => t.GetInterfaces().Any(i=>i == typeof(IEveUITreeReader)));
                        if (type != null)
                        {
                            object o = Activator.CreateInstance(type);
                            EUITR = (o as IEveUITreeReader);
                            Console.WriteLine(type.Name);
                        }
                    }
                    catch(Exception ex) { 
                        Console.WriteLine(ex.ToString()); }
                }
            });
           
            return EUITR;
        }

        public static IEveUITreeReader Get(string specifier)
        {
            if (!WinApi.CheckCkeck())
            {
                return null;
            }
            IEveUITreeReader EUITR = null;
            var backends = new List<string>();
            Directory.GetFiles(Assembly.GetExecutingAssembly().Location.Split("\\SBotCore.dll").First(), "*.dll").ToList().ForEach(c => backends.Add(c.Split("\\").Last()));

            backends.ForEach(backend =>
            {
                if (EUITR == null)
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(backend);
                        var types = assembly.GetTypes();
                        var type = types.FirstOrDefault(t => t.GetInterfaces().Any(i => i == typeof(IEveUITreeReader)) && t.Name.Contains(specifier));
                        if (type != null)
                        {
                            object o = Activator.CreateInstance(type);
                            EUITR = (o as IEveUITreeReader);
                            Console.WriteLine(type.Name);
                        }
                    }
                    catch { }
                }
            });

            return EUITR;
        }
    }
}
