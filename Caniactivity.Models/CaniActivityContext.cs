using Caniactivity.Models.Attributes;
using Caniactivity.Models.Converter;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Caniactivity.Models;

public class CaniActivityContext: IdentityDbContext<RegisteredUser>
{
    private readonly IConfiguration _configuration;
    public CaniActivityContext(DbContextOptions options, IConfiguration configuration)
        : base(options)
    {
        this._configuration = configuration;
    }

    public DbSet<RegisteredUser> RegisteredUsers { get; set; }
    public DbSet<Dog> Dog { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<EmailOutbox> Outbox { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var provider = this._configuration["provider"];
        if (provider == "Postgres")
            modelBuilder.ApplyUtcDateTimeConverter();
    }

    public static async Task InitializeAsync(CaniActivityContext context, 
        UserManager<RegisteredUser> userMngr, RoleManager<IdentityRole> roleMngr)
    {
        string[] roleNames = { "Administrator", "Member" };
        RegisteredUser[] administrators =
        {
            new RegisteredUser()
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = "Aline",
                LastName = "BON",
                Email = "administrator@caniactivity.com",
                UserName = "Aline",
                Status = RegisteredUserStatus.Approved
            },
            new RegisteredUser()
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = "Sylvain",
                LastName = "CESARI",
                Email = "syl.cesari@hotmail.fr",
                UserName = "Sylvain",
                Status = RegisteredUserStatus.Approved
            }
        };

        await context.Database.MigrateAsync();

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleMngr.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                IdentityRole identityRole = new IdentityRole(roleName);
                await roleMngr.CreateAsync(identityRole);
                //await roleMngr.AddClaimAsync(identityRole, new Claim("permissions", "project.administrator"));
            }
        }

        foreach (var admin in administrators)
        {
            var adminExist = await userMngr.FindByEmailAsync(admin.Email);
            if (adminExist is null)
            {
                await userMngr.CreateAsync(admin, "Caniactivity$1");
                adminExist = admin;
            }
            var hasAdminRole = await userMngr.IsInRoleAsync(admin, Models.UserRoles.Admin);
            if(!hasAdminRole)
            {
                await userMngr.AddToRoleAsync(adminExist, "Administrator");
            }
        }

        await context.SaveChangesAsync();
    }
}

public class RegisteredUser: IdentityUser
{
    public const string ADMINISTRATOR_MAIL = "sylvain.cesari@hotmail.fr";

    public RegisteredUser()
    {
        this.Id = Guid.NewGuid().ToString();
    }

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public SSOProvider Provider { get; set; }
    public RegisteredUserStatus Status { get; set; }
    public ICollection<Dog> Dogs { get; set; } = new List<Dog>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public string? RefreshToken { get; set; }
    [IsUtc]
    public DateTime RefreshTokenExpiryTime { get; set; }
}

public class Dog
{
    public Dog()
    {
        this.Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Breed { get; set; } = "";
    public RegisteredUser? Handler { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public DogStatus Status { get; set; }
}

public class Appointment
{
    public Appointment()
    {
        this.Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public ICollection<Dog> Dogs { get; set; } = new List<Dog>();
    public AppointmentStatus Status { get; set; }
    public RegisteredUser RegisteredBy { get; set; }
}

public class EmailOutbox
{
    public EmailOutbox()
    {
        this.Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }
    public string To { get; set; } = String.Empty;
    public string Subject { get; set; } = String.Empty;
    public string Body { get; set; } = String.Empty;
    public bool IsProcessed { get; set; } = false;
}

public enum SSOProvider
{
    Local, Google
}

public enum RegisteredUserStatus
{
    Submitted,
    Approved,
    Rejected
}

public enum AppointmentStatus
{
    Submitted,
    Approved,
    Rejected
}

public enum DogStatus
{
    TestStandBy,
    TestApproved,
    TestRejected
}