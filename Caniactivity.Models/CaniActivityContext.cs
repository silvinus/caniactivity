using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Caniactivity.Models;

public class CaniActivityContext: IdentityDbContext<RegisteredUser>
{
    public CaniActivityContext(DbContextOptions<CaniActivityContext> options)
        : base(options)
    {
    }

    public DbSet<RegisteredUser> RegisteredUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    public static async Task InitializeAsync(CaniActivityContext context, UserManager<RegisteredUser> userMngr)
    {
        await context.Database.MigrateAsync();
        if(context.RegisteredUsers.Any())
            return;

        var fake = new Faker<RegisteredUser>()
            .Rules((f, r) => r.Id = Guid.NewGuid().ToString())
            .Rules((f, r) => r.FirstName = f.Name.FirstName())
            .Rules((f, r) => r.LastName = f.Name.LastName())
            .Rules((f, r) => r.Email = f.Person.Email)
            .Rules((f, r) => r.UserName = r.Email)
            .Rules((f, r) => r.NormalizedUserName = r.UserName.ToUpperInvariant())
            .Rules((f, r) => r.Phone = f.Phone.PhoneNumber())
            .RuleFor(r => r.Dogs, (f, r) => f.Make(f.Random.Number(1, 3), () => new Dog()
            {
                Id = Guid.NewGuid(),
                Name = f.Name.FirstName(),
                Handler = r,
                Breed = f.Random.Word()
            }));

        var users = fake.Generate(100);
        users.ForEach(async (user) => await userMngr.CreateAsync(user, "Aveizieux$1"));
        await context.SaveChangesAsync();
    }
}

public class RegisteredUser: IdentityUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public SSOProvider Provider { get; set; }
    public RegisteredUserStatus Status { get; set; }
    public ICollection<Dog> Dogs { get; set; } = new List<Dog>();
}

public class Dog
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Breed { get; set; } = "";
    public RegisteredUser? Handler { get; set; }
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