
using Caniactivity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Caniactivity.Backend.JwtFeatures;
using AutoMapper.Internal;
using Caniactivity.Backend.Mapper;
using Caniactivity.Backend.Database.Repositories;
using Microsoft.Extensions.Logging.Configuration;
using Caniactivity.Controllers;

namespace Caniactivity
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var config = builder.Configuration;
            var jwtSettings = config.GetSection("JwtSettings");

            builder.Services.AddAutoMapper(
                cfg => cfg.Internal().MethodMappingEnabled = false, 
                typeof(DtoProfile));

            #region Database Configuration

            builder.Services.AddDbContext<CaniActivityContext>(options =>
            {
                var provider = config.GetValue("provider", Database.Provider.Sqlite.Name);
                if (provider == Database.Provider.Sqlite.Name)
                {
                    options.UseSqlite(
                        config.GetConnectionString(Database.Provider.Sqlite.Name)!,
                        x => x.MigrationsAssembly(Database.Provider.Sqlite.Assembly)
                    );
                }
                //if (provider == Postgres.Name)
                //{
                //    options.UseNpgsql(
                //        config.GetConnectionString(Postgres.Name)!,
                //        x => x.MigrationsAssembly(Postgres.Assembly)
                //    );
                //}
            });

            #endregion

            //builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            #region Authentication

            builder.Services.AddIdentity<RegisteredUser, IdentityRole>()
                            .AddEntityFrameworkStores<CaniActivityContext>();
            builder.Services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = false,
                    ValidIssuer = jwtSettings["validIssuer"],
                    ValidAudience = jwtSettings["validAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                        .GetBytes(JwtHandler.SECURITY_KEY)) // .GetSection("securityKey").Value
                };
            })
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = config["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = config["Authentication:Google:ClientSecret"];
            }); 
            builder.Services.AddScoped<JwtHandler>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IDogRepository, DogRepository>();
            builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

            #endregion

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
            {
                builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
            }));

            var app = builder.Build();

            // initialize database
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CaniActivityContext>();
                var userMngr = scope.ServiceProvider.GetRequiredService<UserManager<RegisteredUser>>();
                await CaniActivityContext.InitializeAsync(db, userMngr);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                //app.UseMigrationEndpoint();
            }

            app.UseHttpsRedirection();
            app.UseCors("corsapp");

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}