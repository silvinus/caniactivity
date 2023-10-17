using Caniactivity.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Caniactivity.Backend.JwtFeatures
{
    public class JwtHandler
    {
        public static readonly string SECURITY_KEY_ENV_VAR_NAME = "JWT_SECURITY_KEY";
        public static readonly string GOOGLE_API_KEY_ENV_VAR_NAME = "GOOGLE_API_KEY";
        public static readonly string GOOGLE_CLIENT_SECRET_ENV_VAR_NAME = "GOOGLE_CLIENT_SECRET";
        public static readonly string SECURITY_ALGORITHM = SecurityAlgorithms.HmacSha256;

        private readonly ILogger<JwtHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationSection _jwtSettings;
        // private readonly IConfigurationSection _googleSettings;
        private readonly UserManager<RegisteredUser> _userManager;
        private readonly string SECURITY_KEY = Environment.GetEnvironmentVariable(SECURITY_KEY_ENV_VAR_NAME);
        private readonly string GOOGLE_CLIENT_ID = Environment.GetEnvironmentVariable(GOOGLE_API_KEY_ENV_VAR_NAME);

        public JwtHandler(IConfiguration configuration, ILogger<JwtHandler> logger, UserManager<RegisteredUser> userManager)
        {
            _configuration = configuration;
            _jwtSettings = _configuration.GetSection("JwtSettings");
            // _googleSettings = _configuration.GetSection("GoogleAuthSettings");
            _logger = logger;
            _userManager = userManager;
        }

        public SigningCredentials GetSigningCredentials()
        {
            var key = Encoding.UTF8.GetBytes(SECURITY_KEY);
            var secret = new SymmetricSecurityKey(key);
            return new SigningCredentials(secret, SECURITY_ALGORITHM);
        }

        public async Task<List<Claim>> GetClaims(RegisteredUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            return claims;
        }

        public JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
        {
            var tokenOptions = new JwtSecurityToken(
                issuer: _jwtSettings["validIssuer"],
                audience: _jwtSettings["validAudience"],
                claims: claims,
                expires: DateTime.Now.AddSeconds(5), // DateTime.Now.AddMinutes(Convert.ToDouble(_jwtSettings["expiryInMinutes"])),
                signingCredentials: signingCredentials);
            return tokenOptions;
        }

        public async Task<string> GenerateToken(RegisteredUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SECURITY_KEY));
            var signingCredentials = new SigningCredentials(securityKey, SECURITY_ALGORITHM);

            var claims = await this.GetClaims(user);
            var tokenOptions = this.GenerateTokenOptions(signingCredentials, claims);
            var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
            return token;
        }

        public ClaimsPrincipal ValidateToken(string token, bool validateLifetime = true)
        {
            var validationParameters = new TokenValidationParameters()
            {
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SECURITY_KEY)),
                ValidAudience = _jwtSettings["validAudience"],
                ValidIssuer = _jwtSettings["validIssuer"],
                ValidateLifetime = validateLifetime,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = false
            };

            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, validationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SECURITY_ALGORITHM, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string token)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string>() { GOOGLE_CLIENT_ID }
                };
                var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);
                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to validate token");
                throw new Exception("Unable to validate token", ex);
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
    }
}
