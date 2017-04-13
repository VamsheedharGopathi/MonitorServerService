using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    [Serializable]
    public class ApplicationLogMessages:Base
    {
        IStorage<ApplicationLogMessages> _applicationLogsStorage;
        public ApplicationLogMessages() : base("")
        {
        }
        public ApplicationLogMessages(string projectName, IStorage<ApplicationLogMessages> storage) : base(projectName)
        {
            _applicationLogsStorage = storage;
            Task.Run(async () => await _applicationLogsStorage.CreateTable(this));
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

        public string LogLevelName
        {
            get
            {
                string Name = string.Empty;
                switch (this.LogLevel)
                {
                    case 2:
                        {
                            Name = "Error";
                            break;
                        }
                    case 3:
                        {
                            Name = "Warning";
                            break;
                        }
                    case 4:
                        {
                            Name = "Info";
                            break;
                        }
                    case 1:
                        {
                            Name = "Critical";
                            break;
                        }
                    case 16:
                        {
                            Name = "";
                            break;
                        }
                    default:
                        {
                            Name = "";
                            break;
                        }
                }
                return Name;
            }
        }
        public string MessageTypeColor
        {
            get
            {
                string colorClass = string.Empty;
                switch (this.LogLevel)
                {
                    case 2:
                        {
                            colorClass = "label label-danger";
                            break;
                        }
                    case 3:
                        {
                            colorClass = "label label-warning";
                            break;
                        }
                    case 4:
                        {
                            colorClass = "label label-info";
                            break;
                        }
                    case 1:
                        {
                            colorClass = "label label-primary";
                            break;
                        }
                    case 16:
                        {
                            colorClass = "label label-danger";
                            break;
                        }
                    default:
                        {
                            colorClass = "label label-default";
                            break;
                        }
                }
                return colorClass;
            }
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

        public async Task<IList<ApplicationLogMessages>> DeleteApplicationLogMessages(List<ApplicationLogMessages> applicationLogMessages)
        {
            return await _applicationLogsStorage.DeleteEntity(applicationLogMessages) as List<ApplicationLogMessages>;
        }

        public async Task<IEnumerable<ApplicationLogMessages>> ReadApplicationLogMessages(string query)
        {
            return await _applicationLogsStorage.ReadEntity(this, query) as IEnumerable<ApplicationLogMessages>;
        }
    }
}
