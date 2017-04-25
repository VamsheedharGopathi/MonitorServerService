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
    public class SyncTask
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

        public SyncTask(CloudStorageAccount cloudStorageAccount)
        {
            _cloudStorageAccount = cloudStorageAccount;
        }
        #region MainFunction
        public void MainFunction(string Name)
        {

            switch (Name)
            {
                case "Event":
                    {
                        WriteLog($"MainFunction----{Name}");
                        EventLogs();
                        break;
                    }
                case "Configuration":
                    {
                        WriteLog($"MainFunction----{Name}");
                        FileConfiguration();
                        break;
                    }
                case "Queues":
                    {
                        WriteLog($"MainFunction----{Name}");
                        Queue();
                        break;
                    }
                case "Services":
                    {
                        WriteLog($"MainFunction----{Name}");
                        AutoService();
                        break;
                    }
            }

        }
        #endregion

        #region Queue
        private void Queue()
        {
            try
            {
                WriteLog("Queue Start");
                Storage<Queues> queuesStorage = new Storage<Queues>(_cloudStorageAccount);
                MessageQueue[] msgQueue = MessageQueue.GetPrivateQueuesByMachine(Environment.MachineName);
                //var queue = from p in msgQueue select p;
                Queues queues = new Queues(this.ProjectName, queuesStorage);
                IEnumerable<Queues> resultQueues = null;
                resultQueues = queues.SynReadQueues($"PartitionKey eq '{this.ProjectName}'");
                //var filterqueue = from rq in resultQueues join q in msgQueue on rq.Name equals q.QueueName.Replace("private$\\", "") select q;
                //var queue = filterqueue.Any()? msgQueue.Except(filterqueue): msgQueue;
                List<Queues> queuesCollection = QueueCollection(msgQueue, queuesStorage, resultQueues);
               // WriteLog($"ADD Queues {queuesCollection.Count()}");
                Task task = new Task(async () =>
                {
                    var result = queuesCollection.Any() ? await queues.AddQueues(queuesCollection).ConfigureAwait(false) : null;
                });
                task.Start();
                task.Wait();
            }
            catch (Exception ex)
            {
                WriteLog($"Queue Exception {ex.Message}");
            }
        }

        private List<Queues> QueueCollection(IEnumerable<MessageQueue> queue, Storage<Queues> queuesStorage, IEnumerable<Queues> resultQueues)
        {
            List<Queues> queuesCollection = new List<Queues>();
            Queues queues = null;
            foreach (var q in queue)
            {
                try
                {
                   // WriteLog($"QueueName {q.QueueName}");
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
                            ClearMessage(queues.Name);
                        }
                        resultQueues.First().ClearMessages = false;
                        resultQueues.First()._queueStorage = queuesStorage;
                        resultQueues.First().Count = GetQueueCount(messageQueue);
                        //Task task = new Task(async () =>
                        // {
                        //await resultQueues.First().UpdateQueue().ConfigureAwait(false);
                        //});
                        //task.Start();
                        //task.Wait();
                        resqueue.First().SyncUpdateQueues();
                    }
                    else
                    {
                        queues.Count = GetQueueCount(messageQueue);
                        queues.SyncAddQueues();
                        //queuesCollection.Add(queues);
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"QueueCollection {ex.Message}");
                }
            }
           // WriteLog($"return QueueCollection {queuesCollection.Count()}");
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

        private void ClearMessage(string queueName)
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
            task.Wait();
        }
        #endregion

        #region Configuration
        private void FileConfiguration()
        {
            try
            {
                Storage<Configurations> configurationsStorage = new Storage<Configurations>(_cloudStorageAccount);
                DirectoryInfo directoryInfo = new DirectoryInfo($@"{ConfigurationPath}");
                FileInfo[] fileInfo = directoryInfo.GetFiles();
                Configurations configurations = new Configurations(this.ProjectName, configurationsStorage);
                IEnumerable<Configurations> resultconfigurations = null;
                resultconfigurations = configurations.SyncReadConfigurations($"PartitionKey eq '{this.ProjectName}'");
                List<Configurations> configurationsCollection = ConfigurationsCollection(fileInfo, configurationsStorage, resultconfigurations);
                Task task = new Task(async () =>
                 {
                     var result = configurationsCollection.Any() ? await configurationsStorage.AddEntity(configurationsCollection).ConfigureAwait(false) : null;
                 });
                task.Start();
                task.Wait();
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private List<Configurations> ConfigurationsCollection(FileInfo[] fileInfo, Storage<Configurations> configurationsStorage, IEnumerable<Configurations> Configurations)
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
                            SaveFile(this.ConfigurationPath, resultconfigurations.First().Name, resultconfigurations.First().Format, resultconfigurations.First().Content);
                        }
                        //await resultconfigurations.First().UpdateConfiguration();
                    }
                    else
                    {
                        //Task t = new Task(async() => {
                        //    await configurations.AddConfiguration().ConfigureAwait(false);
                        //});
                        //t.Start();
                        //t.Wait();
                        configurations.SyncAddConfigurations();
                        //configurationsCollection.Add(configurations);
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(ex.Message);
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

        private void SaveFile(string path, string fileName, string format, string content)
        {
            Task task = new Task(() =>
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
                    WriteLog(ex.Message);
                }
            });
            task.Start();
            task.Wait();
        }

        #endregion

        #region EventLogs

        private void EventLogs()
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
                        IEnumerable<EventLogs> resultEventLogs = null;
                            resultEventLogs =  eventLogs.SyncReadLogs($"PartitionKey eq '{this.ProjectName}' and Name eq '{eventLogs.Name}'");
                        if (resultEventLogs != null && resultEventLogs.Any())
                        {
                            if (resultEventLogs.First().CanRead)
                            {
                                IEnumerable<EventLogLevel> resultLevel = null;

                                resultLevel =  eventLogLevel.SyncReadLogLevels($"PartitionKey eq '{this.ProjectName}' and ApplicationName eq '{eventLogs.Name}'");
                                if (resultLevel != null && resultLevel.Any())
                                {
                                    // List<ApplicationLogs> ApplicationLogsCollection = new List<ApplicationLogs>();
                                    ApplicationLogs applicationLogs = new ApplicationLogs(this.ProjectName, applicationLogsStorage);
                                    AddApplicationLogs(eventLogs.Name, resultLevel.First(), applicationLogs, applicationLogsStorage);
                                    IEnumerable<ApplicationLogs> resultApplicationLogs = null;
                                    resultApplicationLogs =  applicationLogs.SyncReadApplicationLog($"PartitionKey eq '{this.ProjectName}' and ParentSource eq '{eventLogs.Name}' and (Error eq {(resultLevel.First().Error ? "true" : "false")} or Critical eq {(resultLevel.First().Critical ? "true" : "false")} or Information eq {(resultLevel.First().Information ? "true" : "false")} or Warning eq {(resultLevel.First().Warnings ? "true" : "false")})");
                                    if (resultApplicationLogs != null && resultApplicationLogs.Any())
                                    {
                                        ApplicationLogMessages applicationLogMessages = new ApplicationLogMessages(this.ProjectName, applicationLogMessagesStorage);
                                        AddApplicationLogsMessages(resultApplicationLogs, applicationLogMessages, applicationLogMessagesStorage);
                                    }
                                }
                            }
                        }
                        else
                        {
                            eventLogs.CanRead = false;
                            eventLogs.SyncAddLogs();
                            //Task task = new Task(async () =>
                            //{
                            //    await eventLogs.AddLog().ConfigureAwait(false);
                            //});
                            //task.Start();
                            //task.Wait();
                            //eventLogsCollection.Add(eventLogs);
                        }
                    }
                    catch (Exception ex) { WriteLog(ex.Message); }
                }
                Task t = new Task(async () =>
                {
                    var result = eventLogsCollection.Any() ? await eventLogs.AddLogs(eventLogsCollection).ConfigureAwait(false) : null;
                });
                t.Start();
                t.Wait();

            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void AddApplicationLogs(string eventLogName, EventLogLevel eventLevel, ApplicationLogs applicationLogs, Storage<ApplicationLogs> Storage)
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
                holdResult.ForEach((a) =>
                {
                    var resultLevel =  applicationLogs.SyncReadApplicationLog($"PartitionKey eq '{this.ProjectName}' and Name eq '{a.Name}'");
                    if (resultLevel == null || !resultLevel.Any())
                    {
                         a.SyncAddApplicationLog();
                    }
                });
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void AddApplicationLogsMessages(IEnumerable<ApplicationLogs> applicationLogs, ApplicationLogMessages applicationLogMessages, Storage<ApplicationLogMessages> applicationLogMessagesStorage)
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
                        IEnumerable<ApplicationLogMessages> resultLogMessages = null;
                        resultLogMessages =  applicationLogMessages.SyncReadApplicationLogMessages($"PartitionKey eq '{this.ProjectName}' and Name eq '{applicationLog.Name}'");
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
                            ////Task t = new Task(async () =>
                            //// {
                            ////     await LogMessages.AddApplicationLogMessage().ConfigureAwait(false);
                            //// });
                            ////t.Start();
                            ////t.Wait();
                            LogMessages.SyncAddApplicationLogMessage();
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        #endregion

        #region Services

        private void AutoService()
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
                            IEnumerable<Services> result = null;

                            result =  serviceInstance.SynReadServices($"PartitionKey eq '{this.ProjectName}' and Name eq '{service.ServiceName}'");
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
                                result.Single().SyncUpdateServices();
                                //Task task = new Task(async () =>
                                //{
                                //    await result.Single().UpdateService().ConfigureAwait(false);
                                //});
                                //task.Start();
                                //task.Wait();
                            }
                            else
                            {
                                if ((int)service.Status != result.Single().Status)
                                    result.Single().Status = (int)service.Status == (int)ServiceControllerStatus.Stopped ? (int)ServiceControllerStatus.Running : (int)ServiceControllerStatus.Stopped;
                                result.Single().ShowAction = true;
                                result.Single()._servicesStorage = servicesStorage;
                                result.Single().SyncUpdateServices();
                                //Task task = new Task(async () =>
                                // {
                                //     await result.Single().UpdateService().ConfigureAwait(false);
                                // });
                                //task.Start();
                                //task.Wait();
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteLog(ex.Message);
                        }
                        //serviceInstance.SyncAddServices();
                    }
                }
                process.ForEach(p =>
                {
                    p.SyncAddServices();
                });
                //Task t = new Task(async () =>
                // {
                //     await serviceInstance.AddServices(process).ConfigureAwait(false);
                // });
                //t.Start();
                //t.Wait();
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }
        #endregion

        #region LogsWritter

        private void WriteLog(string log)
        {
            try
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
            }
            catch (Exception ex) { }
        }
        #endregion
    }
}
