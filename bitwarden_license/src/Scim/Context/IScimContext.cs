﻿using System;
using System.Threading.Tasks;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Models.OrganizationConnectionConfigs;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Microsoft.AspNetCore.Http;

namespace Bit.Scim.Context
{
    public interface IScimContext
    {
        HttpContext HttpContext { get; set; }
        ScimProviderType? RequestScimProvider { get; set; }
        ScimConfig ScimConfiguration { get; set; }
        Guid? OrganizationId { get; set; }
        Organization Organization { get; set; }
        Task BuildAsync(
            HttpContext httpContext,
            GlobalSettings globalSettings,
            IOrganizationRepository organizationRepository,
            IOrganizationConnectionRepository organizationConnectionRepository);
    }
}
