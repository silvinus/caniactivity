using AutoMapper;
using Caniactivity.Backend.Database.Repositories;
using Caniactivity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Caniactivity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;

        public UserController(ILogger<UserController> logger, IUserRepository repository, IMapper mapper)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet(Name = "registered")]
        public IEnumerable<RegisteredUser> Get()
        {
            return _repository.GetAll();
        }

        [HttpPost("userInfo", Name = "userInfo")]
        [Authorize]
        public UserResponse UserInfo([FromBody] UserRequest request)
        {
            return _mapper.Map<UserResponse>(_repository.GetByEmail(request.email));
        }
    }

    public class UserRequest
    {
        [Required]
        public string email { get; set; }
    }

    public class UserResponse
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Provider { get; set; }
        public string Status { get; set; }
    }
}