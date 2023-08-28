using AutoMapper;
using Caniactivity.Backend.Database.Repositories;
using Caniactivity.Backend.DevExtreme;
using Caniactivity.Models;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Caniactivity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IAppointmentRepository _repository;
        private readonly IDogRepository _dogRepository;
        private readonly IMapper _mapper;

        public AppointmentController(ILogger<UserController> logger, IAppointmentRepository repository,
            IMapper mapper, IDogRepository dogRepository)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _dogRepository = dogRepository;
        }

        [HttpGet(Name = "Get")]
        [Authorize]
        public LoadResult Get(
            [ModelBinder(typeof(DataSourceLoadOptionsHttpBinder))] DataSourceLoadOptions loadOptions)
        {
            return
                DataSourceLoader.Load(_repository.GetAllWithDogsCount(), loadOptions);
        }

        [HttpPost(Name = "AddAppointment")]
        [Authorize]
        public async Task<ObjectResult> Add()
        {
            IFormCollection form = await Request.ReadFormAsync();
            var valuesRequest = form.First(w => w.Key == "values").Value.ToString();

            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(valuesRequest);
            if (deserialized is null)
                return BadRequest(ModelState);

            var newAppointment = new Appointment()
            {
                StartDate = DateTime.Parse(deserialized["startDate"].ToString() ?? ""),
                EndDate = DateTime.Parse(deserialized["endDate"].ToString() ?? ""),
                Status = RegisteredUserStatus.Submitted
            };

            string? dogs = deserialized["dogs"].ToString();
            if (dogs is not null)
            {
                (JsonConvert.DeserializeObject<List<string>>(dogs) ?? new List<string>())
                    .ForEach(w => newAppointment.Dogs.Add(_dogRepository.GetById(Guid.Parse(w))));
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _repository.Insert(newAppointment);
            _repository.Save();

            return Ok(_mapper.Map<Appointment>(newAppointment));
        }

        [HttpPut(Name = "UpdateAppointment")]
        [Authorize]
        public async Task<ObjectResult> Update()
        {
            IFormCollection form = (await Request.ReadFormAsync());
            var valuesRequest = form.First(w => w.Key == "values").Value;
            var key = Guid.Parse(form["key"].ToString());
            var appointment = _repository.GetById(key);

            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(valuesRequest);
            if (deserialized is null)
                return BadRequest(ModelState);

            string? dogs = deserialized["dogs"].ToString();
            if (dogs is not null)
            {
                appointment.Dogs.Clear();
                List<String> dogsId = DeserializeUpdatedDogs(dogs);
                dogsId.ForEach(w => appointment.Dogs.Add(_dogRepository.GetById(Guid.Parse(w))));
            }
            if ((AppointmentStatus)Enum.Parse(typeof(AppointmentStatus), deserialized["status"].ToString()) == 
                AppointmentStatus.Approved)
            {
                appointment.Status = RegisteredUserStatus.Approved;
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _repository.Update(appointment);
            _repository.Save();

            return Ok(_mapper.Map<Appointment>(appointment));
        }

        private List<string> DeserializeUpdatedDogs(string dogs)
        {
            try
            {
                return JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dogs)
                    .Select(x => x["id"].ToString())
                    .ToList();
            }
            catch (Exception ex) {
                return JsonConvert.DeserializeObject<List<string>>(dogs);
            }
        }

        [HttpDelete(Name = "DeleteAppointment")]
        [Authorize]
        public async Task<ObjectResult> Delete()
        {
            IFormCollection form = await Request.ReadFormAsync();

            var key = Guid.Parse(form["key"]);

            _repository.Delete(key);
            _repository.Save();

            return Ok(key);
        }
    }
}