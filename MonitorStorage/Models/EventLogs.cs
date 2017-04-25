using MonitorStorage;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;


namespace MonitorStorage.Models
{
    public class EventLogs : Base
    {
        IStorage<EventLogs> _eventStorage;
        public EventLogs() : base("")
        { }
        public EventLogs(string projectName, IStorage<EventLogs> storage) : base(projectName)
        {
            _eventStorage = storage;
            _eventStorage.CreateTable(this);
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

        public async Task<IList<EventLogs>> DeleteLog()
        {
            return await _eventStorage.DeleteEntity(this) as List<EventLogs>;
        }

        public async Task<IEnumerable<EventLogs>> ReadLogs(string query)
        {
            return await _eventStorage.ReadEntity(this, query) as IEnumerable<EventLogs>;
        }

        public IEnumerable<EventLogs> SyncReadLogs(string query)
        {
            return _eventStorage.SynReadEntity(this, query) as IEnumerable<EventLogs>;
        }

        public void SyncAddLogs()
        {
            _eventStorage.SynAddEntity(this);
        }
        public void SyncUpdateLogs()
        {
            _eventStorage.SynUpdateEntity(this);
        }
    }
}
