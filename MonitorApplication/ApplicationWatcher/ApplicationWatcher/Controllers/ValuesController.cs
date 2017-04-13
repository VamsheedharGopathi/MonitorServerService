using System.Collections.Generic;
using System.Web.Http;
using System;
using System.Net.Http;
using System.Net;
using System.Linq;
using System.Web.Http.Cors;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Configuration;
using MonitorStorage;
using MonitorStorage.Models;
using System.Web;
using Microsoft.WindowsAzure.Storage;

namespace ApplicationWatcher.Controllers
{
    /// <summary>
    /// 
    /// </summary>

    [ApplicationAuthorizationFilter]
    [RoutePrefix("api/ECH")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]

    public class ValuesController : ApiController
    {
        private CloudStorageAccount _cloudStorageAccount;

        private string Storage
        {
            get { return ConfigurationManager.AppSettings["storage"] != null ? ConfigurationManager.AppSettings["storage"].ToString() : ""; }
        }
        private User UserDetails
        {
            get
            {
                if (HttpContext.Current.Request.Headers.GetValues("Authorization") != null)
                {
                    string[] request = HttpContext.Current.Request.Headers.GetValues("Authorization")[0].Split(' ');
                    byte[] resultuser = Convert.FromBase64String(request[1]);
                    string returnValue = ASCIIEncoding.ASCII.GetString(resultuser);
                    User user = JsonConvert.DeserializeObject<User>(returnValue);
                    return user;
                }
                return null;
            }
        }

        private string UserID
        {
            get { return ConfigurationManager.AppSettings["storage"] != null ? ConfigurationManager.AppSettings["storage"].ToString() : ""; }
        }
        /// <summary>
        /// Constructor
        /// </summary>
        public ValuesController()
        {
            _cloudStorageAccount = CloudStorageAccount.Parse(Storage);
        }

        #region Queues
        /// <summary>
        /// Gets Queue Names based on Userid
        /// </summary>

