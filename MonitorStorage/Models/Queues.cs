using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    public class Queues : Base
    {
        public Storage<Queues> _queueStorage;
        public Queues() : base("")
        { }
        public Queues(string projectName, Storage<Queues> storage) : base(projectName)
        {
            _queueStorage = storage;
            _queueStorage.CreateTable(this);
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

        public async Task<IList<Queues>> DeleteQueue()
        {
            return await _queueStorage.DeleteEntity(this) as List<Queues>;
        }

        public async Task<IEnumerable<Queues>> ReadQueues(string query)
        {
            return  await _queueStorage.ReadEntity(this, query) as IEnumerable<Queues>;
        }
    }
}
