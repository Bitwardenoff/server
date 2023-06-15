﻿using Bit.Core.Entities;
using Bit.Core.Models.Business;
using Bit.Core.Models.StaticStore;

namespace Bit.Core.OrganizationFeatures.OrganizationPlanUpgrade.Interface;

public interface IValidateUpgradeCommand
{
    void ValidatePlan(Plan newPlan, Plan existingPlan);
    Task ValidateSeatsAsync(Organization organization, Plan passwordManagerPlan, OrganizationUpgrade upgrade);
    Task ValidateSmSeatsAsync(Organization organization, Plan newPlan, OrganizationUpgrade upgrade);
    Task ValidateServiceAccountAsync(Organization organization, Plan newPlan, OrganizationUpgrade upgrade);
    Task ValidateCollectionsAsync(Organization organization, Plan newPlan);
    Task ValidateGroupsAsync(Organization organization, Plan newPlan);
    Task ValidatePoliciesAsync(Organization organization, Plan newPlan);
    Task ValidateSsoAsync(Organization organization, Plan newPlan);
    Task ValidateKeyConnectorAsync(Organization organization, Plan newPlan);
    Task ValidateResetPasswordAsync(Organization organization, Plan newPlan);
    Task ValidateScimAsync(Organization organization, Plan newPlan);
    Task ValidateCustomPermissionsAsync(Organization organization, Plan newPlan);

}
