﻿using Bit.Core.Context;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.SecretsManager.Commands.AccessPolicies.Interfaces;
using Bit.Core.SecretsManager.Entities;
using Bit.Core.SecretsManager.Repositories;

namespace Bit.Commercial.Core.SecretsManager.Commands.AccessPolicies;

public class CreateAccessPoliciesCommand : ICreateAccessPoliciesCommand
{
    private readonly IAccessPolicyRepository _accessPolicyRepository;
    private readonly ICurrentContext _currentContext;
    private readonly IProjectRepository _projectRepository;

    public CreateAccessPoliciesCommand(
        IAccessPolicyRepository accessPolicyRepository,
        ICurrentContext currentContext,
        IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
        _accessPolicyRepository = accessPolicyRepository;
        _currentContext = currentContext;
    }

    public async Task<IEnumerable<BaseAccessPolicy>> CreateForProjectAsync(Guid projectId,
        List<BaseAccessPolicy> accessPolicies, Guid userId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null || !_currentContext.AccessSecretsManager(project.OrganizationId))
        {
            throw new NotFoundException();
        }

        var orgAdmin = await _currentContext.OrganizationAdmin(project.OrganizationId);
        var accessClient = AccessClientHelper.ToAccessClient(_currentContext.ClientType, orgAdmin);

        var hasAccess = accessClient switch
        {
            AccessClientType.NoAccessCheck => true,
            AccessClientType.User => await _projectRepository.UserHasWriteAccessToProject(project.Id, userId),
            _ => false,
        };

        if (!hasAccess)
        {
            throw new NotFoundException();
        }

        var distinctAccessPolicies = accessPolicies.DistinctBy(baseAccessPolicy =>
        {
            return baseAccessPolicy switch
            {
                UserProjectAccessPolicy ap => new Tuple<Guid?, Guid?>(ap.OrganizationUserId, ap.GrantedProjectId),
                GroupProjectAccessPolicy ap => new Tuple<Guid?, Guid?>(ap.GroupId, ap.GrantedProjectId),
                ServiceAccountProjectAccessPolicy ap => new Tuple<Guid?, Guid?>(ap.ServiceAccountId,
                    ap.GrantedProjectId),
                _ => throw new ArgumentException("Unsupported access policy type provided.", nameof(baseAccessPolicy)),
            };
        }).ToList();

        if (accessPolicies.Count != distinctAccessPolicies.Count)
        {
            throw new BadRequestException("Resources must be unique");
        }

        foreach (var accessPolicy in accessPolicies)
        {
            if (await _accessPolicyRepository.AccessPolicyExists(accessPolicy))
            {
                throw new BadRequestException("Resource already exists");
            }
        }
        await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        return await _accessPolicyRepository.GetManyByGrantedProjectIdAsync(projectId);
    }
}
