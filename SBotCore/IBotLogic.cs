using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;
using WMPLib;
using YamlDotNet.Serialization;
using static SBotCore.EveUIParser;

namespace SBotCore
{
    public interface IBotLogic
    {
        public static IBotLogic FromConfigFile(string file_name, int mainwindowhandle, LogWriter writer, bool load_from_bytes = false, byte[] assembly_bytes = null)
        {
            Assembly assembly;
            if (!load_from_bytes)
            {
                assembly = Assembly.LoadFrom("SBotLogicImpl.dll");
            }
            else
            {
                assembly = Assembly.Load(assembly_bytes);
            }
            //DumpAllBots(load_from_bytes, assembly_bytes);
            var botlogic_implments = assembly.GetTypes().Where(t => typeof(IBotLogic).IsAssignableFrom(t) && !t.IsAbstract);

            try
            {
                using StreamReader sr = new StreamReader(file_name);
                var deserializer = new DeserializerBuilder()
                    .Build();
                string stream = sr.ReadToEnd();
                var flatView = deserializer.Deserialize<Dictionary<string, object>>(stream);
                if (!flatView.ContainsKey("BotType"))
                {
                    return null;
                }
                string bot_name = (string)flatView["BotType"];
                var botT = botlogic_implments.FirstOrDefault(bli => bli.Name.Equals(bot_name));
                var desMethod = typeof(Deserializer).GetMethod("Deserialize", new[] { typeof(string) });
                //var methods = typeof(Deserializer).GetMethods();//.Select(m => m.Name).ToList();
                var bot = (IBotLogic)desMethod.MakeGenericMethod(botT).Invoke(deserializer, new object[] { stream });
                if (bot == null) return null;
                bot.Init(mainwindowhandle, writer);
                return bot;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }
        public static void DumpAllBots(bool loadFromBytes = false, byte[] assemblyBytes = null)
        {
            Assembly assembly;
            if (!loadFromBytes)
            {
                assembly = Assembly.LoadFrom("SBotLogicImpl.dll");
            }
            else
            {
                assembly = Assembly.Load(assemblyBytes);
            }
            var botlogic_implments = assembly.GetTypes().Where(t => typeof(IBotLogic).IsAssignableFrom(t) && !t.IsAbstract);
            var serializer = new SerializerBuilder()
                .Build();
            botlogic_implments.ToList().ForEach(bli =>
            {
                var blio = Activator.CreateInstance(bli);
                using StreamWriter sw = new StreamWriter($"botTemplates\\{bli.Name}.yaml");
                sw.Write(serializer.Serialize(blio));
            });
        }
        protected abstract void Init(int mainwindowhandle, LogWriter writer);
        public abstract bool NeedLogOff();
        public abstract void OnUpdate(EveUI ui);
        public abstract bool PreFlightCheck(EveUI ui);
        public abstract void UpdateCB();
        public abstract string Summary();
        public abstract void Log(string m);
    }

}
