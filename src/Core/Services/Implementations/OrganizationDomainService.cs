﻿using Bit.Core.Enums;
using Bit.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace Bit.Core.Services;

public class OrganizationDomainService : IOrganizationDomainService
{
    private readonly IOrganizationDomainRepository _domainRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IDnsResolverService _dnsResolverService;
    private readonly IEventService _eventService;
    private readonly IMailService _mailService;
    private readonly ILogger<OrganizationDomainService> _logger;

    public OrganizationDomainService(
        IOrganizationDomainRepository domainRepository,
        IOrganizationUserRepository organizationUserRepository,
        IDnsResolverService dnsResolverService,
        IEventService eventService,
        IMailService mailService,
        ILogger<OrganizationDomainService> logger)
    {
        _domainRepository = domainRepository;
        _organizationUserRepository = organizationUserRepository;
        _dnsResolverService = dnsResolverService;
        _eventService = eventService;
        _mailService = mailService;
        _logger = logger;
    }

    public async Task ValidateOrganizationsDomainAsync()
    {
        //Date should be set 1 hour behind to ensure it selects all domains that should be verified
        var runDate = DateTime.UtcNow.AddHours(-1);

        var verifiableDomains = await _domainRepository.GetManyByNextRunDateAsync(runDate);
        _logger.LogInformation(Constants.BypassFiltersEventId, null,
            "Validating domains for {0} organizations.", verifiableDomains.Count);

        foreach (var domain in verifiableDomains)
        {
            try
            {
                _logger.LogInformation(Constants.BypassFiltersEventId, null,
                    "Attempting verification for {OrgId} with domain {Domain}", domain.OrganizationId, domain.DomainName);

                var status = await _dnsResolverService.ResolveAsync(domain.DomainName, domain.Txt);
                if (status)
                {
                    _logger.LogInformation(Constants.BypassFiltersEventId, "Successfully validated domain");
                    domain.SetVerifiedDate();
                    domain.SetJobRunCount();

                    await _domainRepository.ReplaceAsync(domain);
                    await _eventService.LogOrganizationDomainEventAsync(domain, EventType.OrganizationDomain_Verified,
                        EventSystemUser.SSO);
                    return;
                }

                domain.SetJobRunCount();
                domain.SetNextRunDate();
                await _domainRepository.ReplaceAsync(domain);
                await _eventService.LogOrganizationDomainEventAsync(domain, EventType.OrganizationDomain_NotVerified,
                    EventSystemUser.SSO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verification for organization {OrgId} with domain {Domain} failed", domain.OrganizationId, domain.DomainName);
            }
        }
    }

    public async Task OrganizationDomainMaintenanceAsync()
    {
        try
        {
            //Get domains that have not been verified within 72 hours
            var expiredDomains = await _domainRepository.GetExpiredOrganizationDomainsAsync();

            _logger.LogInformation(Constants.BypassFiltersEventId, null,
                "Attempting email reminder for {0} organizations.", expiredDomains.Count);

            foreach (var domain in expiredDomains)
            {
                //get admin emails of organization
                var admins = await _organizationUserRepository.GetManyByMinimumRoleAsync(domain.OrganizationId, OrganizationUserType.Admin);
                var adminEmails = admins.Select(a => a.Email).Distinct().ToList();

                //Send email to administrators
                if (adminEmails.Count > 0)
                {
                    await _mailService.SendUnverifiedOrganizationDomainEmailAsync(adminEmails,
                        domain.OrganizationId.ToString(), domain.DomainName);
                }
            }
            //delete domains that have not been verified within 7 days 
            var status = await _domainRepository.DeleteExpiredAsync();
            _logger.LogInformation(Constants.BypassFiltersEventId, null,
                "Delete status {0}", status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Organization domain maintenance failed");
        }
    }
}
