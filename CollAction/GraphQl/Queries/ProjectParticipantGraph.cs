﻿using CollAction.Data;
using CollAction.Models;
using GraphQL.EntityFramework;

namespace CollAction.GraphQl.Queries
{
    public sealed class ProjectParticipantGraph : EfObjectGraphType<ApplicationDbContext, ProjectParticipant>
    {
        public ProjectParticipantGraph(IEfGraphQLService<ApplicationDbContext> entityFrameworkGraphQlService) : base(entityFrameworkGraphQlService)
        {
            Field(x => x.SubscribedToProjectEmails);
            Field(x => x.UnsubscribeToken);
            Field(x => x.ParticipationDate);
            Field(x => x.UserId);
            Field(x => x.ProjectId);
            AddNavigationField(nameof(ProjectParticipant.Project), c => c.Source.Project);
            AddNavigationField(nameof(ProjectParticipant.User), c => c.Source.User, typeof(ApplicationUserGraph));
        }
    }
}
