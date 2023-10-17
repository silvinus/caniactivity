using AutoMapper;
using Caniactivity.Backend.Database.Repositories;
using Caniactivity.Backend.DevExtreme;
using Caniactivity.Models;
using DevExtreme.AspNet.Data.ResponseModel;
using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;

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

        [HttpPost("userInfo", Name = "userInfo")]
        [Authorize]
        public UserResponse UserInfo([FromBody] UserRequest request)
        {
            return _mapper.Map<UserResponse>(_repository.GetByEmail(request.email));
        }

        [HttpGet(Name = "AllUsers")]
        [Authorize(Roles = UserRoles.Admin)]
        public LoadResult Get([ModelBinder(typeof(DataSourceLoadOptionsHttpBinder))] DataSourceLoadOptions loadOptions)
        {
            return
                DataSourceLoader.Load(_repository.GetAll().Select(user => _mapper.Map<UserResponse>(user)), loadOptions);
        }

        [HttpPut(Name = "UpdateUser")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<ObjectResult> Update()
        {
            IFormCollection form = await Request.ReadFormAsync();
            var valuesRequest = form.Where(w => w.Key == "values").First().Value;
            var key = form["key"];
            var user = _repository.GetById(key);

            JsonConvert.PopulateObject(valuesRequest, user);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _repository.Update(user);
            _repository.Save();

            return Ok(_mapper.Map<UserResponse>(user));
        }

        [HttpDelete(Name = "DeleteUser")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<ObjectResult> Delete()
        {
            IFormCollection form = await Request.ReadFormAsync();

            var key = form["key"];
            var dog = _repository.GetById(key);

            _repository.Delete(dog);
            _repository.Save();

            return Ok(key);
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
        public SSOProvider Provider { get; set; }
        public RegisteredUserStatus Status { get; set; }
    }
}