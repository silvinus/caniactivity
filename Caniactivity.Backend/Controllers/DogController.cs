using AutoMapper;
using Caniactivity.Backend.Database.Repositories;
using Caniactivity.Backend.DevExtreme;
using Caniactivity.Models;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.Helpers;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Caniactivity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DogController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IDogRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public DogController(ILogger<UserController> logger, IDogRepository repository, 
            IMapper mapper, IUserRepository userRepository)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _userRepository = userRepository;
        }

        [HttpGet("{handlerId}", Name = "GetByHandler")]
        [Authorize]
        public LoadResult Get(string handlerId,
            [ModelBinder(typeof(DataSourceLoadOptionsHttpBinder))] DataSourceLoadOptions loadOptions)
        {
            return
                DataSourceLoader.Load(_repository.GetByHandler(handlerId).Select(d => _mapper.Map<DogResponse>(d)),
                loadOptions);
        }

        [HttpPost("{handlerId}", Name = "AddDog")]
        [Authorize]
        public async Task<ObjectResult> Add(string handlerId)
        {
            IFormCollection form = await Request.ReadFormAsync();
            var valuesRequest = form.Where(w => w.Key == "values").FirstOrDefault();

            var newDog = new Dog();
            JsonConvert.PopulateObject(valuesRequest.Value, newDog);
            newDog.Handler = _userRepository.GetById(handlerId);

            //Validate(newOrder);
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _repository.Insert(newDog);
            _repository.Save();

            return Ok(_mapper.Map<DogResponse>(newDog));
        }

        [HttpPut("{handlerId}", Name = "AddDog")]
        [Authorize]
        public async Task<ObjectResult> Update(string handlerId)
        {
            IFormCollection form = await Request.ReadFormAsync();
            var valuesRequest = form.Where(w => w.Key == "values").First().Value;
            var key = Guid.Parse(form["key"]);
            var dog = _repository.GetById(key);

            if (dog.Handler.Id != handlerId)
                return BadRequest(ModelState);

            JsonConvert.PopulateObject(valuesRequest, dog);
            dog.Handler = _userRepository.GetById(handlerId);

            //Validate(newOrder);
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _repository.Update(dog);
            _repository.Save();

            return Ok(_mapper.Map<DogResponse>(dog));
        }

        [HttpDelete("{handlerId}", Name = "DeleteDog")]
        [Authorize]
        public async Task<ObjectResult> Delete(string handlerId)
        {
            IFormCollection form = await Request.ReadFormAsync();

            var key = Guid.Parse(form["key"]);
            var dog = _repository.GetById(key);

            if(dog.Handler.Id != handlerId)
                return BadRequest(ModelState);

            _repository.Delete(key);
            _repository.Save();

            return Ok(key);
        }
    }

    public class DogResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Breed { get; set; } = "";
    }
}