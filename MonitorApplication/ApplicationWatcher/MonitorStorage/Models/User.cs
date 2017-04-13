using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MonitorStorage.Models
{
    [Serializable]
    public class User : Base
    {
        public IStorage<User> _userStorage;
        public User() : base("") { }
        public User(string projectName, IStorage<User> storage) : base(projectName)
        {
            _userStorage = storage;
            Task.Run(async () => await _userStorage.CreateTable(this));
        }
        //public Guid? ID { get; set; }
        [Required]
        [Display(Name = "FirstName")]
        public string FirstName { get; set; }
        [Required]
        [Display(Name = "LastName")]
        public string LastName { get; set; }
        public string Password { get; set; }
        public int ExpiryDays { get; set; }
        public bool LoginStatus { get; set; }
        public string EmailID { get; set; }
        public string PhoneNumber { get; set; }
        public string ProjectName { get; set; }
        public string CreateDate { get; set; }
        public async Task<IList<User>> AddUser()
        {
            List<User> collection = new List<User>() { this };
            IList<User> result = await _userStorage.AddEntity(collection) as IList<User>;
            return result;
        }
        public async Task<IList<User>> AddUsers(List<User> collection)
        {
            IList<User> result = await _userStorage.AddEntity(collection) as IList<User>;
            return result;
        }

        public async Task<IList<User>> UpdateUser()
        {
            return await _userStorage.UpdateEntity(this) as IList<User>;
        }

        public async Task<IList<User>> DeleteUser(List<User> users)
        {
            return await _userStorage.DeleteEntity(users) as IList<User>;
        }

        public async Task<IEnumerable<User>> ReadUser(string query)
        {
            return await _userStorage.ReadEntity(this, query) as IEnumerable<User>;
        }

    }

}
