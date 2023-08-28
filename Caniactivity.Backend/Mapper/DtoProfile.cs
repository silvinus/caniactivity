using AutoMapper;
using Caniactivity.Controllers;
using Caniactivity.Models;

namespace Caniactivity.Backend.Mapper
{
    public class DtoProfile: Profile
    {
        public DtoProfile() {
            CreateMap<UserForRegistrationDto, RegisteredUser>()
                .ForMember(u => u.UserName, opt => opt.MapFrom(x => x.Email));
            CreateMap<RegisteredUser, UserResponse>()
                .ForMember(u => u.Id, opt => opt.MapFrom(x => x.Id))
                .ForMember(u => u.Email, opt => opt.MapFrom(x => x.Email))
                .ForMember(u => u.Status, opt => opt.MapFrom(x => x.Status))
                .ForMember(u => u.Provider, opt => opt.MapFrom(x => x.Provider))
                .ForMember(u => u.FirstName, opt => opt.MapFrom(x => x.FirstName))
                .ForMember(u => u.LastName, opt => opt.MapFrom(x => x.LastName))
                .ForMember(u => u.Phone, opt => opt.MapFrom(x => x.Phone));
            CreateMap<Dog, DogResponse>()
                .ForMember(u => u.Id, opt => opt.MapFrom(x => x.Id))
                .ForMember(u => u.Name, opt => opt.MapFrom(x => x.Name))
                .ForMember(u => u.Breed, opt => opt.MapFrom(x => x.Breed));
            //CreateMap<Appointment, AppointmentResponse>()
            //    .ForMember(u => u.Id, opt => opt.MapFrom(x => x.Id))
            //    .ForMember(u => u.StartDate, opt => opt.MapFrom(x => x.StartDate))
            //    .ForMember(u => u.EndDate, opt => opt.MapFrom(x => x.EndDate))
            //    .ForMember(u => u.Dogs, opt => opt.MapFrom(x => x.Dogs.Select(w => w.Id)));
            CreateMap<Appointment, Appointment>()
                .ForMember(u => u.Id, opt => opt.MapFrom(x => x.Id))
                .ForMember(u => u.StartDate, opt => opt.MapFrom(x => x.StartDate))
                .ForMember(u => u.EndDate, opt => opt.MapFrom(x => x.EndDate))
                .ForMember(u => u.Dogs, opt => opt.MapFrom(w => w.Dogs.Select(x => new Dog()
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
                })));
            //CreateMap<Dog, AppointmentDogResponse>()
            //    .ForMember(u => u.Id, opt => opt.MapFrom(x => x.Id))
            //    .ForMember(u => u.Name, opt => opt.MapFrom(x => x.Name))
            //    .ForMember(u => u.Breed, opt => opt.MapFrom(x => x.Breed));
        }
    }
}