        [HttpGet]
        [Route("Queue/GetUserQueueNames")]
        public async Task<HttpResponseMessage> GetUserQueueNames()
        {
            try
            {
                string UserProject = this.UserDetails.ProjectName;
                string userID = this.UserDetails.ID;
                UserQueue userQueue = new UserQueue();
                userQueue._userQueueStorage = new Storage<UserQueue>(_cloudStorageAccount);
                var userQueueResult = await userQueue.ReadUserQueue($"PartitionKey eq '{UserProject}' and UserID eq '{userID}'");
                Storage<Queues> queuesStorage = new Storage<Queues>(_cloudStorageAccount);
                Queues queues = new Queues(UserProject, queuesStorage);
                var queuesResult = await queues.ReadQueues($"PartitionKey eq '{UserProject}'");
                var resultQueue = from uq in userQueueResult
                                  join q in queuesResult on uq.QueueID equals q.ID
                                  select new { ID = q.ID, Name = q.Name };
                var queue = resultQueue;
                return Request.CreateResponse(HttpStatusCode.OK, queue);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Queue/GetQueueNames")]
        public async Task<HttpResponseMessage> GetQueueNames()
        {
            try
            {
                string UserProject = this.UserDetails.ProjectName;
                Storage<Queues> queuesStorage = new Storage<Queues>(_cloudStorageAccount);
                Queues queues = new Queues(UserProject, queuesStorage);
                var queuesResult = await queues.ReadQueues($"PartitionKey eq '{UserProject}'");
                return Request.CreateResponse(HttpStatusCode.OK, queuesResult.Select(q => new { ID = q.ID, Name = q.Name }));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Gets Queue Count based on Name
        /// </summary>
        /// <param name="name">The Name of the data.</param>
        [HttpGet]
        [Route("Queue/GetQueueMessages/{name}")]
        public async Task<HttpResponseMessage> GetQueueMessages(string name)
        {
            try
            {

                int result = await MessageCount(name, this.UserDetails.ProjectName);
                return Request.CreateResponse(HttpStatusCode.OK, new { result });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Queue/ClearQueueMessages/{name}")]
        public async Task<HttpResponseMessage> ClearQueueMessages(string name)
        {
            try
            {
                // string Requeststring = request.Headers.Authorization.Parameter;
                string UserProject = this.UserDetails.ProjectName;
                Storage<Queues> queuesStorage = new Storage<Queues>(_cloudStorageAccount);
                Queues queues = new Queues(UserProject, queuesStorage);
                var queuesResult = await queues.ReadQueues($"PartitionKey eq '{UserProject}' and Name eq '{name}'");
                var resultData = queuesResult.Single();
                resultData.ClearMessages = true;
                resultData._queueStorage = queuesStorage;
                await resultData.UpdateQueue();
                return Request.CreateResponse(HttpStatusCode.OK, new { queues.Count });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        #endregion

        #region Configuration
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Configuration/GetUserConfigurationFilename")]
        public async Task<HttpResponseMessage> GetUserConfigurationFilename()
        {
            try
            {
                //string UserProject = "ECH";
                //string userID = "636271650855388870";
                UserFileSystem userFileSystem = new UserFileSystem();
                userFileSystem._userFileSystem = new Storage<UserFileSystem>(_cloudStorageAccount);
                var userFileSystemeResult = await userFileSystem.ReadUserFileSystem($"PartitionKey eq '{this.UserDetails.ProjectName}' and UserID eq '{this.UserDetails.ID}'");

                Storage<Configurations> configurationsStorage = new Storage<Configurations>(_cloudStorageAccount);
                Configurations configurations = new Configurations(this.UserDetails.ProjectName, configurationsStorage);
                var configurationsResult = await configurations.ReadConfigurations($"PartitionKey eq '{this.UserDetails.ProjectName}'");

                var resultQueue = from ufs in userFileSystemeResult
                                  join cr in configurationsResult on ufs.FileID equals cr.ID
                                  select new { ID = cr.ID, Name = cr.Name };

                var configuration = resultQueue;
                return Request.CreateResponse(HttpStatusCode.OK, configuration);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Configuration/GetConfigurationFilename")]
        public async Task<HttpResponseMessage> GetConfigurationFilename()
        {
            try
            {
                string UserProject = this.UserDetails.ProjectName;
                Storage<Configurations> configurationsStorage = new Storage<Configurations>(_cloudStorageAccount);
                Configurations configurations = new Configurations(UserProject, configurationsStorage);
                var configurationsResult = await configurations.ReadConfigurations($"PartitionKey eq '{UserProject}'");
                var configuration = configurationsResult.Select(p => new { ID = p.ID, Name = p.Name });
                return Request.CreateResponse(HttpStatusCode.OK, configuration);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Configuration/ReadConfiguration/{name}")]
        public async Task<HttpResponseMessage> ReadConfiguration(string name)
        {
            try
            {
                string userProject = this.UserDetails.ProjectName;
                Storage<Configurations> configurationsStorage = new Storage<Configurations>(_cloudStorageAccount);
                Configurations configurations = new Configurations(userProject, configurationsStorage);
                var configurationsResult = await configurations.ReadConfigurations($"PartitionKey eq '{userProject}' and Name eq '{name}'");
                var result = configurationsResult.Single().Content;
                return Request.CreateResponse(HttpStatusCode.OK, new { result = result });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Configuration/SaveConfiguration")]
        public async Task<HttpResponseMessage> SaveConfiguration([FromBody]string value)
        {
            try
            {
                var data = new
                {
                    filename = string.Empty,
                    fileData = string.Empty
                };
                string UserProject = this.UserDetails.ProjectName;
                var resultData = JsonConvert.DeserializeAnonymousType(value, data);
                Storage<Configurations> configurationsStorage = new Storage<Configurations>(_cloudStorageAccount);
                Configurations configurations = new Configurations(UserProject, configurationsStorage);
                var configurationsResult = await configurations.ReadConfigurations($"PartitionKey eq '{UserProject}' and Name eq '{resultData.filename}'");
                configurationsResult.Single()._configurationStorage = configurationsStorage;
                configurationsResult.Single().Content = resultData.fileData;
                configurationsResult.Single().UserName = "";
                await configurationsResult.Single().UpdateConfiguration();
                return Request.CreateResponse(HttpStatusCode.OK, new { resultData.fileData });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Event/GetUserEventLogNames")]
        public async Task<HttpResponseMessage> GetUserEventLogNames()
        {
            try
            {
                //EventLog[] eventLogs = EventLog.GetEventLogs();
                //string[] result = new string[eventLogs.Length];
                //int i = 0;
                //foreach (EventLog eventLog in eventLogs)
                //{
                //    result[i] = eventLog.LogDisplayName;
                //    i++;
                //}
                //return Request.CreateResponse<IEnumerable<string>>(HttpStatusCode.OK, result);
                string UserProject = this.UserDetails.ProjectName;
                string userID = this.UserDetails.ID;
                UserEvents userEvents = new UserEvents();
                userEvents._userEventsStorage = new Storage<UserEvents>(_cloudStorageAccount);
                var userEventsResult = await userEvents.ReadUserEvents($"PartitionKey eq '{UserProject}' and UserID eq '{userID}'");

                Storage<EventLogs> eventLogsStorage = new Storage<EventLogs>(_cloudStorageAccount);
                EventLogs eventLogs = new EventLogs(UserProject, eventLogsStorage);
                var eventLogsResult = await eventLogs.ReadLogs($"PartitionKey eq '{UserProject}'");

                var resultEventLogs = from ue in userEventsResult
                                      join el in eventLogsResult on ue.EventID equals el.ID
                                      select new { ID = el.ID, Name = el.Name };

                var eventLog = resultEventLogs;
                return Request.CreateResponse(HttpStatusCode.OK, eventLog);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Event/GetEventLogNames")]
        public async Task<HttpResponseMessage> GetEventLogNames()
        {
            try
            {
                //EventLog[] eventLogs = EventLog.GetEventLogs();
                //string[] result = new string[eventLogs.Length];
                //int i = 0;
                //foreach (EventLog eventLog in eventLogs)
                //{
                //    result[i] = eventLog.LogDisplayName;
                //    i++;
                //}
                //return Request.CreateResponse<IEnumerable<string>>(HttpStatusCode.OK, result);
                string UserProject = this.UserDetails.ProjectName;
                Storage<EventLogs> eventLogsStorage = new Storage<EventLogs>(_cloudStorageAccount);
                EventLogs eventLogs = new EventLogs(UserProject, eventLogsStorage);
                var eventLogsResult = await eventLogs.ReadLogs($"PartitionKey eq '{UserProject}'");
                var eventLog = eventLogsResult.Select(p => new { ID = p.ID, Name = p.Name });
                return Request.CreateResponse(HttpStatusCode.OK, eventLog);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Event/GetLogsByLogName/{name}")]
        public async Task<HttpResponseMessage> GetLogsByLogName(string name)
        {
            try
            {
                //string query = "*[(System/Level=1 or System/Level=2 or System/Level=3 or System/Level=4) and System/TimeCreated/@SystemTime >= '" + DateTime.Now.AddDays(-1).ToUniversalTime().ToString("o") + "' and System/TimeCreated/@SystemTime <= '" + DateTime.Now.ToUniversalTime().ToString("o") + "']";
                //EventLogQuery eventsQuery = new EventLogQuery(name, PathType.LogName, query);
                //EventLogReader logReader = new EventLogReader(eventsQuery);
                //List<string> listLogName = new List<string>();
                //for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
                //{
                //    listLogName.Add(eventdetail.ProviderName);
                //}
                //// EventLog eventLog = new EventLog(name, ".");
                //// var eventdata = from EventLogEntry elog in eventLog.Entries where elog.TimeWritten >= DateTime.Now.AddHours(-1) group elog by elog.Source into edata select edata.Key;
                //return Request.CreateResponse(HttpStatusCode.OK, listLogName.GroupBy(s => s).Select(s => s.Key));
                string UserProject = this.UserDetails.ProjectName;
                Storage<ApplicationLogs> eventLogsStorage = new Storage<ApplicationLogs>(_cloudStorageAccount);
                ApplicationLogs applicationLogs = new ApplicationLogs(UserProject, eventLogsStorage);
                var applicationLogsResult = await applicationLogs.ReadApplicationLog($"PartitionKey eq '{UserProject}' and ParentSource eq '{name}'");
                var applicationLog = applicationLogsResult.Select(p => new { ID = p.ID, Name = p.Name });
                return Request.CreateResponse(HttpStatusCode.OK, applicationLog);

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="logName"></param>
        /// <param name="sourceName"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Event/GetLogsByCriteria/{logName}/{sourceName}")]
        public async Task<HttpResponseMessage> GetLogsByCriteria([FromBody] string value, string logName, string sourceName)
        {
            try
            {
                IEnumerable<ApplicationLogMessages> eventMessages = await EventLogCriteria(logName, sourceName, value);
                return Request.CreateResponse(HttpStatusCode.OK, eventMessages.OrderByDescending(f => Convert.ToDateTime(f.MessageDate)));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logName"></param>
        /// <param name="sourceName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Event/GetLogsBySourceName/{logName}/{sourceName}")]
        public HttpResponseMessage GetLogsBySourceName(string logName, string sourceName)
        {
            try
            {
                // var eventMessages = EventLogsourceName(logName, sourceName);
                return Request.CreateResponse(HttpStatusCode.OK, "");// eventMessages.FindAll(f => f.Source == sourceName).OrderBy(f => f.MessageTypeID));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logName"></param>
        /// <param name="sourceName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Event/GetLogsCount/{logName}/{sourceName}")]
        public HttpResponseMessage GetLogsCount(string logName, string sourceName)
        {
            try
            {
                //var eventLog = EventLogsourceName(logName, sourceName);
                return Request.CreateResponse(HttpStatusCode.OK, "");// eventLog.FindAll(f => f.Source == sourceName).Count);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        #endregion

        #region Services
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Services/GetUserProcesses")]
        public async Task<HttpResponseMessage> GetUserProcesses()
        {
            try
            {
                //ServiceController[] services = ServiceController.GetServices(Environment.MachineName);
                string UserProject = this.UserDetails.ProjectName;
                string userID = this.UserDetails.ID;

                UserServices userServices = new UserServices();
                userServices._userServicesStorage = new Storage<UserServices>(_cloudStorageAccount);
                var userServicesResult = await userServices.ReadUserServices($"PartitionKey eq '{UserProject}' and UserID eq '{userID}'");

                Storage<Services> servicesStorage = new Storage<Services>(_cloudStorageAccount);
                Services service = new Services(UserProject, servicesStorage);
                var servicesResult = await service.ReadServices($"PartitionKey eq '{UserProject}'");

                var resultEventLogs = from us in userServicesResult
                                      join s in servicesResult on us.ServiceID equals s.ID
                                      select s;

                return Request.CreateResponse(HttpStatusCode.OK, new { result = resultEventLogs });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Services/GetProcesses")]
        public async Task<HttpResponseMessage> GetProcesses()
        {
            try
            {
                //ServiceController[] services = ServiceController.GetServices(Environment.MachineName);
                string UserProject = this.UserDetails.ProjectName;
                Storage<Services> servicesStorage = new Storage<Services>(_cloudStorageAccount);
                Services service = new Services(UserProject, servicesStorage);
                var servicesResult = await service.ReadServices($"PartitionKey eq '{UserProject}'");
                return Request.CreateResponse(HttpStatusCode.OK, new { result = servicesResult });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Services/GetProcessByName/{processName}")]
        public HttpResponseMessage GetProcessByName(string processName)
        {
            try
            {
                ServiceController[] services = ServiceController.GetServices(Environment.MachineName);
                IEnumerable<ServiceController> service = from serv in services where serv.ServiceName == processName select serv;
                return Request.CreateResponse(HttpStatusCode.OK, service.First());
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="actionType"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Services/MakeAction/{processName}/{actionType}")]
        public async Task<HttpResponseMessage> MakeAction(string processName, int actionType)
        {
            try
            {
                //ServiceController[] services = ServiceController.GetServices(Environment.MachineName);
                // IEnumerable<ServiceController> service = from serv in services where serv.ServiceName == processName select serv;
                string UserProject = this.UserDetails.ProjectName;
                Storage<Services> servicesStorage = new Storage<Services>(_cloudStorageAccount);
                Services service = new Services(UserProject, servicesStorage);
                var servicesResult = await service.ReadServices($"PartitionKey eq '{UserProject}' and Name eq '{processName}'");
                servicesResult.First().Status = actionType;
                servicesResult.First().PerformAction = true;
                servicesResult.First().ShowAction = false;
                servicesResult.First()._servicesStorage = servicesStorage;
                var result = await servicesResult.First().UpdateService();
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        #endregion

        #region Private Methods
        private async Task<int> MessageCount(string name, string UserProject)
        {
            Storage<Queues> queuesStorage = new Storage<Queues>(_cloudStorageAccount);
            Queues queues = new Queues(UserProject, queuesStorage);
            var queuesResult = await queues.ReadQueues($"PartitionKey eq '{UserProject}' and Name eq '{name}'");
            return queuesResult.Single().Count;
        }

        private string ReadFile(string name)
        {
            string result = string.Empty;
            var fileStream = new FileStream($@"C:\Assemblies\_Configs\{name}.xml", FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                result = streamReader.ReadToEnd();
            }
            return result;
        }

        private async Task<IEnumerable<ApplicationLogMessages>> EventLogCriteria(string logName, string sourceName, string criteria)
        {

            var data = new
            {
                DateFrom = string.Empty,
                DateFromTime = string.Empty,
                DateTo = string.Empty,
                DateToTime = string.Empty,
                Level = new[] { 0 }
            };
            var result = JsonConvert.DeserializeAnonymousType(criteria, data);
            string strLevel = string.Empty;
            int count = 0;
            foreach (int level in result.Level)
            {
                count = count + 1;
                //strLevel += $"System/Level = {level} {(result.Level.Length == count?"":" or ")}";
                strLevel += $"LogLevel eq {level}{(result.Level.Length == count ? "" : " or ")}";
            }

            string UserProject = "ECH";
            Storage<ApplicationLogMessages> eventLogsStorage = new Storage<ApplicationLogMessages>(_cloudStorageAccount);
            ApplicationLogMessages applicationLogs = new ApplicationLogMessages(UserProject, eventLogsStorage);
            var applicationLogsResult = await applicationLogs.ReadApplicationLogMessages($"PartitionKey eq '{UserProject}' and Name eq '{sourceName}' and {strLevel}");
            return applicationLogsResult.Where(l => Convert.ToDateTime(l.MessageDate) >= Convert.ToDateTime($"{result.DateFrom} {result.DateFromTime}:00") && Convert.ToDateTime(l.MessageDate) < Convert.ToDateTime($"{result.DateTo} {result.DateToTime}:00"));
            // List<EventMessage> eventMessages = new List<EventMessage>();
            // string query = $"*[System/Provider/@Name='{sourceName}' and ({strLevel}) and System/TimeCreated/@SystemTime >= '" + DateTime.Now.AddDays(-1).ToUniversalTime().ToString("o") + "' and System/TimeCreated/@SystemTime <= '" + DateTime.Now.ToUniversalTime().ToString("o") + "']";
            // string query = $"*[System/Provider/@Name='{sourceName}' and ({strLevel}) and System/TimeCreated/@SystemTime >= '" + Convert.ToDateTime($"{result.DateFrom} {result.DateFromTime}:00").ToUniversalTime().AddHours(-3.5).ToString("o") + "' and System/TimeCreated/@SystemTime <= '" + Convert.ToDateTime($"{result.DateTo} {result.DateToTime}:00").AddHours(-3.5).ToUniversalTime().ToString("o") + "']";
            // EventLogQuery eventsQuery = new EventLogQuery(logName, PathType.LogName, query);
            // EventLogReader logReader = new EventLogReader(eventsQuery);
            // for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
            //{
            //  eventMessages.Add(new EventMessage() { Message = EventLog.Message, Source = EventLog.Source, MessageTypeID = (int)EventLog.EntryType, MessageDateTime = EventLog.TimeGenerated });
            // eventMessages.Add(new EventMessage() { Message = eventdetail.FormatDescription(), Source = eventdetail.ProviderName, MessageTypeID = eventdetail.Level, MessageDateTime = eventdetail.TimeCreated });
            //}
            // return eventMessages;

        }

        //private List<EventMessage> EventLogsourceName(string logName, string sourceName)
        //{
        //    // ManagementObjectSearcher
        //    // EventLog eventLog = new EventLog(logName, ".", sourceName);
        //    List<EventMessage> eventMessages = new List<EventMessage>();
        //    // var eventdata = (from EventLogEntry elog in eventLog.Entries where elog.Source==sourceName && elog.EntryType!=EventLogEntryType.Information && elog.TimeWritten>DateTime.Now.AddHours(-24) orderby elog.TimeWritten descending select elog).Take(300);
        //    string query = $"*[System/Provider/@Name='{sourceName}' and (System/Level=1 or System/Level=2 or System/Level=3 or System/Level=4) and System/TimeCreated/@SystemTime >= '" + DateTime.Now.AddDays(-1).ToUniversalTime().ToString("o") + "' and System/TimeCreated/@SystemTime <= '" + DateTime.Now.ToUniversalTime().ToString("o") + "']";
        //    EventLogQuery eventsQuery = new EventLogQuery(logName, PathType.LogName, query);
        //    EventLogReader logReader = new EventLogReader(eventsQuery);
        //    for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
        //    {
        //        //  eventMessages.Add(new EventMessage() { Message = EventLog.Message, Source = EventLog.Source, MessageTypeID = (int)EventLog.EntryType, MessageDateTime = EventLog.TimeGenerated });
        //        eventMessages.Add(new EventMessage() { Message = eventdetail.FormatDescription(), Source = eventdetail.ProviderName, MessageTypeID = eventdetail.Level, MessageDateTime = eventdetail.TimeCreated });
        //    }
        //    return eventMessages;
        //}

        #endregion

        #region Users
        /// <summary>
        /// Save user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("User/SaveUsers")]
        public async Task<HttpResponseMessage> SaveUsers([FromBody]string user)
        {
            try
            {
                var schema = new
                {
                    User =
                    new
                    {
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        Password = string.Empty,
                        EmailID = string.Empty,
                        PhoneNumber = string.Empty,
                        ExpiryDays = 0,
                        Status = false,
                        ProjectName = string.Empty
                    },
                    Queue = new[] { new { Name = string.Empty, Status = false, ID = string.Empty } },
                    ConfigurationFiles = new[] { new { Name = string.Empty, Status = false, ID = string.Empty } },
                    Events = new[] { new { Name = string.Empty, Status = false, ID = string.Empty } },
                    Services = new[] { new { Name = string.Empty, Status = false, ID = string.Empty } }

                };
                var deserializData = JsonConvert.DeserializeAnonymousType(user, schema);
                Storage<User> userStorage = new Storage<User>(_cloudStorageAccount);
                User userData = new User(deserializData.User.ProjectName, userStorage);
                var checkUser = await userData.ReadUser($"PartitionKey eq '{this.UserDetails.ProjectName}' and EmailID eq '{deserializData.User.EmailID}'");
                string UserID = string.Empty;
                string projectName = this.UserDetails.ProjectName;
                IList<User> userResult = null;
                if (checkUser.Any())
                {
                    UserID = checkUser.Single().ID;
                    checkUser.Single()._userStorage = userStorage;
                    checkUser.Single().ExpiryDays = deserializData.User.ExpiryDays;
                    userResult=await checkUser.Single().UpdateUser();
                }
                else
                {
                    userData.ID = DateTime.Now.Ticks.ToString();
                    userData.FirstName = deserializData.User.FirstName;
                    userData.LastName = deserializData.User.LastName;
                    userData.Password = new Random().Next().ToString();
                    userData.EmailID = deserializData.User.EmailID;
                    userData.ExpiryDays = deserializData.User.ExpiryDays;
                    userData.PhoneNumber = deserializData.User.PhoneNumber;
                    userData.CreateDate = DateTime.Now.ToString();
                    userResult = await userData.AddUser();
                    UserID = userData.ID;
                }
                UserQueue userQueue = null;
                Storage<UserQueue> userQueueStorage = new Storage<UserQueue>(_cloudStorageAccount);
                List<UserQueue> listUserQueue = new List<UserQueue>();
                if (checkUser.Any() && deserializData.Queue.Count() > 0)
                {
                    userQueue = new UserQueue();
                    userQueue._userQueueStorage = userQueueStorage;
                    var userQueuedata=await userQueue.ReadUserQueue($"PartitionKey eq '{this.UserDetails.ProjectName}' and UserID eq '{UserID}'");
                    if(userQueuedata.Any())
                    await userQueue.DeleteUserQueue(userQueuedata.ToList<UserQueue>());
                }
                foreach (var queue in deserializData.Queue)
                {
                    userQueue = new UserQueue(projectName, userQueueStorage);
                    userQueue.UserID = UserID;
                    userQueue.QueueID = queue.ID;
                    userQueue.Status = queue.Status;
                    await userQueue.AddUserQueue();
                }
                //if (listUserQueue.Any())
                //{
                   // await userQueue.AddUserQueue(listUserQueue);
                //}

                UserFileSystem userFileSystem = null;
                Storage<UserFileSystem> userFileSystemStorage = new Storage<UserFileSystem>(_cloudStorageAccount);
                List<UserFileSystem> listUserFileSystem = new List<UserFileSystem>();
                if (checkUser.Any() && deserializData.ConfigurationFiles.Count() > 0)
                {
                    userFileSystem = new UserFileSystem();
                    userFileSystem._userFileSystem = userFileSystemStorage;
                    var userFileSystemdata = await userFileSystem.ReadUserFileSystem($"PartitionKey eq '{this.UserDetails.ProjectName}' and UserID eq '{UserID}'");
                    if (userFileSystemdata.Any())
                        await userFileSystem.DeleteUserFileSystem(userFileSystemdata.ToList<UserFileSystem>());
                }
                foreach (var configuration in deserializData.ConfigurationFiles)
                {
                    userFileSystem = new UserFileSystem(projectName, userFileSystemStorage);
                    userFileSystem.UserID = UserID;
                    userFileSystem.FileID = configuration.ID;
                    userFileSystem.Status = configuration.Status;
                    //listUserFileSystem.Add(userFileSystem);
                    await userFileSystem.AddUserFileSystem();
                }
                //if (listUserFileSystem.Any())
                //{
                //    await userFileSystem.AddUserFileSystems(listUserFileSystem);
                //}

                UserEvents userEvents = null;
                Storage<UserEvents> userEventStorage = new Storage<UserEvents>(_cloudStorageAccount);
                List<UserEvents> listUserEvents = new List<UserEvents>();
                if (checkUser.Any() && deserializData.Events.Count() > 0)
                {
                    userEvents = new UserEvents();
                    userEvents._userEventsStorage = userEventStorage;
                    var userEventsdata = await userEvents.ReadUserEvents($"PartitionKey eq '{this.UserDetails.ProjectName}' and UserID eq '{UserID}'");
                    if (userEventsdata.Any())
                        await userEvents.DeleteUserEvents(userEventsdata.ToList<UserEvents>());
                }
                foreach (var events in deserializData.Events)
                {
                    userEvents = new UserEvents(projectName, userEventStorage);
                    userEvents.UserID = UserID;
                    userEvents.EventID = events.ID;
                    userEvents.Status = events.Status;
                    //listUserEvents.Add(userEvents);
                    await userEvents.AddUserEvents();
                }
                //if (listUserEvents.Any())
                //{
                //    await userEvents.AddUserEvents(listUserEvents);
                //}

                UserServices userServices = null;
                Storage<UserServices> userServicesStorage = new Storage<UserServices>(_cloudStorageAccount);
                List<UserServices> listUserServices = new List<UserServices>();
                if (checkUser.Any() && deserializData.Services.Count() > 0)
                {
                    userServices = new UserServices();
                    userServices._userServicesStorage = userServicesStorage;
                    var userServicesdata = await userServices.ReadUserServices($"PartitionKey eq '{this.UserDetails.ProjectName}' and UserID eq '{UserID}'");
                    if (userServicesdata.Any())
                        await userServices.DeleteUserServices(userServicesdata.ToList<UserServices>());
                }
                foreach (var service in deserializData.Services)
                {
                    userServices = new UserServices(projectName, userServicesStorage);
                    userServices.UserID = UserID;
                    userServices.ServiceID = service.ID;
                    userServices.Status = service.Status;
                    //listUserServices.Add(userServices);
                    await userServices.AddUserServices();
                }
                //if (listUserServices.Any())
                //{
                //    await userServices.AddUserServices(listUserServices);
                //}
                return Request.CreateResponse(HttpStatusCode.OK, new { result = deserializData });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

        }
        /// <summary>
        /// Gets user Details
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("User/GetUser")]
        public async Task<HttpResponseMessage> GetUsers(HttpRequestMessage request)
        {
            try
            {
                //string Requeststring = request.Headers.Authorization.Parameter;
                //byte[] resultuser = Convert.FromBase64String(Requeststring);
                //string returnValue = System.Text.ASCIIEncoding.ASCII.GetString(resultuser);
                var requestData = await request.Content.ReadAsStringAsync();
                User user = JsonConvert.DeserializeObject<User>(requestData);
                Storage<User> userStorage = new Storage<User>(_cloudStorageAccount);
                user._userStorage = userStorage;
                var userResult = await user.ReadUser($"EmailID eq '{user.EmailID.Trim()}' and Password eq '{user.Password}'");
                var resultdata = userResult.Single();
                var result = new
                {
                    ID = resultdata.ID,
                    EmailId = resultdata.EmailID,
                    FirstName = resultdata.FirstName,
                    LastName = resultdata.LastName,
                    ProjectName = resultdata.PartitionKey,
                    ExpriyDays = resultdata.ExpiryDays,
                    PhoneNumber = resultdata.PhoneNumber,
                    Password = Convert.ToBase64String(Encoding.ASCII.GetBytes(resultdata.Password))
                };
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// Save Role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("User/SaveRole")]
        public async Task<HttpResponseMessage> SaveRole(Role role)
        {
            try
            {
                Storage<Role> roleStorage = new Storage<Role>(_cloudStorageAccount);
                role._roleStorage = roleStorage;
                var result = await role.AddRole();
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// GetRoles
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [Route("User/GetRoles")]
        public async Task<HttpResponseMessage> GetRoles()
        {
            try
            {
                Role role = new Role();
                Storage<Role> roleStorage = new Storage<Role>(_cloudStorageAccount);
                role._roleStorage = roleStorage;
                var resultdata = await role.ReadRole($"PartitionKey eq 'ECH'");
                var result = resultdata.Select(r => new { ID = r.ID, Name = r.Name });
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// Get User By Email-ID
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("User/GetUserByEmailID")]
        public async Task<HttpResponseMessage> GetUserByEmailID(HttpRequestMessage request)
        {
            try
            {
                string projectName = this.UserDetails.ProjectName;
               // string userID= this.UserDetails.ID;
                var requestData = await request.Content.ReadAsStringAsync();
                User user = new User();
                Storage<User> userStorage = new Storage<User>(_cloudStorageAccount);
                user._userStorage = userStorage;
                var userResultData = await user.ReadUser($"PartitionKey eq '{projectName}' and EmailID eq '{requestData}'");
                var userResult = userResultData.Select(u => new { FirstName = u.FirstName, LastName = u.LastName, EmailID = u.EmailID, ExpiryDays = u.ExpiryDays, PhoneNumber = u.PhoneNumber, ID = u.ID }).Single();

                UserQueue userQueue = new UserQueue();
                Storage<UserQueue> UserQueueStorage = new Storage<UserQueue>(_cloudStorageAccount);
                userQueue._userQueueStorage = UserQueueStorage;
                var userQueueResultData = await userQueue.ReadUserQueue($"PartitionKey eq '{projectName}' and UserID eq '{userResult.ID}'");
                var userQueueResult = userQueueResultData.Select(u => new { queueID = u.QueueID, status=u.Status });

                UserFileSystem userFileSystem = new UserFileSystem();
                Storage<UserFileSystem> userFileSystemStorage = new Storage<UserFileSystem>(_cloudStorageAccount);
                userFileSystem._userFileSystem = userFileSystemStorage;
                var userFileSystemResultData = await userFileSystem.ReadUserFileSystem($"PartitionKey eq '{projectName}' and UserID eq '{userResult.ID}' and Status eq true");
                var userFileSystemResult = userFileSystemResultData.Select(u => new { fileID = u.FileID, status = u.Status });

                UserEvents userEvents = new UserEvents();
                Storage<UserEvents> userEventsStorage = new Storage<UserEvents>(_cloudStorageAccount);
                userEvents._userEventsStorage = userEventsStorage;
                var userEventsResultData = await userEvents.ReadUserEvents($"PartitionKey eq '{projectName}' and UserID eq '{userResult.ID}' and Status eq true");
                var userEventsResult = userEventsResultData.Select(u => new { eventID = u.EventID, status = u.Status });

                UserServices userServices = new UserServices();
                Storage<UserServices> userServicesStorage = new Storage<UserServices>(_cloudStorageAccount);
                userServices._userServicesStorage = userServicesStorage;
                var userServicesResultData = await userServices.ReadUserServices($"PartitionKey eq '{projectName}' and UserID eq '{userResult.ID}' and Status eq true");
                var userServicesResult = userServicesResultData.Select(u => new { serviceID = u.ServiceID, status = u.Status });

                var result = new
                {
                    user = userResult,
                    userQueue= userQueueResult,
                    userFileSystem= userFileSystemResult,
                    userEvents= userEventsResult,
                    userServices= userServicesResult
                };

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        #endregion

    }
}
