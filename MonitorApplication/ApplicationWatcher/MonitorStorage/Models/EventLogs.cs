using System;
using MonitorStorage;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;


namespace MonitorStorage.Models
{
    [Serializable]
    public class EventLogs : Base
    {
        IStorage<EventLogs> _eventStorage;
        public EventLogs() : base("")
        { }
        public EventLogs(string projectName, IStorage<EventLogs> storage) : base(projectName)
        {
            _eventStorage = storage;
            Task.Run(async () => await _eventStorage.CreateTable(this));
        }
        public string Name
        {
            get;
            set;
        }
        public bool CanRead
        {
            get;
            set;
        }
        public string MachineName
        {
            get;
            set;
        }
        public string Source
        {
            get;
            set;
        }
       
        public async Task<IList<EventLogs>> AddLog()
        {
            List<EventLogs> collection = new List<EventLogs>() { this };
            IList<EventLogs> result = await _eventStorage.AddEntity(collection) as List<EventLogs>;
            return result;
        }
        public async Task<IList<EventLogs>> AddLogs(List<EventLogs> collection)
        {
            IList<EventLogs> result = await _eventStorage.AddEntity(collection) as List<EventLogs>;
            return result;
        }
        public async Task<IList<EventLogs>> UpdateLog()
        {
            return await _eventStorage.UpdateEntity(this) as List<EventLogs>;
        }

        public async Task<IList<EventLogs>> DeleteLog(List<EventLogs> EventLogs)
        {
            return await _eventStorage.DeleteEntity(EventLogs) as List<EventLogs>;
        }

        public async Task<IEnumerable<EventLogs>> ReadLogs(string query)
        {
            return await _eventStorage.ReadEntity(this, query) as IEnumerable<EventLogs>;
        }
    }
}
