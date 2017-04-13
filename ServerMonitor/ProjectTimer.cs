using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerMonitor
{
    public class ProjectTimer:System.Timers.Timer
    {
        public readonly string name;
        public ProjectTimer(string name)
        {
            this.name = name;
        }
        public string EventName
        {
            get;
            set;
        }
    }
}
