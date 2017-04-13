using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorStorage.Models
{
    public class UserFileSystem : Base
    {
        public IStorage<UserFileSystem> _userFileSystem;
        public UserFileSystem() : base("")
        {
        }
        public UserFileSystem(string projectName, IStorage<UserFileSystem> storage) : base(projectName)
        {
            _userFileSystem = storage;
            _userFileSystem.CreateTable(this);
        }

        public string UserID { get; set; }
        public string FileID { get; set; }
        public bool Status { get; set; }

        public async Task<IList<UserFileSystem>> AddUserFileSystem()
        {
            List<UserFileSystem> collection = new List<UserFileSystem>() { this };
            IList<UserFileSystem> result = await _userFileSystem.AddEntity(collection) as List<UserFileSystem>;
            return result;
        }
        public async Task<IList<UserFileSystem>> AddUserFileSystems(List<UserFileSystem> collection)
        {
            IList<UserFileSystem> result = await _userFileSystem.AddEntity(collection) as List<UserFileSystem>;
            return result;
        }

        public async Task<IList<UserFileSystem>> UpdateUserFileSystem()
        {
            return await _userFileSystem.UpdateEntity(this) as List<UserFileSystem>;
        }

        public async Task<IList<UserFileSystem>> DeleteUserFileSystem(List<UserFileSystem> userFileSystems)
        {
            return await _userFileSystem.DeleteEntity(userFileSystems) as List<UserFileSystem>;
        }

        public async Task<IEnumerable<UserFileSystem>> ReadUserFileSystem(string query)
        {
            return await _userFileSystem.ReadEntity(this, query) as IEnumerable<UserFileSystem>;
        }
    }
}
