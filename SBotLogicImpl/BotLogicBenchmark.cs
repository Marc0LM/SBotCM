using SBotCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBotLogicImpl
{
    public class BotLogicBenchmark : BotLogic
    {
        Stopwatch sw=new();
        long durSum = 0;
        int iter = 0;
        long start;
        public override string GetBotSummary()
        {
            if (iter == 0)
            {
                start = sw.ElapsedMilliseconds;
            }
            durSum = sw.ElapsedMilliseconds - start;
            return (durSum/iter).ToString();
        }

        public override void OnUpdate()
        {
            sw.Start();
            iter++;
            return;
        }
    }
}
