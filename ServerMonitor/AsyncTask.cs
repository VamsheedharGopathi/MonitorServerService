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
    public class AsyncTask
    {
        private CloudStorageAccount _cloudStorageAccount;
       
        private string ProjectName
        {
            get { return ConfigurationManager.AppSettings["projectName"] != null ? ConfigurationManager.AppSettings["projectName"].ToString() : ""; }
        }
        private string ConfigurationPath
        {
            get { return ConfigurationManager.AppSettings["pathToRead"] != null ? ConfigurationManager.AppSettings["pathToRead"].ToString() : ""; }
        }

        public AsyncTask(CloudStorageAccount cloudStorageAccount)
        {
            _cloudStorageAccount = cloudStorageAccount;
        }
        #region MainFunction
        public async Task MainFunction(string Name)
        {
            Task task = Task.Run(new Action(async () =>
            {
                switch (Name)
                {
                    case "Event":
                        {
                            await WriteLog($"MainFunction----{Name}").ConfigureAwait(false);
                            await EventLogs().ConfigureAwait(false);
                            break;
                        }
                    case "Configuration":
                        {
                            await WriteLog($"MainFunction----{Name}").ConfigureAwait(false);
                            await FileConfiguration().ConfigureAwait(false);
                            break;
                        }
                    case "Queues":
                        {
                            await WriteLog($"MainFunction----{Name}").ConfigureAwait(false);
                            await Queue().ConfigureAwait(false);
                            break;
                        }
                    case "Services":
                        {
                            await WriteLog($"MainFunction----{Name}").ConfigureAwait(false);
                            await AutoService().ConfigureAwait(false);
                            break;
                        }
                }
            }));
           await task.ConfigureAwait(false);
        }
        #endregion

        #region Queue
        private async Task Queue()
        {
            try
            {
                await WriteLog("Queue Start").ConfigureAwait(false);
                Storage<Queues> queuesStorage = new Storage<Queues>(_cloudStorageAccount);
                MessageQueue[] msgQueue = MessageQueue.GetPrivateQueuesByMachine(Environment.MachineName);
                //var queue = from p in msgQueue select p;
                Queues queues = new Queues(this.ProjectName, queuesStorage);
                var resultQueues = await queues.ReadQueues($"PartitionKey eq '{this.ProjectName}'").ConfigureAwait(false);
                //var filterqueue = from rq in resultQueues join q in msgQueue on rq.Name equals q.QueueName.Replace("private$\\", "") select q;
                //var queue = filterqueue.Any()? msgQueue.Except(filterqueue): msgQueue;
                List<Queues> queuesCollection = await QueueCollection(msgQueue, queuesStorage, resultQueues).ConfigureAwait(false);
                await WriteLog($"ADD Queues {queuesCollection.Count()}").ConfigureAwait(false);
                var result = queuesCollection.Any() ? await queues.AddQueues(queuesCollection).ConfigureAwait(false) : null;
            }
            catch (Exception ex)
            {
                await WriteLog($"Queue Exception {ex.Message}").ConfigureAwait(false);
            }
        }

        private async Task<List<Queues>> QueueCollection(IEnumerable<MessageQueue> queue, Storage<Queues> queuesStorage, IEnumerable<Queues> resultQueues)
        {
            List<Queues> queuesCollection = new List<Queues>();
            Queues queues = null;
            foreach (var q in queue)
            {
                try
                {
                    await WriteLog($"QueueName {q.QueueName}").ConfigureAwait(false);
                    queues = new Queues(this.ProjectName, queuesStorage);
                    MessageQueue messageQueue = null;
                    if (MessageQueue.Exists($".\\{q.QueueName}"))
                    {
                        messageQueue = new MessageQueue($".\\{q.QueueName}");
                    }
                    queues.Name = q.QueueName.Replace("private$\\", "");
                    queues.ID = DateTime.Now.Ticks.ToString();
                    queues.ClearMessages = false;
                    var resqueue = from r in resultQueues where r.Name == queues.Name select r;
                    if (resultQueues != null && resqueue.Any())
                    {
                        if (resultQueues.First().ClearMessages)
                        {
                            await ClearMessage(queues.Name).ConfigureAwait(false);
                        }
                        resultQueues.First().ClearMessages = false;
                        resultQueues.First()._queueStorage = queuesStorage;
                        resultQueues.First().Count = GetQueueCount(messageQueue);
                        await resultQueues.First().UpdateQueue().ConfigureAwait(false);
                    }
                    else
                    {
                        queues.Count = GetQueueCount(messageQueue);
                        queuesCollection.Add(queues);
                    }
                }
                catch (Exception ex)
                {
                    await WriteLog($"QueueCollection {ex.Message}").ConfigureAwait(false);
                }
            }
            await WriteLog($"return QueueCollection {queuesCollection.Count()}").ConfigureAwait(false);
            return queuesCollection;
        }

        private int GetQueueCount(MessageQueue messageQueue)
        {
            Message[] msgs = null;
            Task task = new Task(() => { msgs = messageQueue.GetAllMessages(); });
            task.Start();
            task.Wait();
            return msgs.Length;
        }

        private async Task ClearMessage(string queueName)
        {
            Task task = new Task(() =>
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
            });
            task.Start();
            await task.ConfigureAwait(false);
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
                Configurations configurations = new Configurations(this.ProjectName, configurationsStorage);
                var resultconfigurations = await configurations.ReadConfigurations($"PartitionKey eq '{this.ProjectName}'").ConfigureAwait(false);
                List<Configurations> configurationsCollection = await ConfigurationsCollection(fileInfo, configurationsStorage, resultconfigurations).ConfigureAwait(false);
                var result = configurationsCollection.Any() ? await configurationsStorage.AddEntity(configurationsCollection).ConfigureAwait(false) : null;
            }
            catch (Exception ex)
            {
                await WriteLog(ex.Message).ConfigureAwait(false);
            }
        }

        private async Task<List<Configurations>> ConfigurationsCollection(FileInfo[] fileInfo, Storage<Configurations> configurationsStorage, IEnumerable<Configurations> Configurations)
        {
            List<Configurations> configurationsCollection = new List<Configurations>();
            Configurations configurations = null;
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
                    var resultconfigurations = from c in Configurations where c.Name == configurations.Name select c;
                    if (resultconfigurations != null && resultconfigurations.Any())
                    {
                        resultconfigurations.First()._configurationStorage = configurationsStorage;
                        if (resultconfigurations.First().CanModified)
                        {
                            await SaveFile(this.ConfigurationPath, resultconfigurations.First().Name, resultconfigurations.First().Format, resultconfigurations.First().Content).ConfigureAwait(false);
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
                    await WriteLog(ex.Message).ConfigureAwait(false);
                }
            }
            return configurationsCollection;
        }

        private string ReadFile(string path, string fileName, string format)
        {
            string result = string.Empty;
            Task task = new Task(() =>
            {
                var fileStream = new FileStream($@"{path}\{fileName}{format}", FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    result = streamReader.ReadToEnd();
                }
            });
            task.Start();
            task.Wait();
            return result;
        }

        private async Task SaveFile(string path, string fileName, string format, string content)
        {
            Task task = new Task(async () =>
            {
                try
                {
                    using (StreamWriter newTask = new StreamWriter($@"{path}\{fileName}{format}", false))
                    {
                        newTask.WriteLine(content);
                    }
                }
                catch (Exception ex)
                {
                    await WriteLog(ex.Message).ConfigureAwait(false);
                }
            });
            task.Start();
            await task.ConfigureAwait(false);
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
                        var resultEventLogs = await eventLogs.ReadLogs($"PartitionKey eq '{this.ProjectName}' and Name eq '{eventLogs.Name}'").ConfigureAwait(false);
                        if (resultEventLogs != null && resultEventLogs.Any())
                        {
                            if (resultEventLogs.First().CanRead)
                            {
                                var resultLevel = await eventLogLevel.ReadLogLevels($"PartitionKey eq '{this.ProjectName}' and ApplicationName eq '{eventLogs.Name}'").ConfigureAwait(false);
                                if (resultLevel != null && resultLevel.Any())
                                {
                                    // List<ApplicationLogs> ApplicationLogsCollection = new List<ApplicationLogs>();
                                    ApplicationLogs applicationLogs = new ApplicationLogs(this.ProjectName, applicationLogsStorage);
                                    await AddApplicationLogs(eventLogs.Name, resultLevel.First(), applicationLogs, applicationLogsStorage).ConfigureAwait(false);
                                    var resultApplicationLogs = await applicationLogs.ReadApplicationLog($"PartitionKey eq '{this.ProjectName}' and ParentSource eq '{eventLogs.Name}' and (Error eq {(resultLevel.First().Error ? "true" : "false")} or Critical eq {(resultLevel.First().Critical ? "true" : "false")} or Information eq {(resultLevel.First().Information ? "true" : "false")} or Warning eq {(resultLevel.First().Warnings ? "true" : "false")})").ConfigureAwait(false);
                                    if (resultApplicationLogs != null && resultApplicationLogs.Any())
                                    {
                                        ApplicationLogMessages applicationLogMessages = new ApplicationLogMessages(this.ProjectName, applicationLogMessagesStorage);
                                        await AddApplicationLogsMessages(resultApplicationLogs, applicationLogMessages, applicationLogMessagesStorage).ConfigureAwait(false);
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
                    catch (Exception ex) { await WriteLog(ex.Message).ConfigureAwait(false); }
                }

                var result = eventLogsCollection.Any() ? await eventLogs.AddLogs(eventLogsCollection).ConfigureAwait(false) : null;

            }
            catch (Exception ex)
            {
                await WriteLog(ex.Message).ConfigureAwait(false);
            }
        }

        private async Task AddApplicationLogs(string eventLogName, EventLogLevel eventLevel, ApplicationLogs applicationLogs, Storage<ApplicationLogs> Storage)
        {
            try
            {
                List<ApplicationLogs> holdResult = new List<ApplicationLogs>();
                string query = "*[(System/Level=1 or System/Level=2 or System/Level=3 or System/Level=4) and System/TimeCreated/@SystemTime > '" + DateTime.Now.AddDays(-1).ToUniversalTime().ToString("o") + "' and System/TimeCreated/@SystemTime <= '" + DateTime.Now.ToUniversalTime().ToString("o") + "']";
                EventLogQuery eventsQuery = new EventLogQuery(eventLogName, PathType.LogName, query);
                EventLogReader logReader = new EventLogReader(eventsQuery);
                for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
                {
                    if (!holdResult.Exists(l => l.Name == eventdetail.ProviderName))
                        holdResult.Add(new ApplicationLogs(this.ProjectName, Storage) { ID = DateTime.Now.Ticks.ToString(), Name = eventdetail.ProviderName, CanRead = false, MachineName = eventdetail.MachineName, ParentSource = eventLogName, Critical = false, Error = false, Information = false, Warnings = false });
                }
                holdResult.ForEach(async (a) =>
                {
                    var resultLevel = await applicationLogs.ReadApplicationLog($"PartitionKey eq '{this.ProjectName}' and Name eq '{a.Name}'").ConfigureAwait(false);
                    if (resultLevel == null || !resultLevel.Any())
                    {
                        await a.AddApplicationLog().ConfigureAwait(false);
                    }
                });
            }
            catch (Exception ex)
            {
                await WriteLog(ex.Message).ConfigureAwait(false);
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
                        var resultLogMessages = await applicationLogMessages.ReadApplicationLogMessages($"PartitionKey eq '{this.ProjectName}' and Name eq '{applicationLog.Name}'").ConfigureAwait(false);
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
                            await LogMessages.AddApplicationLogMessage().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        await WriteLog(ex.Message).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                await WriteLog(ex.Message).ConfigureAwait(false);
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
                            var result = await serviceInstance.ReadServices($"PartitionKey eq '{this.ProjectName}' and Name eq '{service.ServiceName}'").ConfigureAwait(false);
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
                                await result.Single().UpdateService().ConfigureAwait(false);
                            }
                            else
                            {
                                if ((int)service.Status != result.Single().Status)
                                    result.Single().Status = (int)service.Status == (int)ServiceControllerStatus.Stopped ? (int)ServiceControllerStatus.Running : (int)ServiceControllerStatus.Stopped;
                                result.Single().ShowAction = true;
                                result.Single()._servicesStorage = servicesStorage;
                                await result.Single().UpdateService().ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            await WriteLog(ex.Message).ConfigureAwait(false);
                        }
                    }
                }
                await serviceInstance.AddServices(process).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await WriteLog(ex.Message).ConfigureAwait(false);
            }
        }
        #endregion

        #region LogsWritter

        private async Task WriteLog(string log)
        {
            Task task = new Task(() =>
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "MonitorApplicationLog.txt";
                if (File.Exists(path))
                {
                    FileStream file = new FileStream(path, FileMode.Append, FileAccess.Write);
                    using (StreamWriter newTask = new StreamWriter(file))
                    {
                        newTask.WriteLine($"\r\n ----------{DateTime.Now}-------- \r\n{log}");
                    }
                }
                else
                {
                    File.AppendAllText(path, $"\r\n ----------{DateTime.Now}-------- \r\n{log}");
                }
            });
            task.Start();
            await task.ConfigureAwait(false);
        }
        #endregion
    }
}
