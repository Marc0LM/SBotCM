using SBotCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static SBotCore.EveUIParser;
using System.Xml.Linq;
using System.Xml.Serialization;
using WMPLib;
using YamlDotNet.Serialization;

namespace SBotLogicImpl
{
    public abstract class BotLogic : IBotLogic
    {
        static readonly int versionCode = 221122;
        public int versionConfig = 221122;
        public string BotType { get => this.GetType().Name; set => new Object(); }

        [YamlIgnore]
        public LogWriter? logWriter;

        protected IInput? input;


        protected EveUI? ui;
        //public static void DumpAllBots(bool loadFromBytes = false, byte[] assemblyBytes = null)
        //{
        //    Assembly assembly;
        //    if (!loadFromBytes)
        //    {
        //        assembly = Assembly.LoadFrom("SBotLogicImpl.dll");
        //    }
        //    else
        //    {
        //        assembly = Assembly.Load(assemblyBytes);
        //    }
        //    var botlogic_implments = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(BotLogic)) && !t.IsAbstract);
        //    var serializer = new SerializerBuilder()
        //        .Build();
        //    botlogic_implments.ToList().ForEach(bli =>
        //    {
        //        var blio=Activator.CreateInstance(bli);
        //        using StreamWriter sw = new StreamWriter($"botTemplates\\{bli.Name}.yaml");
        //        sw.Write(serializer.Serialize(blio));
        //    });
        //}
        //public static BotLogic FromConfigFile(string file_name, int mainwindowhandle, LogWriter writer, bool load_from_bytes = false, byte[] assembly_bytes = null)
        //{
        //    Assembly assembly;
        //    if (!load_from_bytes)
        //    {
        //        assembly = Assembly.LoadFrom("SBotLogicImpl.dll");
        //    }
        //    else
        //    {
        //        assembly = Assembly.Load(assembly_bytes);
        //    }
        //    //DumpAllBots(load_from_bytes, assembly_bytes);
        //    var botlogic_implments = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(BotLogic)) && !t.IsAbstract);

        //    try
        //    {
        //        using StreamReader sr = new StreamReader(file_name);
        //        var deserializer = new DeserializerBuilder()
        //            .Build();
        //        string stream = sr.ReadToEnd();
        //        var flatView = deserializer.Deserialize<Dictionary<string, object>>(stream);
        //        if (!flatView.ContainsKey("BotType"))
        //        {
        //            return null;
        //        }
        //        string bot_name = (string)flatView["BotType"];
        //        var botT = botlogic_implments.FirstOrDefault(bli => bli.Name.Equals(bot_name));
        //        var desMethod = typeof(Deserializer).GetMethod("Deserialize", new[] { typeof(string) });
        //        //var methods = typeof(Deserializer).GetMethods();//.Select(m => m.Name).ToList();
        //        var bot = (BotLogic)desMethod.MakeGenericMethod(botT).Invoke(deserializer, new object[] { stream });
        //        if (bot == null) return null;
        //        if (versionCode != bot.versionConfig)
        //        {
        //            if (MessageBox.Show("Bot config version mismatch! Continue?", "WARNING", MessageBoxButtons.YesNo) != DialogResult.Yes)
        //            {
        //                return null;
        //            }
        //        }
        //        bot.input = new InputMsg(mainwindowhandle);
        //        bot.logWriter = writer;
        //        return bot;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}
        public static BotLogic FromConfigFileDecre(string file_name, int mainwindowhandle, LogWriter writer, bool load_from_bytes = false, byte[] assembly_bytes = null)
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
            IBotLogic.DumpAllBots(load_from_bytes, assembly_bytes);
            var botlogic_implments = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(BotLogic)));
            string bot_name;
            try
            {
                XDocument bl = XDocument.Load(file_name);
                bot_name = bl.Root.Name.LocalName;
                var botlogic = botlogic_implments.FirstOrDefault(bli => bli.Name.Equals(bot_name));
                if (botlogic != null)
                {
                    XmlSerializer serializer = new(botlogic);
                    using StreamReader sr = new(file_name);
                    BotLogic bot;
                    bot = (BotLogic)serializer.Deserialize(sr);
                    bot.input = new InputMsg(mainwindowhandle);
                    bot.logWriter = writer;
                    return bot;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public virtual bool NeedLogOff()
        {
            return false;
        }

        public void OnUpdate(EveUI ui)
        {
            this.ui = ui;
            UpdateCB();
        }
        protected WindowsMediaPlayer alertWarningPlayer = new();
        protected WindowsMediaPlayer dcWarningPlayer = new();
        protected WindowsMediaPlayer essWarningPlayer = new();
        public virtual bool PreFlightCheck(EveUI ui)
        {
            alertWarningPlayer.settings.autoStart = false;
            dcWarningPlayer.settings.autoStart = false;
            essWarningPlayer.settings.autoStart = false;

            alertWarningPlayer.URL = "w_hostile.mp3";
            dcWarningPlayer.URL = "w_dc.mp3";
            essWarningPlayer.URL = "w_ess.mp3";



            return true;
        }
        public abstract void UpdateCB();
        public abstract string Summary();

        void IBotLogic.Init(int mainwindowhandle, LogWriter writer)
        {
            if (versionCode != versionConfig)
            {
                if (MessageBox.Show("Bot config version mismatch! Continue?", "WARNING", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    throw new Exception("Bot version mismatch");
                }
            }
            input = new InputMsg(mainwindowhandle);
            logWriter = writer;
        }

        public void Log(string m)
        {
            if (logWriter != null)
            {
                logWriter.LogWrite(m);
            }
            else
            {
                MessageBox.Show(m);
            }
        }
    }
}
