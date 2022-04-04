﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Core.Entities;

namespace Bit.Core.Repositories
{
    public interface IOrganizationSponsorshipRepository : IRepository<OrganizationSponsorship, Guid>
    {
        Task<ICollection<Guid>> CreateManyAsync(IEnumerable<OrganizationSponsorship> organizationSponsorships);
        Task ReplaceManyAsync(IEnumerable<OrganizationSponsorship> organizationSponsorships);
        Task UpsertManyAsync(IEnumerable<OrganizationSponsorship> organizationSponsorships);
        Task DeleteManyAsync(IEnumerable<Guid> organizationSponsorshipIds);
        Task DeleteExpired();
        Task<ICollection<OrganizationSponsorship>> GetManyBySponsoringOrganizationAsync(Guid sponsoringOrganizationId);
        Task<OrganizationSponsorship> GetBySponsoringOrganizationUserIdAsync(Guid sponsoringOrganizationUserId);
        Task<OrganizationSponsorship> GetBySponsoredOrganizationIdAsync(Guid sponsoredOrganizationId);
        Task<DateTime?> GetLatestSyncDateBySponsoringOrganizationIdAsync(Guid sponsoringOrganizationId);
    }
}
