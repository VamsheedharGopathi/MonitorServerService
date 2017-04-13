using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    public class MonitoringTimer : Base
    {
        public IStorage<MonitoringTimer> _monitoringTimerStorage;
        public MonitoringTimer() : base("")
        { }
        public MonitoringTimer(string projectName, IStorage<MonitoringTimer> storage) : base(projectName)
        {
            _monitoringTimerStorage = storage;
            _monitoringTimerStorage.CreateTable(this);
        }

        public string Name { get; set; }
        public int Minutes { get; set; }
        public bool CanRead { get; set; }
        
        public async Task<IList<MonitoringTimer>> AddMonitoringTimer()
        {
            List<MonitoringTimer> collection = new List<MonitoringTimer>() { this };
            IList<MonitoringTimer> result = await _monitoringTimerStorage.AddEntity(collection) as List<MonitoringTimer>;
            return result;
        }
        public async Task<IList<MonitoringTimer>> AddMonitoringTimer(List<MonitoringTimer> collection)
        {
            IList<MonitoringTimer> result = await _monitoringTimerStorage.AddEntity(collection) as List<MonitoringTimer>;
            return result;
        }

        public async Task<IList<MonitoringTimer>> UpdateMonitoringTimer()
        {
            return await _monitoringTimerStorage.UpdateEntity(this) as IList<MonitoringTimer>;
        }
        public async Task<IEnumerable<MonitoringTimer>> ReadMonitoringTimer(string query)
        {
            return await _monitoringTimerStorage.ReadEntity(this, query) as IEnumerable<MonitoringTimer>;
        }
    }
}
