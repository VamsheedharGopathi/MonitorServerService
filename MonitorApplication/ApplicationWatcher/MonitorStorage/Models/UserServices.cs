using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    public class UserServices : Base
    {
        public IStorage<UserServices> _userServicesStorage;
        public UserServices() : base("")
        {
        }
        public UserServices(string projectName, IStorage<UserServices> storage) : base(projectName)
        {
            _userServicesStorage = storage;
            _userServicesStorage.CreateTable(this);
        }

        public string UserID { get; set; }
        public string ServiceID { get; set; }
        public bool Status { get; set; }

        public async Task<IList<UserServices>> AddUserServices()
        {
            List<UserServices> collection = new List<UserServices>() { this };
            IList<UserServices> result = await _userServicesStorage.AddEntity(collection) as List<UserServices>;
            return result;
        }
        public async Task<IList<UserServices>> AddUserServices(List<UserServices> collection)
        {
            IList<UserServices> result = await _userServicesStorage.AddEntity(collection) as List<UserServices>;
            return result;
        }

        public async Task<IList<UserServices>> UpdateUserServices()
        {
            return await _userServicesStorage.UpdateEntity(this) as List<UserServices>;
        }

        public async Task<IList<UserServices>> DeleteUserServices(List<UserServices> userServices)
        {
            return await _userServicesStorage.DeleteEntity(userServices) as List<UserServices>;
        }

        public async Task<IEnumerable<UserServices>> ReadUserServices(string query)
        {
            return await _userServicesStorage.ReadEntity(this, query) as IEnumerable<UserServices>;
        }
    }
}
