using AutoMapper;
using Caniactivity.Backend.Database.Repositories;
using Caniactivity.Backend.JwtFeatures;
using Caniactivity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Caniactivity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly ILogger<AccountsController> _logger;
        private readonly UserManager<RegisteredUser> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly JwtHandler _jwtHandler;

        public AccountsController(ILogger<AccountsController> logger,
            UserManager<RegisteredUser> userManager, IMapper mapper, JwtHandler jwtHandler,
            IUserRepository userRepository)
        {
            _logger = logger;
            _userManager = userManager;
            _mapper = mapper;
            _jwtHandler = jwtHandler;
            _userRepository = userRepository;
        }

        [HttpPost(Name = "PostLogin")]
        public async Task<IActionResult> Login([FromBody] LoginCredentialDto credential)
        {
            var user = await _userManager.FindByEmailAsync(credential.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, credential.Password))
                return Unauthorized(new AuthResponseDto { ErrorMessage = "Invalid Authentication" });

            if (user.Status != RegisteredUserStatus.Approved)
                return BadRequest("Votre compte n'est pas encore validé");

            var token = await _jwtHandler.GenerateToken(user);
            string refreshToken = _jwtHandler.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            _userRepository.Update(user);
            _userRepository.Save();

            return Ok(new AuthResponseDto {
                IsAuthSuccessful = true,
                Token = token,
                RefreshToken = refreshToken,
                User = new UserDto { Id = user.Id, Email = user.Email, FirstName = user.FirstName, LastName = user.LastName }
            });
        }

        [HttpPost("Registration")]
        public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userForRegistration)
        {
            if (userForRegistration == null || !ModelState.IsValid)
                return BadRequest();

            var user = _mapper.Map<RegisteredUser>(userForRegistration);
            var result = await _userManager.CreateAsync(user, userForRegistration.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new RegistrationResponseDto { Errors = errors });
            }
            await _userManager.AddToRoleAsync(user, UserRoles.Member);

            return StatusCode(201);
        }

        [HttpPost("Validate")]
        public async Task<IActionResult> ValidateToken([FromBody] UnvalidatedTokenDto unvalidatedToken)
        {
            if(unvalidatedToken.Provider == SSOProvider.Local)
            {
                return Ok();
            }

            if(unvalidatedToken.Provider == SSOProvider.Google)
            {
                var payload = await _jwtHandler.VerifyGoogleToken(unvalidatedToken.Credential);
                if (payload == null)
                    return BadRequest("Invalid External Authentication.");

                var info = new UserLoginInfo(unvalidatedToken.Provider.ToString(), payload.Subject, unvalidatedToken.Provider.ToString());
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(payload.Email);
                    if (user == null)
                    {
                        user = new RegisteredUser {
                            Email = payload.Email,
                            UserName = payload.Email,
                            FirstName = payload.GivenName,
                            LastName = payload.FamilyName,
                            AvatarUrl = payload.Picture,
                            Status = RegisteredUserStatus.Submitted,
                            Provider = SSOProvider.Google
                        };
                        await _userManager.CreateAsync(user);
                        //await _userManager.AddToRoleAsync(user, "Viewer");
                        await _userManager.AddLoginAsync(user, info);
                    }
                    else
                    {
                        await _userManager.AddLoginAsync(user, info);
                    }
                }

                if (user == null)
                    return BadRequest("Invalid External Authentication.");

                //check for the Locked out account
                var token = await _jwtHandler.GenerateToken(user); string refreshToken = _jwtHandler.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
                _userRepository.Update(user);
                _userRepository.Save();

                return Ok(new AuthResponseDto { 
                    Token = token,
                    RefreshToken = refreshToken,
                    IsAuthSuccessful = true, 
                    User = new UserDto { 
                        Id = user.Id,
                        Email = user.Email, 
                        FirstName = user.FirstName, 
                        LastName = user.LastName,
                        AvatarUrl = user.AvatarUrl
                    }});
            }

            return BadRequest($"Authentication with {unvalidatedToken.Provider} not available");
        }

        [HttpPost("Reconnect")]
        public async Task<IActionResult> ReconnectToken([FromBody] UnvalidatedTokenDto reconnectToken)
        {
            try
            {
                var claim = _jwtHandler.ValidateToken(reconnectToken.Credential);
                var user = await _userManager.FindByEmailAsync(claim.Claims.First(claim => claim.Type == ClaimTypes.Name).Value);
                return Ok(new AuthResponseDto
                {
                    IsAuthSuccessful = true,
                    Token = reconnectToken.Credential,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        AvatarUrl = user.AvatarUrl
                    }
                }
                );
            }
            catch (SecurityTokenExpiredException) { return Unauthorized(); }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenRequestDto tokenApiModel)
        {
            if (tokenApiModel is null)
                return BadRequest("Invalid client request");
            string accessToken = tokenApiModel.AccessToken;
            string refreshToken = tokenApiModel.RefreshToken;
            var principal = _jwtHandler.ValidateToken(accessToken, false);

            var username = principal.Identity.Name; //this is mapped to the Name claim by default
            var user = _userRepository.GetByEmail(username);
            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
                return BadRequest("Invalid client request");

            var newAccessToken = await _jwtHandler.GenerateToken(user);
            var newRefreshToken = _jwtHandler.GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            _userRepository.Update(user);
            _userRepository.Save();

            return Ok(new TokenResponseDto()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        [HttpPost("revoke"), Authorize]
        public IActionResult Revoke()
        {
            var username = User.Identity.Name;
            var user = _userRepository.GetByEmail(username);
            if (user == null) return BadRequest();

            _userRepository.Update(user);
            _userRepository.Save();

            return NoContent();
        }
    }

    public class LoginCredentialDto
    {
        [Required(ErrorMessage = "Email is required.")]
        public String Email { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public String Password { get; set; }
    }

    public class AuthResponseDto
    {
        public bool IsAuthSuccessful { get; set; }
        public UserDto? User { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
    }

    public class UserDto
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class UserForRegistrationDto
    {
        [Required(ErrorMessage = "Le prénom est obligatoire")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "L'email est obligatoire")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        public string? Password { get; set; }

        [Compare("Password", ErrorMessage = "Le message et la confirmation ne correspondent pas.")]
        public string? ConfirmPassword { get; set; }
    }

    public class RegistrationResponseDto
    {
        public bool IsSuccessfulRegistration { get; set; }
        public IEnumerable<string>? Errors { get; set; }
    }

    public class UnvalidatedTokenDto
    {
        public string Credential { get; set; }
        public SSOProvider Provider { get; set; }
    }

    public class TokenRequestDto
    {
        [Required]
        public string AccessToken { get; set; } = "";
        [Required]
        public string RefreshToken { get; set; } = "";
    }

    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}