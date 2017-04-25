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

        private string AsyncTask
        {
            get { return ConfigurationManager.AppSettings["asyncTask"] != null ? ConfigurationManager.AppSettings["asyncTask"].ToString() : ""; }
        }
        private string Storage
        {
            get { return ConfigurationManager.AppSettings["storage"] != null ? ConfigurationManager.AppSettings["storage"].ToString() : ""; }
        }
        private string ProjectName
        {
            get { return ConfigurationManager.AppSettings["projectName"] != null ? ConfigurationManager.AppSettings["projectName"].ToString() : ""; }
        }
        public MonitorService()
        {
            InitializeComponent();
            _cloudStorageAccount = CloudStorageAccount.Parse(Storage);
        }
        protected override void OnStart(string[] args)
        {
# if DEBUG       
            System.Threading.Thread.Sleep(20000);
#endif
            Task.Run(new Action(async () =>
            {
                await WriteLog("Started").ConfigureAwait(false);
                await ScheduleTimer().ConfigureAwait(false);
            }));
        }
        private async Task ScheduleTimer()
        {
            try
            {
                await WriteLog($"ScheduleTimer Started Project Name:{ this.ProjectName}").ConfigureAwait(false);
                Storage<MonitoringTimer> monitoringTimerStorage = new Storage<MonitoringTimer>(_cloudStorageAccount);
                MonitoringTimer monitoringTimer = new MonitoringTimer(this.ProjectName, monitoringTimerStorage);
                IEnumerable<MonitoringTimer> monitoringTimerResult = await monitoringTimer.ReadMonitoringTimer($"PartitionKey eq '{this.ProjectName}'").ConfigureAwait(false);
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
            catch (Exception ex)
            {
                await WriteLog(ex.Message).ConfigureAwait(false);
            }
        }
        void Main_Tick(object sender, EventArgs args)
        {
            try
            {
                ProjectTimer projectTimer = sender as ProjectTimer;
                Task t = null;
                switch (this.AsyncTask)
                {
                    case "Async":
                        {
                            AsyncTask asyncTask = new AsyncTask(_cloudStorageAccount);
                            t = new Task(async () =>
                            {
                                await asyncTask.MainFunction(projectTimer.EventName);
                            });
                            t.Start();
                            t.Wait();
                            break;
                        }
                    case "Sync":
                        {
                            SyncTask syncTask = new SyncTask(_cloudStorageAccount);
                            syncTask.MainFunction(projectTimer.EventName);
                            break;
                        }
                    default:
                        {
                            AsyncTask asyncTask = new AsyncTask(_cloudStorageAccount);
                            t = new Task(async () =>
                            {
                                await asyncTask.MainFunction(projectTimer.EventName);
                            });
                            t.Start();
                            t.Wait();
                            break;
                        }
                }
                
            }
            catch (Exception ex)
            { }

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
