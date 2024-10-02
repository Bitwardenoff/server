﻿using Bit.Core.Models.Data.Organizations.OrganizationUsers;
using Bit.Core.Repositories;
using Bit.Scim.Users.Interfaces;

namespace Bit.Scim.Users;

public class GetUsersListQuery : IGetUsersListQuery
{
    private readonly IOrganizationUserRepository _organizationUserRepository;

    public GetUsersListQuery(IOrganizationUserRepository organizationUserRepository)
    {
        _organizationUserRepository = organizationUserRepository;
    }

    public async Task<(IEnumerable<OrganizationUserUserDetails> userList, int totalResults)> GetUsersListAsync(Guid organizationId, string filter, int? count, int? startIndex)
    {
        string emailFilter = null;
        string usernameFilter = null;
        string externalIdFilter = null;
        count ??= 50;
        startIndex ??= 1;

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var filterLower = filter.ToLowerInvariant();
            if (filterLower.StartsWith("username eq "))
            {
                usernameFilter = filterLower.Substring(12).Trim('"');
                if (usernameFilter.Contains("@"))
                {
                    emailFilter = usernameFilter;
                }
            }
            else if (filterLower.StartsWith("externalid eq "))
            {
                externalIdFilter = filter.Substring(14).Trim('"');
            }
        }

        var userList = new List<OrganizationUserUserDetails>();
        var orgUsers = await _organizationUserRepository.GetManyDetailsByOrganizationAsync(organizationId);
        var totalResults = 0;
        if (!string.IsNullOrWhiteSpace(emailFilter))
        {
            var orgUser = orgUsers.FirstOrDefault(ou => ou.Email.ToLowerInvariant() == emailFilter);
            if (orgUser != null)
            {
                userList.Add(orgUser);
            }
            totalResults = userList.Count;
        }
        else if (!string.IsNullOrWhiteSpace(externalIdFilter))
        {
            var orgUser = orgUsers.FirstOrDefault(ou => ou.ExternalId == externalIdFilter);
            if (orgUser != null)
            {
                userList.Add(orgUser);
            }
            totalResults = userList.Count;
        }
        else if (string.IsNullOrWhiteSpace(filter))
        {
            userList = orgUsers.OrderBy(ou => ou.Email)
                .Skip(startIndex.Value - 1)
                .Take(count.Value)
                .ToList();
            totalResults = orgUsers.Count;
        }

        return (userList, totalResults);
    }
}
