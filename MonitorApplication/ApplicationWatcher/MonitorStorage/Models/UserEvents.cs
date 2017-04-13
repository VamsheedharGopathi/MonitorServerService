using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    public class UserEvents : Base
    {
        public IStorage<UserEvents> _userEventsStorage;
        public UserEvents() : base("")
        {
        }
        public UserEvents(string projectName, IStorage<UserEvents> storage) : base(projectName)
        {
            _userEventsStorage = storage;
            _userEventsStorage.CreateTable(this);
        }

        public string UserID { get; set; }
        public string EventID { get; set; }
        public bool Status { get; set; }

        public async Task<IList<UserEvents>> AddUserEvents()
        {
            List<UserEvents> collection = new List<UserEvents>() { this };
            IList<UserEvents> result = await _userEventsStorage.AddEntity(collection) as List<UserEvents>;
            return result;
        }
        public async Task<IList<UserEvents>> AddUserEvents(List<UserEvents> collection)
        {
            IList<UserEvents> result = await _userEventsStorage.AddEntity(collection) as List<UserEvents>;
            return result;
        }

        public async Task<IList<UserEvents>> UpdateUserEvents()
        {
            return await _userEventsStorage.UpdateEntity(this) as List<UserEvents>;
        }

        public async Task<IList<UserEvents>> DeleteUserEvents(List<UserEvents> userEvents)
        {
            return await _userEventsStorage.DeleteEntity(userEvents) as List<UserEvents>;
        }

        public async Task<IEnumerable<UserEvents>> ReadUserEvents(string query)
        {
            return await _userEventsStorage.ReadEntity(this, query) as IEnumerable<UserEvents>;
        }
    }
}
