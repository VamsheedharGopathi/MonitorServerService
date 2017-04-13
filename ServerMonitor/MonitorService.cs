using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using MonitorStorage;
using MonitorStorage.Models;
using System.Messaging;
using System.Configuration;
using System.Xml;
using System.IO;
using System.Diagnostics.Eventing.Reader;


namespace ServerMonitor
{
    public partial class MonitorService : ServiceBase
    {
        private CloudStorageAccount _cloudStorageAccount;
        private List<ProjectTimer> projectTimerCollection;
        private ProjectTimer timer;
        private string Storage
        {
            get { return ConfigurationManager.AppSettings["storage"] != null ? ConfigurationManager.AppSettings["storage"].ToString() : ""; }
        }
        private string ProjectName {
            get { return ConfigurationManager.AppSettings["projectName"] != null ? ConfigurationManager.AppSettings["projectName"].ToString() : ""; }
        }
        private string ConfigurationPath
        {
            get { return ConfigurationManager.AppSettings["pathToRead"] != null ? ConfigurationManager.AppSettings["pathToRead"].ToString() : ""; }
        }
        public MonitorService()
        {
            InitializeComponent();
            _cloudStorageAccount = CloudStorageAccount.Parse(Storage);
        }
        protected override void OnStart(string[] args)
        {
            Task.Run(new Action(async () =>
            {
                await ScheduleTimer();
            }));
        }
        private async Task ScheduleTimer()
        {
            System.Threading.Thread.Sleep(20000);
            Storage<MonitoringTimer> monitoringTimerStorage = new Storage<MonitoringTimer>(_cloudStorageAccount);
            MonitoringTimer monitoringTimer = new MonitoringTimer(this.ProjectName, monitoringTimerStorage);
            IEnumerable<MonitoringTimer> monitoringTimerResult = await monitoringTimer.ReadMonitoringTimer($"PartitionKey eq '{this.ProjectName}'");
            projectTimerCollection = new List<ProjectTimer>();
            monitoringTimerResult.Where(t => t.CanRead == true).ToList().ForEach(t =>
            {
                timer = new ProjectTimer(this.ProjectName);
                timer.Interval = t.Minutes * 60 * 1000;
                timer.EventName = t.Name;
                timer.Elapsed += Main_Tick;
                timer.AutoReset = true;
                timer.Start();
                projectTimerCollection.Add(timer);
            });
        }
         void Main_Tick(object sender, EventArgs args)
        {
            ProjectTimer projectTimer = sender as ProjectTimer;
            MainFunction(projectTimer.EventName);
        }
        private void MainFunction(string Name)
        {
            Task.Run(new Action(async () =>
            {
                switch (Name)
                {
                    case "Event":
                        {
                            await EventLogs();
                            break;
                        }
                    case "Configuration":
                        {
                            await FileConfiguration();
                            break;
                        }
                    case "Queues":
                        {
                            await Queue();
                            break;
                        }
                    case "Services":
                        {
                            await AutoService();
                            break;
                        }
                }
            }));
        }
        protected override void OnStop()
        {
            if (projectTimerCollection != null)
            {
                projectTimerCollection.ForEach(t =>
                {
                    t.Stop();
                });
            }
        }

