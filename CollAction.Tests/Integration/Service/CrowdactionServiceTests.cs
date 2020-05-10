﻿using CollAction.Data;
using CollAction.GraphQl.Mutations.Input;
using CollAction.Models;
using CollAction.Services.Crowdactions;
using CollAction.Services.Crowdactions.Models;
using CollAction.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CollAction.Tests.Integration.Service
{
    [TestClass]
    [TestCategory("Integration")]
    public sealed class CrowdactionServiceTests : IntegrationTestBase
    {
        [TestMethod]
        public Task TestCrowdactionCreate()
            => WithServiceProvider(
                   async scope =>
                   {
                       ICrowdactionService crowdactionService = scope.ServiceProvider.GetRequiredService<ICrowdactionService>();
                       ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                       SignInManager<ApplicationUser> signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
                       var user = await context.Users.FirstAsync().ConfigureAwait(false);
                       var claimsPrincipal = await signInManager.CreateUserPrincipalAsync(user).ConfigureAwait(false);
                       var r = new Random();

                       var newCrowdaction =
                           new NewCrowdaction()
                           {
                               Name = "test" + Guid.NewGuid(),
                               Categories = new List<Category>() { Category.Other, Category.Health },
                               Description = Guid.NewGuid().ToString(),
                               DescriptionVideoLink = "https://www.youtube.com/embed/xY0XTysJUDY",
                               End = DateTime.Now.AddDays(30),
                               Start = DateTime.Now.AddDays(10),
                               Goal = Guid.NewGuid().ToString(),
                               CreatorComments = Guid.NewGuid().ToString(),
                               Proposal = Guid.NewGuid().ToString(),
                               Target = 40,
                               Tags = new string[3] { $"a{r.Next(1000)}", $"b{r.Next(1000)}", $"c{r.Next(1000)}" }
                           };
                       CrowdactionResult crowdactionResult = await crowdactionService.CreateCrowdaction(newCrowdaction, claimsPrincipal, CancellationToken.None).ConfigureAwait(false);
                       int? crowdactionId = crowdactionResult.Crowdaction?.Id;
                       Assert.IsNotNull(crowdactionId);
                       Crowdaction retrievedCrowdaction = await context.Crowdactions.Include(c => c.Tags).ThenInclude(t => t.Tag).FirstOrDefaultAsync(c => c.Id == crowdactionId).ConfigureAwait(false);
                       Assert.IsNotNull(retrievedCrowdaction);

                       Assert.IsTrue(crowdactionResult.Succeeded);
                       Assert.IsFalse(crowdactionResult.Errors.Any());
                       Assert.AreEqual(crowdactionResult.Crowdaction?.Name, retrievedCrowdaction.Name);
                       Assert.IsTrue(Enumerable.SequenceEqual(crowdactionResult.Crowdaction?.Tags.Select(t => t.Tag.Name).OrderBy(t => t), retrievedCrowdaction.Tags.Select(t => t.Tag.Name).OrderBy(t => t)));
                   });

        [TestMethod]
        public Task TestCrowdactionUpdate()
            => WithServiceProvider(
                   async scope =>
                   {
                       ICrowdactionService crowdactionService = scope.ServiceProvider.GetRequiredService<ICrowdactionService>();
                       ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                       SignInManager<ApplicationUser> signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
                       UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                       Crowdaction currentCrowdaction = await context.Crowdactions.Include(c => c.Owner).FirstAsync(c => c.OwnerId != null).ConfigureAwait(false);
                       var admin = (await userManager.GetUsersInRoleAsync(AuthorizationConstants.AdminRole).ConfigureAwait(false)).First();
                       var adminClaims = await signInManager.CreateUserPrincipalAsync(admin).ConfigureAwait(false);
                       var owner = await signInManager.CreateUserPrincipalAsync(currentCrowdaction.Owner ?? throw new InvalidOperationException("Owner is null")).ConfigureAwait(false);
                       var r = new Random();
                       var updatedCrowdaction =
                           new UpdatedCrowdaction()
                           {
                               Name = Guid.NewGuid().ToString(),
                               BannerImageFileId = currentCrowdaction.BannerImageFileId,
                               Categories = new[] { Category.Community, Category.Environment },
                               CreatorComments = currentCrowdaction.CreatorComments,
                               Description = currentCrowdaction.Description,
                               OwnerId = currentCrowdaction.OwnerId,
                               DescriptionVideoLink = "https://www.youtube-nocookie.com/embed/xY0XTysJUDY",
                               DescriptiveImageFileId = currentCrowdaction.DescriptiveImageFileId,
                               DisplayPriority = CrowdactionDisplayPriority.Top,
                               End = DateTime.Now.AddDays(30),
                               Start = DateTime.Now.AddDays(10),
                               Goal = Guid.NewGuid().ToString(),
                               Tags = new string[3] { $"a{r.Next(1000)}", $"b{r.Next(1000)}", $"c{r.Next(1000)}" },
                               Id = currentCrowdaction.Id,
                               NumberCrowdactionEmailsSent = 3,
                               Proposal = currentCrowdaction.Proposal,
                               Status = CrowdactionStatus.Running,
                               Target = 33
                           };
                       var newCrowdactionResult = await crowdactionService.UpdateCrowdaction(updatedCrowdaction, adminClaims, CancellationToken.None).ConfigureAwait(false);
                       Assert.IsTrue(newCrowdactionResult.Succeeded);
                       int? newCrowdactionId = newCrowdactionResult.Crowdaction?.Id;
                       Assert.IsNotNull(newCrowdactionId);
                       Crowdaction retrievedCrowdaction = await context.Crowdactions.Include(c => c.Tags).ThenInclude(t => t.Tag).FirstOrDefaultAsync(c => c.Id == newCrowdactionId).ConfigureAwait(false);

                       Assert.IsNotNull(retrievedCrowdaction);
                       Assert.AreEqual(updatedCrowdaction.Name, retrievedCrowdaction.Name);
                       Assert.IsTrue(Enumerable.SequenceEqual(updatedCrowdaction.Tags.OrderBy(t => t), retrievedCrowdaction.Tags.Select(t => t.Tag.Name).OrderBy(t => t)));
                   });

        [TestMethod]
        public Task TestCrowdactionCommitAnonymous()
            => WithServiceProvider(
                   async scope =>
                   {
                       // Setup
                       ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                       ICrowdactionService crowdactionService = scope.ServiceProvider.GetRequiredService<ICrowdactionService>();
                       SignInManager<ApplicationUser> signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
                       UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                       var user = await context.Users.FirstAsync().ConfigureAwait(false);
                       Crowdaction crowdaction = new Crowdaction($"test-{Guid.NewGuid()}", CrowdactionStatus.Running, user.Id, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), "t", "t", "t", null, null);
                       context.Crowdactions.Add(crowdaction);
                       await context.SaveChangesAsync().ConfigureAwait(false);

                       // Test
                       string testEmail = GetTestEmail();
                       var result = await crowdactionService.CommitToCrowdactionAnonymous(testEmail, crowdaction.Id, CancellationToken.None).ConfigureAwait(false);
                       Assert.AreEqual(AddParticipantScenario.AnonymousCreatedAndAdded, result.Scenario);
                       result = await crowdactionService.CommitToCrowdactionAnonymous(testEmail, crowdaction.Id, CancellationToken.None).ConfigureAwait(false);
                       Assert.AreEqual(AddParticipantScenario.AnonymousNotRegisteredPresentAndAlreadyParticipating, result.Scenario);
                   });

        [TestMethod]
        public Task TestCrowdactionCommitLoggedIn()
            => WithServiceProvider(
                   async scope =>
                   {
                       // Setup
                       ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                       ICrowdactionService crowdactionService = scope.ServiceProvider.GetRequiredService<ICrowdactionService>();
                       SignInManager<ApplicationUser> signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
                       UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                       var user = await context.Users.FirstAsync().ConfigureAwait(false);
                       var userClaim = await signInManager.CreateUserPrincipalAsync(user).ConfigureAwait(false);
                       var crowdaction = new Crowdaction($"test-{Guid.NewGuid()}", CrowdactionStatus.Running, user.Id, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), "t", "t", "t", null, null);
                       context.Crowdactions.Add(crowdaction);
                       await context.SaveChangesAsync().ConfigureAwait(false);

                       // Test
                       var result = await crowdactionService.CommitToCrowdactionLoggedIn(userClaim, crowdaction.Id, CancellationToken.None).ConfigureAwait(false);
                       Assert.AreEqual(AddParticipantScenario.LoggedInAndAdded, result.Scenario);
                       result = await crowdactionService.CommitToCrowdactionLoggedIn(userClaim, crowdaction.Id, CancellationToken.None).ConfigureAwait(false);
                       Assert.AreEqual(AddParticipantScenario.LoggedInAndNotAdded, result.Scenario);
                   });

        [TestMethod]
        public Task TestCrowdactionEmail()
            => WithServiceProvider(
                   async scope =>
                   {
                       ICrowdactionService crowdactionService = scope.ServiceProvider.GetRequiredService<ICrowdactionService>();
                       ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                       SignInManager<ApplicationUser> signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
                       UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                       var user = await context.Users.FirstAsync().ConfigureAwait(false);
                       var claimsUser = await signInManager.CreateUserPrincipalAsync(user).ConfigureAwait(false);
                       var newCrowdaction =
                           new Crowdaction(
                               name: $"test{Guid.NewGuid()}",
                               categories: new List<CrowdactionCategory>() { new CrowdactionCategory(Category.Environment), new CrowdactionCategory(Category.Community) },
                               tags: new List<CrowdactionTag>(),
                               description: Guid.NewGuid().ToString(),
                               descriptionVideoLink: Guid.NewGuid().ToString(),
                               start: DateTime.Now.AddDays(-10),
                               end: DateTime.Now.AddDays(30),
                               goal: Guid.NewGuid().ToString(),
                               creatorComments: Guid.NewGuid().ToString(),
                               proposal: Guid.NewGuid().ToString(),
                               target: 40,
                               status: CrowdactionStatus.Running,
                               displayPriority: CrowdactionDisplayPriority.Medium,
                               ownerId: user.Id);
                       context.Crowdactions.Add(newCrowdaction);
                       await context.SaveChangesAsync().ConfigureAwait(false);

                       Assert.AreEqual(0, newCrowdaction.NumberCrowdactionEmailsSent);
                       Assert.IsTrue(crowdactionService.CanSendCrowdactionEmail(newCrowdaction));
                       await crowdactionService.SendCrowdactionEmail(newCrowdaction.Id, "test", "test", claimsUser, CancellationToken.None).ConfigureAwait(false);
                       Assert.AreEqual(1, newCrowdaction.NumberCrowdactionEmailsSent);
                       Assert.IsTrue(crowdactionService.CanSendCrowdactionEmail(newCrowdaction));
                       for (int i = 0; i < 3; i++)
                       {
                           await crowdactionService.SendCrowdactionEmail(newCrowdaction.Id, "test", "test", claimsUser, CancellationToken.None).ConfigureAwait(false);
                       }

                       Assert.AreEqual(4, newCrowdaction.NumberCrowdactionEmailsSent);
                       Assert.IsFalse(crowdactionService.CanSendCrowdactionEmail(newCrowdaction));
                   });

        [TestMethod]
        public Task TestCrowdactionSearch()
            => WithServiceProvider(
                   async scope =>
                   {
                       ICrowdactionService crowdactionService = scope.ServiceProvider.GetRequiredService<ICrowdactionService>();
                       Random r = new Random();

                       Assert.IsTrue(await crowdactionService.SearchCrowdactions(null, null).AnyAsync().ConfigureAwait(false));

                       Category searchCategory = (Category)r.Next(7);
                       Assert.IsTrue(await crowdactionService.SearchCrowdactions(searchCategory, null).Include(c => c.Categories).AllAsync(c => c.Categories.Any(pc => pc.Category == searchCategory)).ConfigureAwait(false));
                       Assert.IsTrue(await crowdactionService.SearchCrowdactions(null, SearchCrowdactionStatus.Closed).AllAsync(c => c.End < DateTime.UtcNow).ConfigureAwait(false));
                       Assert.IsTrue(await crowdactionService.SearchCrowdactions(searchCategory, SearchCrowdactionStatus.Closed).AllAsync(c => c.End < DateTime.UtcNow).ConfigureAwait(false));
                       Assert.IsTrue(await crowdactionService.SearchCrowdactions(null, SearchCrowdactionStatus.ComingSoon).AllAsync(c => c.Start > DateTime.UtcNow && c.Status == CrowdactionStatus.Running).ConfigureAwait(false));
                       Assert.IsTrue(await crowdactionService.SearchCrowdactions(searchCategory, SearchCrowdactionStatus.ComingSoon).AllAsync(c => c.Start > DateTime.UtcNow && c.Status == CrowdactionStatus.Running).ConfigureAwait(false));
                       Assert.IsTrue(await crowdactionService.SearchCrowdactions(null, SearchCrowdactionStatus.Open).AllAsync(c => c.Start <= DateTime.UtcNow && c.End >= DateTime.UtcNow && c.Status == CrowdactionStatus.Running).ConfigureAwait(false));
                       Assert.IsTrue(await crowdactionService.SearchCrowdactions(searchCategory, SearchCrowdactionStatus.Open).AllAsync(c => c.Start <= DateTime.UtcNow && c.End >= DateTime.UtcNow && c.Status == CrowdactionStatus.Running).ConfigureAwait(false));
                   });

        protected override void ConfigureReplacementServicesProvider(IServiceCollection collection)
        {
            collection.AddTransient(s => new Mock<IEmailSender>().Object);
        }

        private static string GetTestEmail()
            => $"collaction-test-email-{Guid.NewGuid()}@collaction.org";
    }
}
