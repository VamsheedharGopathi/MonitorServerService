using System;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    [Serializable]
    public class Queues : Base
    {
        public IStorage<Queues> _queueStorage;
        public Queues() : base("")
        { }
        public Queues(string projectName, IStorage<Queues> storage) : base(projectName)
        {
            _queueStorage = storage;
            Task.Run(async () => await _queueStorage.CreateTable(this));
        }

        public string Name
        {
            get;
            set;
        }
        public int Count
        {
            get;
            set;
        }
        public bool ClearMessages
        {
            get;
            set;
        }
        public bool Username
        {
            get;
            set;
        }
        public async Task<IList<Queues>> AddQueue()
        {
            await _queueStorage.CreateTable(this);
            List<Queues> collection = new List<Queues>() { this };
            IList<Queues> result = await _queueStorage.AddEntity(collection) as List<Queues>;
            return result;
        }
        public async Task<IList<Queues>> AddQueues(List<Queues> collection)
        {
            IList<Queues> result = await _queueStorage.AddEntity(collection) as List<Queues>;
            return result;
        }

        public async Task<IList<Queues>> UpdateQueue()
        {
            return await _queueStorage.UpdateEntity(this) as List<Queues>;
        }

        public async Task<IList<Queues>> DeleteQueue(List<Queues> Queues)
        {
            return await _queueStorage.DeleteEntity(Queues) as List<Queues>;
        }

        public async Task<IEnumerable<Queues>> ReadQueues(string query)
        {
            return  await _queueStorage.ReadEntity(this, query) as IEnumerable<Queues>;
        }
    }
}
