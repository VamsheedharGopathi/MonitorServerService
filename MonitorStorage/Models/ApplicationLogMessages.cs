using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    public class ApplicationLogMessages:Base
    {
        IStorage<ApplicationLogMessages> _applicationLogsStorage;
        public ApplicationLogMessages() : base("")
        {
        }
        public ApplicationLogMessages(string projectName, IStorage<ApplicationLogMessages> storage) : base(projectName)
        {
            _applicationLogsStorage = storage;
            _applicationLogsStorage.CreateTable(this);
        }
        public string Name
        {
            get;
            set;
        }
        public string Message
        {
            get;
            set;
        }
        public string MessageDate
        {
            get;
            set;
        }
        public string MachineName
        {
            get;
            set;
        }
        public string ParentSource
        {
            get;
            set;
        }
        public int LogLevel
        {
            get;
            set;
        }
        public int LogID
        {
            get;
            set;
        }
        public async Task<IList<ApplicationLogMessages>> AddApplicationLogMessage()
        {
            List<ApplicationLogMessages> collection = new List<ApplicationLogMessages>() { this };
            IList<ApplicationLogMessages> result = await _applicationLogsStorage.AddEntity(collection) as List<ApplicationLogMessages>;
            return result;
        }
        public async Task<IList<ApplicationLogMessages>> AddApplicationLogMessages(List<ApplicationLogMessages> collection)
        {
            IList<ApplicationLogMessages> result = await _applicationLogsStorage.AddEntity(collection) as List<ApplicationLogMessages>;
            return result;
        }
        public async Task<IList<ApplicationLogMessages>> UpdateApplicationLogMessages()
        {
            return await _applicationLogsStorage.UpdateEntity(this) as List<ApplicationLogMessages>;
        }

        public async Task<IList<ApplicationLogMessages>> DeleteApplicationLogMessages()
        {
            return await _applicationLogsStorage.DeleteEntity(this) as List<ApplicationLogMessages>;
        }

        public async Task<IEnumerable<ApplicationLogMessages>> ReadApplicationLogMessages(string query)
        {
            return await _applicationLogsStorage.ReadEntity(this, query) as IEnumerable<ApplicationLogMessages>;
        }
    }
}
