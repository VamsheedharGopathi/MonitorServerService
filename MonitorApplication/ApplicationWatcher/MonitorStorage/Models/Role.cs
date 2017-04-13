using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    [Serializable]
    public class Role : Base
    {
        public IStorage<Role> _roleStorage;
        public Role() : base("") { }
        public Role(string projectName, IStorage<Role> storage) : base(projectName)
        {
            _roleStorage = storage;
            Task.Run(async () => await _roleStorage.CreateTable(this));
        }

        public string Name { get; set; }
        public async Task<IList<Role>> AddRole()
        {
            List<Role> collection = new List<Role>() { this };
            IList<Role> result = await _roleStorage.AddEntity(collection) as List<Role>;
            return result;
        }
        public async Task<IList<Role>> AddRoles(List<Role> collection)
        {
            IList<Role> result = await _roleStorage.AddEntity(collection) as List<Role>;
            return result;
        }

        public async Task<IList<Role>> UpdateRole()
        {
            return await _roleStorage.UpdateEntity(this) as List<Role>;
        }

        public async Task<IList<Role>> DeleteRole(List<Role> roles)
        {
            return await _roleStorage.DeleteEntity(roles) as List<Role>;
        }

        public async Task<IEnumerable<Role>> ReadRole(string query)
        {
            return await _roleStorage.ReadEntity(this, query) as IEnumerable<Role>;
        }
    }
}