        #region Queue
        private async Task Queue()
        {
            try
            {
                Storage<Queues> queuesStorage = new Storage<Queues>(_cloudStorageAccount);
                List<Queues> queuesCollection = new List<Queues>();
                MessageQueue[] msgQueue = MessageQueue.GetPrivateQueuesByMachine(Environment.MachineName);
                var queue = from p in msgQueue select p;
                Queues queues = null;
                foreach (var q in queue)
                {
                    try
                    {
                        queues = new Queues(this.ProjectName, queuesStorage);
                        Message[] msgs = null;
                        MessageQueue messageQueue = null;
                        if (MessageQueue.Exists($".\\{q.QueueName}"))
                        {
                            messageQueue = new MessageQueue($".\\{q.QueueName}");
                        }
                        queues.Name = q.QueueName.Replace("private$\\", "");
                        queues.ID = DateTime.Now.Ticks.ToString();
                        queues.ClearMessages = false;
                        var resultQueues = await queues.ReadQueues($"PartitionKey eq '{this.ProjectName}' and Name eq '{queues.Name}'");
                        if (resultQueues != null && resultQueues.Any())
                        {
                            if (resultQueues.First().ClearMessages)
                            {
                                ClearMessage(queues.Name);
                            }
                            msgs = messageQueue.GetAllMessages();
                            resultQueues.First().ClearMessages = false;
                            resultQueues.First()._queueStorage = queuesStorage;
                            resultQueues.First().Count = msgs.Length;
                            await resultQueues.First().UpdateQueue();
                        }
                        else
                        {
                            msgs = messageQueue.GetAllMessages();
                            queues.Count = msgs.Length;
                            queuesCollection.Add(queues);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
                var result = queuesCollection.Any() ? await queues.AddQueues(queuesCollection) : null;
            }
            catch (Exception ex)
            {

            }
        }

        private void ClearMessage(string queueName)
        {
            Message[] msgs = null;
            MessageQueue messageQueue = new MessageQueue($".\\private$\\{queueName}");
            messageQueue.MessageReadPropertyFilter.AppSpecific = true;
            messageQueue.MessageReadPropertyFilter.ArrivedTime = true;
            messageQueue.MessageReadPropertyFilter.AttachSenderId = true;
            messageQueue.MessageReadPropertyFilter.Authenticated = true;
            messageQueue.MessageReadPropertyFilter.AuthenticationProviderName = true;
            messageQueue.MessageReadPropertyFilter.AuthenticationProviderType = true;
            messageQueue.MessageReadPropertyFilter.Body = true;
            var re = messageQueue.Peek(TimeSpan.FromSeconds(10.0));
            msgs = messageQueue.GetAllMessages();
            int i = 0;
            foreach (Message msg in msgs)
            {
                msg.Formatter = new XmlMessageFormatter(new Type[] { typeof(XmlElement) });
                msgs[i] = messageQueue.Receive();
                i++;
            }
        }
        #endregion

        #region Configuration
        private async Task FileConfiguration()
        {
            try
            {
                Storage<Configurations> configurationsStorage = new Storage<Configurations>(_cloudStorageAccount);
                DirectoryInfo directoryInfo = new DirectoryInfo($@"{ConfigurationPath}");
                FileInfo[] fileInfo = directoryInfo.GetFiles();
                Configurations configurations = null;
                List<Configurations> configurationsCollection = new List<Configurations>();
                foreach (FileInfo file in fileInfo)
                {
                    try
                    {
                        string fileName = file.Name.Replace($"{file.Name.Substring(file.Name.IndexOf('.'), file.Name.Length - file.Name.IndexOf('.'))}", "");
                        configurations = new Configurations(this.ProjectName, configurationsStorage);
                        configurations.Name = fileName;
                        configurations.UserName = configurations.UserName;
                        configurations.Format = file.Name.Substring(file.Name.IndexOf('.'), file.Name.Length - file.Name.IndexOf('.'));
                        configurations.Content = ReadFile(this.ConfigurationPath, configurations.Name, configurations.Format);
                        configurations.CanModified = false;
                        configurations.ID = DateTime.Now.Ticks.ToString();
                        var resultconfigurations = await configurations.ReadConfigurations($"PartitionKey eq '{this.ProjectName}' and Name eq '{configurations.Name}'");
                        if (resultconfigurations != null && resultconfigurations.Any())
                        {
                            resultconfigurations.First()._configurationStorage = configurationsStorage;
                            if (resultconfigurations.First().CanModified)
                            {
                                SaveFile(this.ConfigurationPath, resultconfigurations.First().Name, resultconfigurations.First().Format, resultconfigurations.First().Content);
                            }
                            //await resultconfigurations.First().UpdateConfiguration();
                        }
                        else
                        {

                            configurationsCollection.Add(configurations);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
                var result = configurationsCollection.Any() ? await configurationsStorage.AddEntity(configurationsCollection) : null;
            }
            catch (Exception ex)
            { }
        }

        private string ReadFile(string path,string fileName,string format)
        {
            string result = string.Empty;
            var fileStream = new FileStream($@"{path}\{fileName}{format}", FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                result = streamReader.ReadToEnd();
            }
            return result;
        }

        private void SaveFile(string path, string fileName, string format,string content)
        {
            try
            {
                using (StreamWriter newTask = new StreamWriter($@"{path}\{fileName}{format}", false))
                {
                    newTask.WriteLine(content);
                }
            }
            catch (Exception ex)
            { }
        }

        #endregion

        #region EventLogs

        private async Task EventLogs()
        {
            try
            {
                Storage<EventLogs> eventLogsStorage = new Storage<EventLogs>(_cloudStorageAccount);
                Storage<ApplicationLogs> applicationLogsStorage = new Storage<ApplicationLogs>(_cloudStorageAccount);
                Storage<ApplicationLogMessages> applicationLogMessagesStorage = new Storage<ApplicationLogMessages>(_cloudStorageAccount);
                // Remove Code
                Storage<EventLogLevel> eventLogLevelStorage = new Storage<EventLogLevel>(_cloudStorageAccount);
                EventLogLevel eventLogLevel = new EventLogLevel(this.ProjectName, eventLogLevelStorage);
                //
                List<EventLogs> eventLogsCollection = new List<EventLogs>();
                EventLogs eventLogs = null;
                EventLog[] eventLogsArray = EventLog.GetEventLogs();
                foreach (EventLog eventLog in eventLogsArray)
                {
                    try
                    {
                        eventLogs = new EventLogs(this.ProjectName, eventLogsStorage);
                        eventLogs.Name = eventLog.LogDisplayName;
                        eventLogs.MachineName = Environment.MachineName;
                        eventLogs.Source = eventLog.Source;
                        eventLogs.ID = DateTime.Now.Ticks.ToString();
                        var resultEventLogs = await eventLogs.ReadLogs($"PartitionKey eq '{this.ProjectName}' and Name eq '{eventLogs.Name}'");
                        if (resultEventLogs != null && resultEventLogs.Any())
                        {
                            if (resultEventLogs.First().CanRead)
                            {
                                var resultLevel = await eventLogLevel.ReadLogLevels($"PartitionKey eq '{this.ProjectName}' and ApplicationName eq '{eventLogs.Name}'");
                                if (resultLevel != null && resultLevel.Any())
                                {
                                    // List<ApplicationLogs> ApplicationLogsCollection = new List<ApplicationLogs>();
                                    ApplicationLogs applicationLogs = new ApplicationLogs(this.ProjectName, applicationLogsStorage);
                                    AddApplicationLogs(eventLogs.Name, resultLevel.First(), applicationLogs, applicationLogsStorage);
                                    var resultApplicationLogs = await applicationLogs.ReadApplicationLog($"PartitionKey eq '{this.ProjectName}' and ParentSource eq '{eventLogs.Name}' and (Error eq {(resultLevel.First().Error ? "true" : "false")} or Critical eq {(resultLevel.First().Critical ? "true" : "false")} or Information eq {(resultLevel.First().Information ? "true" : "false")} or Warning eq {(resultLevel.First().Warnings ? "true" : "false")})");
                                    if (resultApplicationLogs != null && resultApplicationLogs.Any())
                                    {
                                        ApplicationLogMessages applicationLogMessages = new ApplicationLogMessages(this.ProjectName, applicationLogMessagesStorage);
                                        await AddApplicationLogsMessages(resultApplicationLogs, applicationLogMessages, applicationLogMessagesStorage);
                                    }
                                }
                            }
                        }
                        else
                        {
                            eventLogs.CanRead = false;
                            eventLogsCollection.Add(eventLogs);
                        }
                    }
                    catch (Exception ex) { }
                }

                var result = eventLogsCollection.Any() ? await eventLogs.AddLogs(eventLogsCollection) : null;

            }
            catch (Exception ex)
            {
            }
        }

        private void AddApplicationLogs(string eventLogName,EventLogLevel eventLevel,ApplicationLogs applicationLogs, Storage<ApplicationLogs> Storage)
        {
            try
            {
                List<ApplicationLogs> holdResult = new List<ApplicationLogs>();
                //string query = $"*[({(eventLevel.Critical? "System/Level=1 or " : "")}{(eventLevel.Error ? "System/Level=2 or " : "")}{(eventLevel.Information ? "System/Level=4  or System/Level=0 or " : "")}{(eventLevel.Warnings ? "System/Level=3" : "")}) and System/TimeCreated/@SystemTime >= '" + DateTime.Now.AddDays(-1).ToUniversalTime().ToString("o") + "' and System/TimeCreated/@SystemTime <= '" + DateTime.Now.ToUniversalTime().ToString("o") + "']";
                string query = "*[(System/Level=1 or System/Level=2 or System/Level=3 or System/Level=4) and System/TimeCreated/@SystemTime > '" + DateTime.Now.AddDays(-1).ToUniversalTime().ToString("o") + "' and System/TimeCreated/@SystemTime <= '" + DateTime.Now.ToUniversalTime().ToString("o") + "']";
                EventLogQuery eventsQuery = new EventLogQuery(eventLogName, PathType.LogName, query);
                EventLogReader logReader = new EventLogReader(eventsQuery);
                for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
                {
                    if (!holdResult.Exists(l => l.Name == eventdetail.ProviderName))
                        holdResult.Add(new ApplicationLogs(this.ProjectName, Storage) {ID=DateTime.Now.Ticks.ToString(), Name = eventdetail.ProviderName, CanRead = false, MachineName = eventdetail.MachineName, ParentSource = eventLogName, Critical = false, Error = false, Information = false, Warnings = false });
                }
                holdResult.ForEach(async (a) =>
                {
                    var resultLevel = await applicationLogs.ReadApplicationLog($"PartitionKey eq '{this.ProjectName}' and Name eq '{a.Name}'");
                    if (resultLevel == null || !resultLevel.Any())
                    {
                        await a.AddApplicationLog();
                    }
                });
            }
            catch (Exception ex)
            {
               
            }
        }

        private async Task AddApplicationLogsMessages(IEnumerable<ApplicationLogs> applicationLogs, ApplicationLogMessages applicationLogMessages, Storage<ApplicationLogMessages> applicationLogMessagesStorage)
        {
            try
            {
                List<ApplicationLogs> holdResult = new List<ApplicationLogs>();
                DateTime fromDate = DateTime.Now.AddDays(-1);
                DateTime toDate = DateTime.Now;
                string query = string.Empty;
                foreach (ApplicationLogs applicationLog in applicationLogs)
                {
                    try
                    {
                        var resultLogMessages = await applicationLogMessages.ReadApplicationLogMessages($"PartitionKey eq '{this.ProjectName}' and Name eq '{applicationLog.Name}'");
                        if (resultLogMessages == null || !resultLogMessages.Any())
                        {
                            query = $"*[System/Provider/@Name='{applicationLog.Name}' and ({(applicationLog.Critical ? "System/Level=1 or " : "")}{(applicationLog.Error ? "System/Level=2 or " : "")}{(applicationLog.Information ? "System/Level=4  or System/Level=0" : "")}{(applicationLog.Warnings ? "or System/Level=3" : "")}) and System/TimeCreated/@SystemTime > '" + fromDate.ToUniversalTime().ToString("o") + "' and System/TimeCreated/@SystemTime <= '" + toDate.ToUniversalTime().ToString("o") + "']";
                        }
                        else
                        {
                            resultLogMessages = resultLogMessages.OrderByDescending(r => Convert.ToDateTime(r.MessageDate));
                            query = $"*[System/Provider/@Name='{applicationLog.Name}' and ({(applicationLog.Critical ? "System/Level=1 or " : "")}{(applicationLog.Error ? "System/Level=2 or " : "")}{(applicationLog.Information ? "System/Level=4  or System/Level=0" : "")}{(applicationLog.Warnings ? "or System/Level=3" : "")}) and System/TimeCreated/@SystemTime > '" + Convert.ToDateTime(resultLogMessages.First().MessageDate).ToUniversalTime().ToString("o") + "' and System/TimeCreated/@SystemTime <= '" + toDate.ToUniversalTime().ToString("o") + "']";
                        }
                        EventLogQuery eventsQuery = new EventLogQuery(applicationLog.ParentSource, PathType.LogName, query);
                        EventLogReader logReader = new EventLogReader(eventsQuery);
                        for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
                        {
                            ApplicationLogMessages LogMessages = new ApplicationLogMessages(this.ProjectName, applicationLogMessagesStorage) { ID = DateTime.Now.Ticks.ToString(), Message = eventdetail.FormatDescription(), Name = eventdetail.ProviderName, MachineName = eventdetail.MachineName, ParentSource = applicationLog.ParentSource, LogID = eventdetail.Id, LogLevel = (int)eventdetail.Level, MessageDate = eventdetail.TimeCreated.ToString() };
                            await LogMessages.AddApplicationLogMessage();
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        #endregion

        #region Services

        private async Task AutoService()
        {
            try
            {
                Storage<Services> servicesStorage = new Storage<Services>(_cloudStorageAccount);
                ServiceController[] services = ServiceController.GetServices(Environment.MachineName);
                List<Services> process = new List<Services>();
                Services serviceInstance = null;
                foreach (ServiceController service in services)
                {
                    if (!service.ServiceName.Equals("MonitorApplication"))
                    {
                        try
                        {
                            serviceInstance = new Services(this.ProjectName, servicesStorage) { ID = DateTime.Now.Ticks.ToString(), Name = service.ServiceName, DisplayName = service.DisplayName, MachineName = service.MachineName, ShowAction = (int)service.Status == 1 ? true : (int)service.Status == 4 ? true : false, Status = (int)service.Status, PerformAction = false };
                            var result = await serviceInstance.ReadServices($"PartitionKey eq '{this.ProjectName}' and Name eq '{service.ServiceName}'");
                            if (result == null || !result.Any())
                            {
                                process.Add(serviceInstance);
                            }
                            else if (result.Single().PerformAction && (int)service.Status != result.Single().Status)
                            {
                                IEnumerable<ServiceController> serviceStatus = from serv in services where serv.ServiceName == result.Single().Name select serv;
                                switch (result.Single().Status)
                                {
                                    case (int)ServiceControllerStatus.Stopped:
                                        {
                                            serviceStatus.First().Stop();
                                            result.Single().Status = (int)ServiceControllerStatus.Stopped;
                                            //result.Single().PerformAction = false;
                                            break;
                                        }
                                    case (int)ServiceControllerStatus.Running:
                                        {
                                            serviceStatus.First().Start();
                                            result.Single().Status = (int)ServiceControllerStatus.Running;
                                            //result.Single().PerformAction = false;
                                            break;
                                        }
                                }
                                result.Single().ShowAction = true;
                                result.Single()._servicesStorage = servicesStorage;
                                await result.Single().UpdateService();
                            }
                            else
                            {
                                if((int)service.Status != result.Single().Status)
                                result.Single().Status = (int)service.Status == (int)ServiceControllerStatus.Stopped? (int)ServiceControllerStatus.Running: (int)ServiceControllerStatus.Stopped;
                                result.Single().ShowAction = true;
                                result.Single()._servicesStorage = servicesStorage;
                                await result.Single().UpdateService();
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
                await serviceInstance.AddServices(process);
            }
            catch (Exception ex)
            {

            }
        }
        #endregion

      
    }
}
