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
        }
    }
}
