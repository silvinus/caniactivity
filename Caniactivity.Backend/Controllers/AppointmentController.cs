using AutoMapper;
using Caniactivity.Backend.Database.Repositories;
using Caniactivity.Backend.DevExtreme;
using Caniactivity.Backend.Services;
using Caniactivity.Models;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Caniactivity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IAppointmentRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IDogRepository _dogRepository;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;

        public AppointmentController(ILogger<UserController> logger, IAppointmentRepository repository,
            IMapper mapper, IDogRepository dogRepository, IUserRepository userRepository, IEmailService emailService)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
            _dogRepository = dogRepository;
            _userRepository = userRepository;
            _emailService = emailService;
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
            string userName = User.Claims.Where(w => w.Type == ClaimTypes.Name).First().Value;
            RegisteredUser registeredUser = _userRepository.GetByEmail(userName);
            
            if (registeredUser.Status != RegisteredUserStatus.Approved)
                return BadRequest("Votre compte n'est pas validé");

            IFormCollection form = await Request.ReadFormAsync();
            var valuesRequest = form.First(w => w.Key == "values").Value.ToString();

            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(valuesRequest);
            if (deserialized is null)
                return BadRequest(ModelState);

            var newAppointment = new Appointment()
            {
                StartDate = deserialized["startDate"] is DateTime ? ((DateTime)deserialized["startDate"]).ToString("yyyy-MM-ddTHH:mmZ") : deserialized["startDate"].ToString(),
                EndDate = deserialized["endDate"] is DateTime ? ((DateTime)deserialized["endDate"]).ToString("yyyy-MM-ddTHH:mmZ") : deserialized["endDate"].ToString(),
                Status = AppointmentStatus.Submitted,
                RegisteredBy = registeredUser
            };

            string? dogs = deserialized["dogs"].ToString();
            if (dogs is not null)
            {
                (JsonConvert.DeserializeObject<List<string>>(dogs) ?? new List<string>())
                    .ForEach(w => {
                        Dog dog = _dogRepository.GetById(Guid.Parse(w));
                        if (dog.Status != DogStatus.TestApproved) BadRequest($"Test de sociabilité obligatoire pour {dog.Name}");

                        newAppointment.Dogs.Add(dog);
                    });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _repository.Insert(newAppointment);
            _repository.Save();

            DateTime start = DateTime.Parse(newAppointment.StartDate);
            Message messageToUser = Message.AppointmentCreated(
                    new List<string>() { registeredUser.Email },
                        "Demande de rendez-vous crée",
                        "Demande de rendez-vous crée",
                        String.Format("{0:dddd, d MMMM yyyy}", DateTime.Now),
                        registeredUser.LastName + " " + registeredUser.FirstName,
                        $"{start:D} à {start:t}",
                        String.Join(',', newAppointment.Dogs.Select(w => w.Name))
                    );
            var messageToAdmin = 
                messageToUser with { To = new List<MailboxAddress>() { new MailboxAddress(RegisteredUser.ADMINISTRATOR_MAIL, RegisteredUser.ADMINISTRATOR_MAIL) } };

            this._emailService.SendEmail(messageToUser, 3);
            this._emailService.SendEmail(messageToAdmin, 3);

            return Ok(_mapper.Map<Appointment>(newAppointment));
        }

        [HttpPut(Name = "UpdateAppointment")]
        [Authorize]
        public async Task<ObjectResult> Update()
        {
            string userName = User.Claims.Where(w => w.Type == ClaimTypes.Name).First().Value;
            RegisteredUser registeredUser = _userRepository.GetByEmail(userName);
            if (registeredUser.Status != RegisteredUserStatus.Approved)
                return BadRequest("Votre compte n'est pas validé");

            IFormCollection form = (await Request.ReadFormAsync());
            var valuesRequest = form.First(w => w.Key == "values").Value;
            var key = Guid.Parse(form["key"].ToString());
            var appointment = _repository.GetById(key);

            // Can't update if :
            // others dogs are registered
            var hasOtherHandlers = appointment.Dogs.Select(w => w.Handler).Where(w => w.Email != userName).Distinct().Count() > 0;
            // appoitment is validated
            var isValidated = appointment.Status == AppointmentStatus.Approved;
            // appointment wasn't created by handler
            var isHandler = appointment.RegisteredBy != null && appointment.RegisteredBy.Id == registeredUser.Id;
            var isAdmin = User.IsInRole(UserRoles.Admin);

            var canUpdate = isAdmin || (isHandler && !hasOtherHandlers && !isValidated);

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
                appointment.Status = AppointmentStatus.Approved;
            }

            var startDate = deserialized["startDate"];
            if(!startDate.Equals(appointment.StartDate))
            {
                if (!canUpdate) return BadRequest("Impossible de modifier la date sur un rendez-vous validé, avec d'autre membre ou dont vous n'êtes pas l'auteur");
                appointment.StartDate = startDate.ToString();
            }
            var endDate = deserialized["endDate"];
            if(!endDate.Equals(appointment.EndDate))
            {
                if (!canUpdate) return BadRequest("Impossible de modifier la date sur un rendez-vous validé, avec d'autre membre ou dont vous n'êtes pas l'auteur");
                appointment.EndDate = endDate.ToString();
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _repository.Update(appointment);
            _repository.Save();

            DateTime start = DateTime.Parse(appointment.StartDate);
            if (appointment.Status == AppointmentStatus.Approved)
            {
                Message messageToUser = Message.AppointmentValidated(
                new List<string>() { registeredUser.Email },
                    "Votre demande a été validée",
                    "Validation de rendez-vous",
                    String.Format("{0:dddd, d MMMM yyyy}", DateTime.Now),
                    registeredUser.LastName + " " + registeredUser.FirstName,
                    $"{start:D} à {start:t}",
                    String.Join(',', appointment.Dogs.Select(w => w.Name))
                );
                var messageToAdmin =
                    messageToUser with { To = new List<MailboxAddress>() { new MailboxAddress(RegisteredUser.ADMINISTRATOR_MAIL, RegisteredUser.ADMINISTRATOR_MAIL) } };

                this._emailService.SendEmail(messageToUser, 3);
                this._emailService.SendEmail(messageToAdmin, 3);
            }
            else
            {
                Message messageToUser = Message.AppointmentModified(
                new List<string>() { registeredUser.Email },
                    "Votre demande a été modifiée",
                    "Demande de modification de rendez-vous",
                    String.Format("{0:dddd, d MMMM yyyy}", DateTime.Now),
                    registeredUser.LastName + " " + registeredUser.FirstName,
                    $"{start:D} à {start:t}",
                    String.Join(',', appointment.Dogs.Select(w => w.Name))
                );
                var messageToAdmin =
                    messageToUser with { To = new List<MailboxAddress>() { new MailboxAddress(RegisteredUser.ADMINISTRATOR_MAIL, RegisteredUser.ADMINISTRATOR_MAIL) } };

                this._emailService.SendEmail(messageToUser, 3);
                this._emailService.SendEmail(messageToAdmin, 3);
            }

            return Ok(_mapper.Map<Appointment>(appointment));
        }

        [HttpDelete(Name = "DeleteAppointment")]
        [Authorize]
        public async Task<ObjectResult> Delete()
        {
            string userName = User.Claims.Where(w => w.Type == ClaimTypes.Name).First().Value;
            RegisteredUser registeredUser = _userRepository.GetByEmail(userName);

            IFormCollection form = await Request.ReadFormAsync();

            var key = Guid.Parse(form["key"]);
            var appointment = _repository.GetById(key);

            _repository.Delete(key);
            _repository.Save();


            DateTime start = DateTime.Parse(appointment.StartDate);
            Message messageToUser = Message.AppointmentDeleted(
            new List<string>() { registeredUser.Email },
                "Votre demande a été annulée",
                "Désolé, votre demande de rendez-vous a été annulée",
                String.Format("{0:dddd, d MMMM yyyy}", DateTime.Now),
                registeredUser.LastName + " " + registeredUser.FirstName,
                $"{start:D} à {start:t}",
                String.Join(',', appointment.Dogs.Select(w => w.Name))
            );
            var messageToAdmin =
                messageToUser with { To = new List<MailboxAddress>() { new MailboxAddress(RegisteredUser.ADMINISTRATOR_MAIL, RegisteredUser.ADMINISTRATOR_MAIL) } };

            this._emailService.SendEmail(messageToUser, 3);
            this._emailService.SendEmail(messageToAdmin, 3);

            return Ok(key);
        }

        private List<string> DeserializeUpdatedDogs(string dogs)
        {
            try
            {
                return JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dogs)
                    .Select(x => x["id"].ToString())
                    .ToList();
            }
            catch (Exception ex)
            {
                return JsonConvert.DeserializeObject<List<string>>(dogs);
            }
        }
    }
}