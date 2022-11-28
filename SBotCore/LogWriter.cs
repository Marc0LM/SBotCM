using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;


namespace SBotCore
{
    public class LogWriter
    {

        private string log_name_ = "default";

        public LogWriter(string name)
        {
            log_name_ = name;
        }
        public void LogWrite(string logMessage)
        {
            var m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            using (StreamWriter w = File.AppendText(m_exePath + "\\logs\\" + log_name_+".log"))
            {
                w.WriteLine(logMessage);
            }
            Console.WriteLine(logMessage);
        }
    }
}
