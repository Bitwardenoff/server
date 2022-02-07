﻿using System.Threading.Tasks;
using Bit.Core.Entities;

namespace Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise.Interfaces
{
    public interface ISendSponsorshipOfferCommand
    {
        Task SendSponsorshipOfferAsync(OrganizationSponsorship sponsorship, string sponsoringEmail);
        Task SendSponsorshipOfferAsync(Organization sponsoringOrg, OrganizationUser sponsoringOrgUser,
            OrganizationSponsorship sponsorship, string sponsoringUserEmail);
    }
}
