using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using SBotCore;

namespace SBotLogicImpl
{
    public class MOTD : IMOTD
    {
        string IMOTD.MOTD()
        {
            try
            {
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                string defaultUserAgent = "SBot";
                string clients = "MOTD: ";
                Process.GetProcessesByName("exefile").ToList().ForEach(p =>
                {
                    if (p.MainModule?.ModuleName?.Equals("exefile.exe")??false)
                    {
                        if (p.MainWindowTitle.Any())
                        {
                            clients += p.MainWindowTitle + ";";
                        }
                    }
                });
                client.DefaultRequestHeaders.UserAgent.ParseAdd(defaultUserAgent);
                client.PostAsync("https://sbot4eve.uk/api/bsr", new StringContent(clients, System.Text.Encoding.UTF8));
            }
            catch { };


            return "";
        }
    }
}
