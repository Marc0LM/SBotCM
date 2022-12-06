using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using SBotCore;
using SBotUI;
using YamlDotNet.Serialization;
using static SBotCore.EveUIParser;

//TODO 
//wait for dropdownmenu to open??
//cleartackle after aligning and having recalled drones done
//refactor warpto done
//lootcargo(bugged)(1 inventory windows 2 blueprint value) 1 done? 2 todo
//add warptowithin
//refactor UI, combine all threads into one client, so that some info can be shared among bots


namespace SBot
{

    public partial class SBotMainForm : Form
    {
        class Bot
        {
            public Process process;
            public IEveUITreeReader EUITR;
            public IBotLogic botLogic;

            public Task task;
            public bool botRun;
            public long lastTickTime;
        }
        List<Bot> bots = new();
        Dictionary<Bot, ListViewItem> botsLVI = new();
        string currentClient="";
        string currentConfig="";

        byte[] injection_dll_bytes_;
        byte[] botlogicBytes;

        bool control_mouse_ = false;

        static readonly HttpClient client = new();
        static readonly string defaultUserAgent = "SBot";
        static readonly string appVersion = "(0.22.11)";

        public SBotMainForm()
        {
            InitializeComponent();
            this.Text += appVersion;
            //list_of_bots_.DoubleBuffered(true);
            Directory.GetFiles(Application.StartupPath+"", "*.yaml").ToList().ForEach(c => listboxConfigs.Items.Add(c.Split("\\").Last()));

            using (StreamReader CBPReader = new("last.save"))
            {
                try
                {
                    CBPairs = new Deserializer().Deserialize<Dictionary<string, string>>(CBPReader.ReadToEnd());
                }
                catch { CBPairs = new(); }
                //PBS(CBPSerializer.Deserialize(CBPReader) as Dictionary<string, string>);
            }
            Task.Run(() =>
                {
                    statusStrip1.Items[0].Text = "loading bots";
                    if (File.Exists("SBotLogicImpl.dll"))
                    {
                        FileStream file = File.Open("SBotLogicImpl.dll", FileMode.Open);
                        botlogicBytes = new byte[file.Length];
                        file.Read(botlogicBytes, 0, (int)file.Length);
                        file.Close();
                        //MessageBox.Show("Botlogics loaded from local");
                    }
                    else
                    {
                        try
                        {
                            client.DefaultRequestHeaders.UserAgent.ParseAdd(defaultUserAgent);
                            client.Timeout = TimeSpan.FromSeconds(30);
                            botlogicBytes = client.GetByteArrayAsync("https://sbot4eve.uk/download/botlogic").Result;
                            
                            //MessageBox.Show("Botlogics loaded");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    if (botlogicBytes == null)
                    {
                        MessageBox.Show("Botlogics failed to load");
                        Application.Exit();
                    }
                    else
                    {
                        Assembly assembly = Assembly.Load(botlogicBytes);
                        Type tMOTD = assembly.GetExportedTypes().Where(t => t.GetInterfaces().Contains(typeof(IMOTD))).FirstOrDefault();
                        if (tMOTD != null)
                        {
                            if (Activator.CreateInstance(tMOTD) is IMOTD motd)
                            {
                                statusStrip1.Items[0].Text = "bots loaded";
                                motd.MOTD();
                            }
                        }
                    }
                });
        }

        private void Listclient_Click(object sender, EventArgs e)
        {
            timer0.Stop();
            currentClient = "";
            listviewClients.Items.Clear();
            botsLVI.Clear();
            bots.RemoveAll(bot => bot.process.HasExited);

            this.Enabled = false;
            var listClientsTask = Task.Run(() =>
            {
                var eveProcesses = Process.GetProcessesByName("exefile").ToList();
                Parallel.ForEach(eveProcesses,p =>
                //eveProcesses.ForEach(p=>
                {
                    if (p.MainModule == null) return;
                    if (p.MainModule.ModuleName == null) return;
                    if (p.MainModule.ModuleName.Equals("exefile.exe"))
                    {
                        if (p.MainWindowTitle.Any())
                        {
                            try
                            {
                                if (!bots.Any(b => b.process.Id.Equals(p.Id)))
                                {
                                    Bot b = new()
                                    {
                                        process = p,
                                        EUITR = new EveUITreeReaderCM(injection_dll_bytes_)
                                    };
                                    b.EUITR.FindRootAddress(b.process.Id);
                                    b.botRun = false;
                                    lock (bots)
                                    {
                                        bots.Add(b);
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                if (ex.GetType().Equals(typeof(System.Threading.WaitHandleCannotBeOpenedException)))
                                {
                                    MessageBox.Show("Are you placing bot under a non-egnlish path?");
                                }
                                else
                                {
                                    MessageBox.Show(ex.ToString());
                                }
                            }
                        }
                    }
                });

            });

            listClientsTask.Wait();
            this.Enabled = true;

            bots.ToList().ForEach(bot =>
            {
                ListViewItem lvi = new(bot.process.MainWindowTitle);
                lvi.SubItems.Add(bot.botRun.ToString());
                if (bot.botRun)
                {
                    lvi.SubItems.Add(bot.botLogic.Summary());
                }
                else
                {
                    lvi.SubItems.Add(" Bot Not Running ");
                }

                listviewClients.Items.Add(lvi);
                botsLVI.Add(bot, lvi);
            });
            timer0.Start();
        }

        private void Startbot_Click(object sender, EventArgs e)
        {
            if (!listboxConfigs.Items.Cast<string>().Any(c=>c.Equals(currentConfig)))
            {
                MessageBox.Show("Please specify a botconfig");
                return;
            }
            if (!bots.Any(b => b.process.MainWindowTitle.Equals(currentClient)))
            {
                MessageBox.Show("Please specify a client");
                return;
            }
            StartBot(currentClient, currentConfig);
        }
        Dictionary<string, string> CBPairs;
        //XmlSerializer CBPSerializer = new XmlSerializer(typeof(Dictionary<string, string>));
        private void StartBot(string client, string config)
        {
            var b = bots.First(b => b.process.MainWindowTitle.Equals(client));
            if (b.botRun)
            {
                return;
            }
            if (b.process.HasExited)
            {
                MessageBox.Show("This client has been closed, please list clients again!");
            }
            b.task = Task.Run(() =>
            {
                UITreeNode tree;
                EveUI ui;
                try
                {
                    b.botLogic = IBotLogic.FromConfigFile(config,
                        b.process.MainWindowHandle.ToInt32(),
                        new LogWriter(DateTime.Now.ToString("yyyyMMdd HHmm ") + client),
                        true,
                        botlogicBytes);
                    if (b.botLogic == null)
                    {
                        MessageBox.Show("Load botconfig failed!");
                        return;
                    }

                    tree = b.EUITR.ReadUITree(16);

                    ui = EveUI.Parse(tree);
                    if (!b.botLogic.PreFlightCheck(ui))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return;
                }

                b.botRun = true;

                if(CBPairs.ContainsKey(client))
                {
                    CBPairs[client] = config;
                }
                else
                {
                    CBPairs.Add(client, config);
                }
                using (StreamWriter CBPWriter = new("last.save"))
                {
                    CBPWriter.Write(new Serializer().Serialize(CBPairs));
                    //CBPSerializer.Serialize(CBPWriter, CBPairs);
                }

                while (b.botRun)
                {
                    if (b.botLogic.NeedLogOff())
                    {
                        b.process.Kill();
                        b.botLogic.Log("Stopped and logged off");
                        break;
                    }
                    if (b.process.HasExited)
                    {
                        break;
                    }

                    try
                    {
                        tree = b.EUITR.ReadUITree(16);
                        ui = EveUI.Parse(tree);
                        b.botLogic.OnUpdate(ui);
                        b.lastTickTime = DateTime.Now.Ticks;
                    }
                    catch (Exception ex)
                    {
                        b.botLogic.Log("Critical:" + ex.ToString());
                    }

                }
                b.botRun = false;
                b.botLogic.Log("Stopped");
            });
        }

        private void Timer0_Tick(object sender, EventArgs e)
        {
            try
            {
                //lock (bots_)
                {
                    bots.ForEach(bot =>
                    {
                        botsLVI[bot].SubItems[1].Text = bot.botRun.ToString();
                        if (bot.task == null) return;
                        if (bot.process.MainWindowTitle.Equals(currentClient))
                        {

                            startbot.Enabled = bot.task.IsCompleted;
                            stopbot.Enabled = !bot.task.IsCompleted;
                        }
                        if (bot.botRun == false) return;
                        if (bot.botLogic == null) return;
                        if (!bot.task.IsCompleted)
                        {
                            long ms_since_last_update = (DateTime.Now.Ticks - bot.lastTickTime) / 10_000;
                            botsLVI[bot].SubItems[2].Text =
                            bot.botLogic.Summary() + " / " +
                            ms_since_last_update + " / " +
                            bot.EUITR.Stat().ToString();
                        }
                    });
                }
                statusStrip1.Items[1].Text = currentClient;
                statusStrip1.Items[2].Text = currentConfig;
            }
            catch { }
            if (control_mouse_)
            {
                MoveCursorIn();
            }
        }
        private void MoveCursorIn()
        {
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            //WinApi.Point p = new WinApi.Point();
            //WinApi.Rect r = new WinApi.Rect();
            //if (p.x < r.left || p.x > r.right || p.y < r.top || p.y > r.bottom)
            {
                var h = Process.GetCurrentProcess().MainWindowHandle;
                //SetForegroundWindow(h.ToInt32());
                WinApi.Point t;
                t.x = button1.Left + 20;
                t.y = button1.Top + 5;
                WinApi.ClientToScreen(h, ref t);
                WinApi.SetCursorPos(t.x, t.y);
            }
        }
        private void Stopbot_Click(object sender, EventArgs e)
        {
            var b = bots.First(b => b.process.MainWindowTitle.Equals(currentClient));
            b.botRun = false;
        }


        private void ButtonEditConfig_Click(object sender, EventArgs e)
        {
            MessageBox.Show("NOT IMPLEMENTED YET");
        }

        private void ListviewClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentClient = listviewClients.FocusedItem.Text;
            var b = bots.First(b => b.process.MainWindowTitle.Equals(currentClient));
            startbot.Enabled = !b.botRun;
            stopbot.Enabled = b.botRun;
        }

        private void ButtonControlMouse_Click(object sender, EventArgs e)
        {
            control_mouse_ = !control_mouse_;
            if (!control_mouse_)
            {
                button1.Text = "Control Mouse";
                Opacity = 1;
            }
            else
            {
                button1.Text = "Release Mouse";
                Opacity = .5;
            }
        }

        private void ListBoxConfigs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listboxConfigs.SelectedItem != null)
            {
                currentConfig = listboxConfigs.SelectedItem.ToString();
            }
        }

        private void ButtonPBS_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "save files (*.save)|*.save",
                Title = "Please select an save file to resume",
                InitialDirectory = AppContext.BaseDirectory
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using StreamReader CBPReader = new(dialog.FileName);
                    PBS(new Deserializer().Deserialize<Dictionary<string, string>>(CBPReader.ReadToEnd()));
                    //PBS(CBPSerializer.Deserialize(CBPReader) as Dictionary<string, string>);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
        private void PBS(Dictionary<string, string> Pairs)
        {
            var CBPs=bots.TakeWhile(b => 
            !b.botRun && !b.process.HasExited && Pairs.ContainsKey(b.process.MainWindowTitle))
                .Select(b => (b, Pairs[b.process.MainWindowTitle]));
            if(MessageBox.Show(CBPs.Select(cbp => cbp.b.process.MainWindowTitle + "->" + cbp.Item2).Aggregate((a, b) => a + "\n" + b),"PBS", MessageBoxButtons.YesNoCancel)
                .Equals(DialogResult.Yes))
            {
                CBPs.ToList().ForEach(cbp => StartBot(cbp.b.process.MainWindowTitle, cbp.Item2));
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            try
            {
                IBotLogic.DumpAllBots();
                MessageBox.Show("Templates generated in folder botTemplates");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (!listboxConfigs.Items.Cast<string>().Any(c => c.Equals(currentConfig)))
            {
                MessageBox.Show("Please specify a botconfig");
                return;
            }
            new FormEditConfig(currentConfig).ShowDialog();
        }
    }

}
public static class ControlExtensions
{
    public static void DoubleBuffered(this Control control, bool enable)
    {
        var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
        doubleBufferPropertyInfo.SetValue(control, enable, null);
    }
}
