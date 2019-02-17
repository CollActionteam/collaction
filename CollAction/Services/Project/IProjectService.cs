﻿using CollAction.Models;
using CollAction.Models.ProjectViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CollAction.Services.Project
{
    public interface IProjectService
    {
        Task<Models.Project> GetProjectById(int id);
        Task<bool> AddParticipant(string userId, int projectId);
        Task<ProjectParticipant> GetParticipant(string userId, int projectId);
        Task<string> GenerateParticipantsDataExport(int projectId);
        bool CanSendProjectEmail(Models.Project project);
        Task SendProjectEmail(Models.Project project, string subject, string message, HttpRequest request, IUrlHelper helper);
        IQueryable<DisplayProjectViewModel> GetProjectDisplayViewModels(Expression<Func<Models.Project, bool>> filter);
        Task<FindProjectsViewModel> FindProject(int projectId);
        IQueryable<FindProjectsViewModel> FindProjects(Expression<Func<Models.Project, bool>> filter, int? limit);
        int NumberEmailsAllowedToSend(Models.Project project);
        DateTime CanSendEmailsUntil(Models.Project project);
        Task RefreshParticipantCountMaterializedView();
        string GetProjectNameNormalized(string projectName);
        Task<IEnumerable<FindProjectsViewModel>> MyProjects(string userId);
        Task<IEnumerable<FindProjectsViewModel>> ParticipatingInProjects(string userId);
        Task<bool> ToggleNewsletterSubscription(int projectId, string userId);
    }
}
