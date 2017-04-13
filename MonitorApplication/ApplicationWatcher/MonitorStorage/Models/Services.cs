using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace MonitorStorage.Models
{

    public class Services : Base
    {
       public IStorage<Services> _servicesStorage;
        public string Name
        {
            get;
            set;
        }
        public string DisplayName
        {
            get;
            set;
        }
        public string MachineName
        {
            get;
            set;
        }
        public Boolean ShowAction
        {
            get;
            set;
        }
        public int Status
        {
            get;
            set;
        }
        public Boolean PerformAction
        {
            get;
            set;
        }
        public Services() : base("")
        {
        }
        public Services(string projectName, IStorage<Services> storage) : base(projectName)
        {
            _servicesStorage = storage;
            Task.Run(async () => await _servicesStorage.CreateTable(this));
        }
        public async Task<IList<Services>> AddService()
        {
            List<Services> collection = new List<Services>() { this };
            IList<Services> result = await _servicesStorage.AddEntity(collection) as List<Services>;
            return result;
        }
        public async Task<IList<Services>> AddServices(List<Services> collection)
        {
            IList<Services> result = await _servicesStorage.AddEntity(collection) as List<Services>;
            return result;
        }

        public async Task<IList<Services>> UpdateService()
        {
            return await _servicesStorage.UpdateEntity(this) as List<Services>;
        }

        public async Task<IEnumerable<Services>> ReadServices(string query)
        {
            return await _servicesStorage.ReadEntity(this, query) as IEnumerable<Services>;
        }
    }
}
