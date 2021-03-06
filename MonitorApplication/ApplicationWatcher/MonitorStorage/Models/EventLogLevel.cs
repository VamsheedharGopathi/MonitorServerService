﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    [Serializable]
    public class EventLogLevel:Base
    {
        IStorage<EventLogLevel> _eventLogLevelStorage;
        public EventLogLevel() : base("")
        { }
        public EventLogLevel(string projectName, IStorage<EventLogLevel> storage) : base(projectName)
        {
            _eventLogLevelStorage = storage;
            Task.Run(async () => await _eventLogLevelStorage.CreateTable(this));
        }
        public string ApplicationName
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
       
        public async Task<IList<EventLogLevel>> AddLogLevel()
        {
            List<EventLogLevel> collection = new List<EventLogLevel>() { this };
            IList<EventLogLevel> result = await _eventLogLevelStorage.AddEntity(collection) as List<EventLogLevel>;
            return result;
        }
        public async Task<IList<EventLogLevel>> AddLogLevels(List<EventLogLevel> collection)
        {
            IList<EventLogLevel> result = await _eventLogLevelStorage.AddEntity(collection) as List<EventLogLevel>;
            return result;
        }
        public async Task<IList<EventLogLevel>> UpdateLogLevel()
        {
            return await _eventLogLevelStorage.UpdateEntity(this) as List<EventLogLevel>;
        }

        public async Task<IList<EventLogLevel>> DeleteLogLevel(List<EventLogLevel> EventLogLevels)
        {
            return await _eventLogLevelStorage.DeleteEntity(EventLogLevels) as List<EventLogLevel>;
        }

        public async Task<IEnumerable<EventLogLevel>> ReadLogLevels(string query)
        {
            return await _eventLogLevelStorage.ReadEntity(this, query) as IEnumerable<EventLogLevel>;
        }
    }
}
