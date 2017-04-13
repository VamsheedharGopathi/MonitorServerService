using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    public class Configurations : Base
    {
        public IStorage<Configurations> _configurationStorage;
        public Configurations() : base("")
        { }

        public string Name { get; set; }
        public string Content { get; set;}
        public bool CanModified { get; set;}
        public string Format { get; set; }
        public string UserName { get; set; }


        public Configurations(string projectName, IStorage<Configurations> storage) : base(projectName)
        {
            _configurationStorage = storage;
            _configurationStorage.CreateTable(this);
        }

        public async Task<IList<Configurations>> AddConfiguration()
        {
            List<Configurations> collection = new List<Configurations>() { this };
            IList<Configurations> result = await _configurationStorage.AddEntity(collection) as List<Configurations>;
            return result;
        }
        public async Task<IList<Configurations>> AddConfigurations(List<Configurations> collection)
        {
            IList<Configurations> result = await _configurationStorage.AddEntity(collection) as List<Configurations>;
            return result;
        }

        public async Task<IList<Configurations>> UpdateConfiguration()
        {
            return await _configurationStorage.UpdateEntity(this) as IList<Configurations>;
        }
        public async Task<IEnumerable<Configurations>> ReadConfigurations(string query)
        {
            return await _configurationStorage.ReadEntity(this, query) as IEnumerable<Configurations>;
        }
    }
}
