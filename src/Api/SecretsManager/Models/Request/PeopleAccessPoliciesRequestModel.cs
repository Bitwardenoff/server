﻿using Bit.Core.Exceptions;
using Bit.Core.SecretsManager.Entities;
using Bit.Core.SecretsManager.Models.Data;

namespace Bit.Api.SecretsManager.Models.Request;

public class PeopleAccessPoliciesRequestModel
{
    public IEnumerable<AccessPolicyRequest> UserAccessPolicyRequests { get; set; }

    public IEnumerable<AccessPolicyRequest> GroupAccessPolicyRequests { get; set; }

    private static void CheckForDistinctAccessPolicies(IReadOnlyCollection<BaseAccessPolicy> accessPolicies)
    {
        var distinctAccessPolicies = accessPolicies.DistinctBy(baseAccessPolicy =>
        {
            return baseAccessPolicy switch
            {
                UserProjectAccessPolicy ap => new Tuple<Guid?, Guid?>(ap.OrganizationUserId, ap.GrantedProjectId),
                GroupProjectAccessPolicy ap => new Tuple<Guid?, Guid?>(ap.GroupId, ap.GrantedProjectId),
                ServiceAccountProjectAccessPolicy ap => new Tuple<Guid?, Guid?>(ap.ServiceAccountId,
                    ap.GrantedProjectId),
                UserServiceAccountAccessPolicy ap => new Tuple<Guid?, Guid?>(ap.OrganizationUserId,
                    ap.GrantedServiceAccountId),
                GroupServiceAccountAccessPolicy ap => new Tuple<Guid?, Guid?>(ap.GroupId, ap.GrantedServiceAccountId),
                _ => throw new ArgumentException("Unsupported access policy type provided.", nameof(baseAccessPolicy))
            };
        }).ToList();

        if (accessPolicies.Count != distinctAccessPolicies.Count)
        {
            throw new BadRequestException("Resources must be unique");
        }
    }

    public ProjectPeopleAccessPolicies ToProjectPeopleAccessPolicies(Guid grantedProjectId, Guid organizationId)
    {
        var userAccessPolicies = UserAccessPolicyRequests?
            .Select(x => x.ToUserProjectAccessPolicy(grantedProjectId, organizationId)).ToList();

        var groupAccessPolicies = GroupAccessPolicyRequests?
            .Select(x => x.ToGroupProjectAccessPolicy(grantedProjectId, organizationId)).ToList();
        var policies = new List<BaseAccessPolicy>();
        if (userAccessPolicies != null)
        {
            policies.AddRange(userAccessPolicies);
        }

        if (groupAccessPolicies != null)
        {
            policies.AddRange(groupAccessPolicies);
        }

        CheckForDistinctAccessPolicies(policies);

        return new ProjectPeopleAccessPolicies
        {
            Id = grantedProjectId,
            OrganizationId = organizationId,
            UserAccessPolicies = userAccessPolicies,
            GroupAccessPolicies = groupAccessPolicies
        };
    }
}
