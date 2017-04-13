using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    public class UserQueue : Base
    {
        public IStorage<UserQueue> _userQueueStorage;
        public UserQueue() : base("")
        {
        }
        public UserQueue(string projectName, IStorage<UserQueue> storage) : base(projectName)
        {
            _userQueueStorage = storage;
            _userQueueStorage.CreateTable(this);
        }

        public string UserID { get; set; }
        public string QueueID { get; set; }
        public bool Status { get; set; }

        public async Task<IList<UserQueue>> AddUserQueue()
        {
            List<UserQueue> collection = new List<UserQueue>() { this };
            IList<UserQueue> result = await _userQueueStorage.AddEntity(collection) as List<UserQueue>;
            return result;
        }
        public async Task<IList<UserQueue>> AddUserQueue(List<UserQueue> collection)
        {
            IList<UserQueue> result = await _userQueueStorage.AddEntity(collection) as List<UserQueue>;
            return result;
        }

        public async Task<IList<UserQueue>> UpdateUserQueue()
        {
            return await _userQueueStorage.UpdateEntity(this) as List<UserQueue>;
        }

        public async Task<IList<UserQueue>> DeleteUserQueue(List<UserQueue> users)
        {
            return await _userQueueStorage.DeleteEntity(users) as List<UserQueue>;
        }

        public async Task<IEnumerable<UserQueue>> ReadUserQueue(string query)
        {
            return await _userQueueStorage.ReadEntity(this, query) as IEnumerable<UserQueue>;
        }
    }
}
