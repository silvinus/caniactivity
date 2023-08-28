using Caniactivity.Models;
using static Duende.IdentityServer.Models.IdentityResources;

namespace Caniactivity.Backend.Database.Repositories
{
    public interface IUserRepository
    {
        IEnumerable<RegisteredUser> GetAll();
        RegisteredUser GetById(string EmployeeID);
        RegisteredUser GetByEmail(string email);
        //void Insert(Employee employee);
        void Update(RegisteredUser employee);
        void Delete(int EmployeeID);
        void Save();
    }

    public class UserRepository : IUserRepository
    {
        private readonly CaniActivityContext activityContext;

        public UserRepository(CaniActivityContext activityContext)
        {
            this.activityContext = activityContext;
        }

        public void Delete(int UserId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<RegisteredUser> GetAll() => activityContext.RegisteredUsers.ToList();

        public RegisteredUser GetByEmail(string email) => activityContext.RegisteredUsers.Where(w => w.Email == email).First();

        public RegisteredUser GetById(string userId) => activityContext.RegisteredUsers.Where(w => w.Id == userId).First();

        public void Save()
        {
            activityContext.SaveChanges();
        }

        public void Update(RegisteredUser user)
        {
            throw new NotImplementedException();
        }
    }
}
