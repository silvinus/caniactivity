using Caniactivity.Models;
using Microsoft.EntityFrameworkCore;

namespace Caniactivity.Backend.Database.Repositories
{
    public interface IAppointmentRepository
    {
        Appointment GetById(Guid appointmentId);
        IEnumerable<Appointment> GetByRange(DateTime startDate, DateTime endDate);
        IEnumerable<Appointment> GetAll();
        IEnumerable<object> GetAllWithDogsCount();
        Task Insert(Appointment appointment);
        void Update(Appointment appointment);
        void Delete(Guid appointmentId);
        void Save();
    }

    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly CaniActivityContext activityContext;

        public AppointmentRepository(CaniActivityContext activityContext)
        {
            this.activityContext = activityContext;
        }

        public void Delete(Guid appointmentId)
        {
            activityContext.Remove(this.GetById(appointmentId));
        }

        public IEnumerable<Appointment> GetAll()
        {
            return activityContext
                    .Appointments;
        }

        public IEnumerable<object> GetAllWithDogsCount()
        {
            return activityContext
                    .Appointments
                    .Select(w =>
                        new { 
                            EndDate= w.EndDate,
                            StartDate = w.StartDate, 
                            Status = w.Status,
                            Id = w.Id,
                            Dogs = (ICollection<Dog>)w.Dogs.Select(x => new Dog()
                            {
                                Breed = x.Breed,
                                Id = x.Id,
                                Name = x.Name,
                                Handler = new RegisteredUser()
                                {
                                    FirstName = x.Handler.FirstName,
                                    LastName = x.Handler.LastName,
                                    Id = x.Handler.Id,
                                    Email = x.Handler.Email
                                }
                            }),
                            RegisteredBy = new RegisteredUser()
                            {
                                Id = w.RegisteredBy.Id,
                                FirstName = w.RegisteredBy.FirstName,
                            },
                            HasMultipleMemberRegistered = w.Dogs.Select(x => x.Handler).Distinct().Count() > 1
                        }
                    );
        }

        public Appointment GetById(Guid appointmentId)
        {
            return activityContext
                 .Appointments
                 .Include(w => w.Dogs)
                 .ThenInclude(x => x.Handler)
                 .Include(w => w.RegisteredBy)
                 .Where(w => w.Id == appointmentId)
                 .First();
        }

        public IEnumerable<Appointment> GetByRange(DateTime startDate, DateTime endDate)
        {
            
            return activityContext
                    .Appointments
                    .Include(w => w.Dogs)
                    .Where(w => startDate >= DateTime.Parse(w.StartDate) && DateTime.Parse(w.EndDate) <= endDate);
        }

        public async Task Insert(Appointment appointment)
        {
            await activityContext.Appointments.AddAsync(appointment);
        }

        public void Save()
        {
            activityContext.SaveChanges();
        }

        public void Update(Appointment appointment)
        {
            activityContext.Appointments.Update(appointment);
        }
    }
}
