using Caniactivity.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static Duende.IdentityServer.Models.IdentityResources;

namespace Caniactivity.Backend.Database.Repositories
{
    public interface IUserRepository
    {
        IEnumerable<RegisteredUser> GetAll();
        RegisteredUser GetById(string EmployeeID);
        RegisteredUser GetByEmail(string email);
        //void Insert(Employee employee);
        void Update(RegisteredUser user);
        void Delete(RegisteredUser user);
        void Save();
    }

    public class UserRepository : IUserRepository
    {
        private readonly CaniActivityContext activityContext;

        public UserRepository(CaniActivityContext activityContext)
        {
            this.activityContext = activityContext;
        }

        public void Delete(RegisteredUser user)
        {
            activityContext.Dog.Where(x => x.Handler.Equals(user))
                .ToList().ForEach(w => activityContext.Dog.Remove(w));
            activityContext.RegisteredUsers.Remove(user);
        }

        public IEnumerable<RegisteredUser> GetAll() => activityContext.RegisteredUsers.ToList();

        public RegisteredUser GetByEmail(string email) => activityContext.RegisteredUsers.Where(w => w.Email == email).First();

        public RegisteredUser GetById(string userId) => 
            activityContext.RegisteredUsers
                .Include(w => w.Dogs)
                .Include(w => w.Appointments)
                .Where(w => w.Id == userId).First();

        public void Save()
        {
            activityContext.SaveChanges();
        }

        public void Update(RegisteredUser user)
        {
            activityContext.RegisteredUsers.Update(user);
        }
    }
}
