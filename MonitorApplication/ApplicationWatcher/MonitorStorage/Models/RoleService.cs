using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    [Serializable]
    class RoleService:Base
    {
        public IStorage<RoleService> _roleServiceStorage;
        public RoleService() : base("")
        { }
        public RoleService(string projectName, IStorage<RoleService> storage) : base(projectName)
        {
            _roleServiceStorage = storage;
            Task.Run(async () => await _roleServiceStorage.CreateTable(this));
        }

        public string ServiceName
        {
            get;
            set;
        }
        public string ServiceID
        {
            get;
            set;
        }
        public string RoleName
        {
            get;
            set;
        }
        public string RoleID
        {
            get;
            set;
        }
        public bool View
        {
            get;
            set;
        }
        public bool PerformAction
        {
            get;
            set;
        }
        public async Task<IList<RoleService>> AddQueue()
        {
            await _roleServiceStorage.CreateTable(this);
            List<RoleService> collection = new List<RoleService>() { this };
            IList<RoleService> result = await _roleServiceStorage.AddEntity(collection) as List<RoleService>;
            return result;
        }
        public async Task<IList<RoleService>> AddQueues(List<RoleService> collection)
        {
            IList<RoleService> result = await _roleServiceStorage.AddEntity(collection) as List<RoleService>;
            return result;
        }

        public async Task<IList<RoleService>> UpdateQueue()
        {
            return await _roleServiceStorage.UpdateEntity(this) as List<RoleService>;
        }

        public async Task<IList<RoleService>> DeleteQueue(List<RoleService> roleService)
        {
            return await _roleServiceStorage.DeleteEntity(roleService) as List<RoleService>;
        }

        public async Task<IEnumerable<RoleService>> ReadQueues(string query)
        {
            return await _roleServiceStorage.ReadEntity(this, query) as IEnumerable<RoleService>;
        }
    }
}
