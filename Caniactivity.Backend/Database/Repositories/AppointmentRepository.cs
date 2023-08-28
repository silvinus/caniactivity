﻿using Caniactivity.Models;
using Microsoft.EntityFrameworkCore;

namespace Caniactivity.Backend.Database.Repositories
{
    public interface IAppointmentRepository
    {
        Appointment GetById(Guid appointmentId);
        IEnumerable<Appointment> GetByRange(DateTime startDate, DateTime endDate);
        IEnumerable<Appointment> GetAll();
        IEnumerable<Appointment> GetAllWithDogsCount();
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

        public IEnumerable<Appointment> GetAllWithDogsCount()
        {
            return activityContext
                    .Appointments
                    .Select(w => new Appointment()
                        {
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
                            })
                        }
                    );
        }

        public Appointment GetById(Guid appointmentId)
        {
            return activityContext
                 .Appointments
                 .Include(w => w.Dogs)
                 .Where(w => w.Id == appointmentId)
                 .First();
        }

        public IEnumerable<Appointment> GetByRange(DateTime startDate, DateTime endDate)
        {
            
            return activityContext
                    .Appointments
                    .Include(w => w.Dogs)
                    .Where(w => startDate >= w.StartDate && w.EndDate <= endDate);
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
