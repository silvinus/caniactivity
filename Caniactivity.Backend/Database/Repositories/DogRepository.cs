using Caniactivity.Models;
using Microsoft.EntityFrameworkCore;

namespace Caniactivity.Backend.Database.Repositories
{
    public interface IDogRepository
    {
        Dog GetById(Guid dogId);
        IEnumerable<Dog> GetByHandler(string handlerId);
        Task Insert(Dog dog);
        void Update(Dog dog);
        void Delete(Guid dogId);
        void Save();
    }

    public class DogRepository : IDogRepository
    {
        private readonly CaniActivityContext activityContext;

        public DogRepository(CaniActivityContext activityContext)
        {
            this.activityContext = activityContext;
        }

        public void Delete(Guid dogId)
        {
            activityContext.Remove(this.GetById(dogId));
        }

        public IEnumerable<Dog> GetByHandler(string handlerId)
        {
           return activityContext
                .Dog
                 .Include(w => w.Handler)
                 .Where(w => w.Handler.Id == handlerId)
                 .ToList();
        }

        public Dog GetById(Guid dogId)
        {
            return activityContext
                 .Dog
                 .Include(w => w.Handler)
                 .Where(w => w.Id == dogId)
                 .First();
        }

        public async Task Insert(Dog dog)
        {
            await activityContext.Dog.AddAsync(dog);
        }

        public void Save()
        {
            activityContext.SaveChanges();
        }

        public void Update(Dog dog)
        {
            activityContext.Dog.Update(dog);
        }
    }
}
