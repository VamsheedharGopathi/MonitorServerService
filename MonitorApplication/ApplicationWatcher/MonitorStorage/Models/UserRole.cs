using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    [Serializable]
    public class UserRole : Base
    {
        public IStorage<UserRole> _roleStorage;
        public UserRole() : base("") { }
        public UserRole(string projectName, IStorage<UserRole> storage) : base(projectName)
        {
            _roleStorage = storage;
            Task.Run(async () => await _roleStorage.CreateTable(this));
        }

        public Guid ID { get; set; }
        public Guid UserID { get; set; }
        public Guid RoleID { get; set; }

        public async Task<IList<UserRole>> AddRole()
        {
            List<UserRole> collection = new List<UserRole>() { this };
            IList<UserRole> result = await _roleStorage.AddEntity(collection) as List<UserRole>;
            return result;
        }
        public async Task<IList<UserRole>> AddRoles(List<UserRole> collection)
        {
            IList<UserRole> result = await _roleStorage.AddEntity(collection) as List<UserRole>;
            return result;
        }

        public async Task<IList<UserRole>> UpdateRole()
        {
            return await _roleStorage.UpdateEntity(this) as List<UserRole>;
        }

        public async Task<IList<UserRole>> DeleteRole(List<UserRole> userRoles)
        {
            return await _roleStorage.DeleteEntity(userRoles) as List<UserRole>;
        }

        public async Task<IEnumerable<UserRole>> ReadUser(string query)
        {
            return await _roleStorage.ReadEntity(this, query) as IEnumerable<UserRole>;
        }

    }
}

