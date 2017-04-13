using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    public class ApplicationLogs : Base
    {
        IStorage<ApplicationLogs> _applicationLogsStorage;
        public ApplicationLogs() : base("")
        {
        }
        public ApplicationLogs(string projectName, IStorage<ApplicationLogs> storage) : base(projectName)
        {
            _applicationLogsStorage = storage;
            _applicationLogsStorage.CreateTable(this);
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
        public string ParentSource
        {
            get;
            set;
        }
        public bool Error
        {
            get;
            set;
        }
        public bool Warnings
        {
            get;
            set;
        }
        public bool Information
        {
            get;
            set;
        }
        public bool Critical
        {
            get;
            set;
        }
       
        public async Task<IList<ApplicationLogs>> AddApplicationLog()
        {
            List<ApplicationLogs> collection = new List<ApplicationLogs>() { this };
            IList<ApplicationLogs> result = await _applicationLogsStorage.AddEntity(collection) as List<ApplicationLogs>;
            return result;
        }
        public async Task<IList<ApplicationLogs>> AddApplicationLogs(List<ApplicationLogs> collection)
        {
            IList<ApplicationLogs> result = await _applicationLogsStorage.AddEntity(collection) as List<ApplicationLogs>;
            return result;
        }
        public async Task<IList<ApplicationLogs>> UpdateApplicationLog()
        {
            return await _applicationLogsStorage.UpdateEntity(this) as List<ApplicationLogs>;
        }

        public async Task<IList<ApplicationLogs>> DeleteApplicationLog()
        {
            return await _applicationLogsStorage.DeleteEntity(this) as List<ApplicationLogs>;
        }

        public async Task<IEnumerable<ApplicationLogs>> ReadApplicationLog(string query)
        {
            try
            {
                return await _applicationLogsStorage.ReadEntity(this, query) as IEnumerable<ApplicationLogs>;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
