
using Caniactivity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Caniactivity.Backend.JwtFeatures;
using AutoMapper.Internal;
using Caniactivity.Backend.Mapper;
using Caniactivity.Backend.Database.Repositories;
using Caniactivity.Backend.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Caniactivity.Backend;
using Microsoft.Extensions.FileProviders;

namespace Caniactivity
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
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
                    if (provider == Database.Provider.Postgres.Name)
                    {
                        options.UseNpgsql(
                            config.GetConnectionString(Database.Provider.Postgres.Name)!,
                            x => x.MigrationsAssembly(Database.Provider.Postgres.Assembly)
                        );
                    }
                    if (provider == Database.Provider.Mysql.Name)
                    {
                        options.UseMySQL(
                            config.GetConnectionString(Database.Provider.Mysql.Name)!,
                            x => x.MigrationsAssembly(Database.Provider.Mysql.Assembly)
                        );
                    }
                });

                #endregion

                #region Authentication

                builder.Services.AddIdentity<RegisteredUser, IdentityRole>()
                                .AddEntityFrameworkStores<CaniActivityContext>()
                                .AddDefaultTokenProviders();

                builder.Services.AddAuthentication(opt =>
                {
                    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                    .AddJwtBearer(options =>
                    {
                        options.SaveToken = true;
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = false,
                            ValidIssuer = jwtSettings["validIssuer"],
                            ValidAudience = jwtSettings["validAudience"],
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                                .GetBytes(Environment.GetEnvironmentVariable(JwtHandler.SECURITY_KEY_ENV_VAR_NAME)))
                        };
                    })
                    .AddGoogle(googleOptions =>
                    {
                        googleOptions.ClientId = Environment.GetEnvironmentVariable(JwtHandler.GOOGLE_API_KEY_ENV_VAR_NAME); // config["Authentication:Google:ClientId"];
                        googleOptions.ClientSecret = Environment.GetEnvironmentVariable(JwtHandler.GOOGLE_CLIENT_SECRET_ENV_VAR_NAME);
                    });
                builder.Services.AddScoped<JwtHandler>();
                builder.Services.AddScoped<IUserRepository, UserRepository>();
                builder.Services.AddScoped<IDogRepository, DogRepository>();
                builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

                #endregion

                var emailConfig = builder.Configuration
                                        .GetSection("EmailConfiguration")
                                        .Get<EmailConfiguration>();
                //builder.Configuration.Sources.ToList()
                //    .ForEach(source => Console.WriteLine(source));
                builder.Services.AddSingleton(emailConfig);
                builder.Services.AddSingleton<IEmailService, EmailService>();
                builder.Services.AddSingleton<OutboxRecoverTask>();
                builder.Services.AddHostedService(
                    provider => provider.GetRequiredService<OutboxRecoverTask>());
                builder.Services.AddSingleton<OutboxCleanup>();
                builder.Services.AddHostedService(
                    provider => provider.GetRequiredService<OutboxCleanup>());


                Console.WriteLine("");
                builder.Services.AddControllers();

                //builder.Services.AddSpaStaticFiles(configuration =>
                //{
                //    configuration.RootPath = "wwwroot";
                //});

                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();
                builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
                {
                    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
                }));

                var app = builder.Build();

                app.UseMiddleware<EnableRequestBodyBufferingMiddleware>();

                // initialize database
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<CaniActivityContext>();
                    var userMngr = scope.ServiceProvider.GetRequiredService<UserManager<RegisteredUser>>();
                    var roleMngr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    await CaniActivityContext.InitializeAsync(db, userMngr, roleMngr);
                }

                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });

                string photosPath = Path.Combine(builder.Environment.ContentRootPath, "Photos");
                if (!Directory.Exists(photosPath))
                    Directory.CreateDirectory(photosPath);
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(photosPath),
                    RequestPath = "/Photos"
                });

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                    //app.UseMigrationEndpoint();
                }
                else
                {
                    //app.UseDefaultFiles();
                    //app.UseSpaStaticFiles();
                    app.UseStaticFiles();
                }

                if (!app.Environment.IsDevelopment())
                {
                    app.UseHttpsRedirection();
                }
                app.UseCors("corsapp");

                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();

                app.UseSpa(spa => spa.Options.SourcePath = "wwwroot");

                app.Run();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException?.Message);
                    Console.WriteLine(ex.InnerException?.StackTrace);
                }
                else
                {
                    Console.WriteLine("No inner exception");
                }
            }
        }
    }
}