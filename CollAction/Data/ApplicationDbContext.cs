﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CollAction.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using CollAction.Data.Geonames;

namespace CollAction.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectParticipant> ProjectParticipants { get; set; }
        public DbSet<ProjectTag> ProjectTags { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<LocationContinent> LocationContinents { get; set; }
        public DbSet<LocationCountry> LocationCountries { get; set; }
        public DbSet<LocationAlternateName> LocationAlternateNames { get; set; }
        public DbSet<LocationLevel1> LocationLevel1 { get; set; }
        public DbSet<LocationLevel2> LocationLevel2 { get; set; }
        public DbSet<ImageFile> ImageFiles { get; set; }
        public DbSet<VideoLink> VideoLinks { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<ProjectParticipantCount> ProjectParticipantCounts { get; set; }
        public DbSet<UserEvent> UserEvents { get; set; }
        public DbSet<DonationEventLog> DonationEventLog { get; set; }

        /// <summary>
        /// Configure the model (foreign keys, relations, primary keys, etc)
        /// </summary>
        /// <param name="builder">Model builder</param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Tag>()
                   .HasAlternateKey(t => t.Name);
            builder.Entity<Project>()
                   .HasIndex(p => p.Name)
                   .HasName("IX_Projects_Name").IsUnique();
            builder.Entity<Project>()
                   .Property(p => p.DisplayPriority)
                   .HasDefaultValue(ProjectDisplayPriority.Medium);
            builder.Entity<ApplicationUser>()
                   .HasMany(p => p.Projects)
                   .WithOne(proj => proj.Owner)
                   .HasForeignKey(proj => proj.OwnerId)
                   .OnDelete(DeleteBehavior.SetNull);
            builder.Entity<ProjectParticipant>()
                   .HasKey("UserId", "ProjectId");
            builder.Entity<ProjectTag>()
                   .HasKey("TagId", "ProjectId");
            builder.Entity<Project>()
                   .HasOne(p => p.ParticipantCounts)
                   .WithOne(p => p.Project)
                   .HasForeignKey<ProjectParticipantCount>(p => p.ProjectId);
            builder.Entity<Location>()
                   .HasOne(l => l.Country)
                   .WithMany(c => c.Locations)
                   .HasForeignKey(l => l.CountryId)
                   .OnDelete(DeleteBehavior.SetNull);
            builder.Entity<LocationCountry>()
                   .HasOne(c => c.Location);
            builder.Entity<Location>()
                   .HasOne(l => l.Level1)
                   .WithMany(l => l.Locations)
                   .HasForeignKey(l => l.Level1Id)
                   .OnDelete(DeleteBehavior.SetNull);
            builder.Entity<LocationLevel1>().HasOne(l => l.Location);
            builder.Entity<Location>().HasOne(l => l.Level2)
                                      .WithMany(l => l.Locations)
                                      .HasForeignKey(l => l.Level2Id)
                                      .OnDelete(DeleteBehavior.SetNull);
            builder.Entity<LocationLevel2>().HasOne(l => l.Location);
            builder.Entity<ApplicationUser>().Property(u => u.RepresentsNumberParticipants).HasDefaultValue(1);
        }

        /// <summary>
        /// Seed the database with initialisation data here
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="roleManager">Role managers to create and query roles</param>
        /// <param name="userManager">User manager to create and query users</param>
        /// <param name="token">Cancellation token</param>
        public async Task Seed(IConfiguration configuration, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            ChangeTracker.AutoDetectChangesEnabled = false;
            await CreateAdminRoleAndUser(configuration, userManager, roleManager);
            await CreateCategories();
            await ImportLocationData(configuration);
            await SeedTestProjects(configuration, userManager);
        }

        private async Task ImportLocationData(IConfiguration configuration)
        {
            if (configuration["ImportLocationData"].Equals("1", StringComparison.Ordinal))
                await new GeonamesImporter(this).ImportLocationData();
        }

        private async Task CreateAdminRoleAndUser(IConfiguration configuration, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Create admin role if not exists
            IdentityRole adminRole = await roleManager.FindByNameAsync(Constants.AdminRole);
            if (adminRole == null)
            {
                adminRole = new IdentityRole(Constants.AdminRole) { NormalizedName = Constants.AdminRole };
                IdentityResult result = await roleManager.CreateAsync(adminRole);
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Error creating role.{Environment.NewLine}{string.Join(Environment.NewLine, result.Errors.Select(e => $"{e.Code}: {e.Description}"))}");
            }

            // Create admin user if not exists
            string adminPassword = configuration["AdminPassword"];
            string adminEmail = configuration["AdminEmail"];
            ApplicationUser admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser(adminEmail) { EmailConfirmed = true };
                IdentityResult result = await userManager.CreateAsync(admin, adminPassword);
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Error creating user.{Environment.NewLine}{string.Join(Environment.NewLine, result.Errors.Select(e => $"{e.Code}: {e.Description}"))}");
            }

            // Assign admin role if not assigned
            if (!(await userManager.IsInRoleAsync(admin, Constants.AdminRole)))
            {
                IdentityResult result = await userManager.AddToRoleAsync(admin, Constants.AdminRole);
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Error assigning admin role.{Environment.NewLine}{string.Join(Environment.NewLine, result.Errors.Select(e => $"{e.Code}: {e.Description}"))}");
            }

            DetachAll();
        }

        private async Task CreateCategories()
        {
            // Initialize categories
            if (!(await Categories.AnyAsync()))
            {
                Categories.AddRange(new[] {
                    new Category() { Name = "Environment", ColorHex = "E88424", Description = "Environment", File = "" },
                    new Category() { Name = "Community", ColorHex = "7B2164", Description = "Community", File = "" },
                    new Category() { Name = "Consumption", ColorHex = "9D1D20", Description = "Consumption", File = "" },
                    new Category() { Name = "Well-being", ColorHex = "3762AE", Description = "Well-being", File = "" },
                    new Category() { Name = "Governance", ColorHex = "29ABE2", Description = "Governance", File = "" },
                    new Category() { Name = "Health", ColorHex = "EB078C", Description = "Health", File = "" },
                    new Category() { Name = "Other", ColorHex = "007D43", Description = "Other", File = "" },
                });
                await SaveChangesAsync();
                DetachAll();
            }
        }

        private async Task SeedTestProjects(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            if (configuration.GetValue<bool>("SeedTestProjects") && !(await Projects.AnyAsync()))
            {
                Random r = new Random();
                ApplicationUser admin = await userManager.FindByEmailAsync(configuration["AdminEmail"]);
                Projects.AddRange(
                    Enumerable.Range(0, r.Next(20, 200))
                              .Select(i =>
                                  new Project()
                                  {
                                      Name = Guid.NewGuid().ToString(),
                                      Description = Guid.NewGuid().ToString(),
                                      Start = DateTime.Now.AddDays(r.Next(-10, 10)),
                                      End = DateTime.Now.AddDays(r.Next(20, 30)),
                                      AnonymousUserParticipants = r.Next(0, 5),
                                      CategoryId = r.Next(1, 5),
                                      CreatorComments = Guid.NewGuid().ToString(),
                                      DisplayPriority = (ProjectDisplayPriority)r.Next(0, 2),
                                      Goal = Guid.NewGuid().ToString(),
                                      OwnerId = admin.Id,
                                      Proposal = Guid.NewGuid().ToString(),
                                      Status = (ProjectStatus)r.Next(0, 4),
                                      Target = r.Next(1, 10000),
                                      NumberProjectEmailsSend = r.Next(0, 3)
                                  }));
                await SaveChangesAsync();
                await Database.ExecuteSqlCommandAsync(@"REFRESH MATERIALIZED VIEW CONCURRENTLY ""ProjectParticipantCounts"";");
            }
        }

        public void DetachAll()
        {
            foreach (EntityEntry entry in ChangeTracker.Entries().ToArray())
                if (entry.Entity != null)
                    entry.State = EntityState.Detached;
        }
    }
}
