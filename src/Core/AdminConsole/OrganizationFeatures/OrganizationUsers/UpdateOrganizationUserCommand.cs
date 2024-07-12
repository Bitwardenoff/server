﻿#nullable enable
using Bit.Core.AdminConsole.OrganizationFeatures.OrganizationUsers.Interfaces;
using Bit.Core.AdminConsole.Repositories;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Models.Business;
using Bit.Core.Models.Data;
using Bit.Core.OrganizationFeatures.OrganizationSubscriptions.Interface;
using Bit.Core.Repositories;
using Bit.Core.Services;

namespace Bit.Core.AdminConsole.OrganizationFeatures.OrganizationUsers;

public class UpdateOrganizationUserCommand : IUpdateOrganizationUserCommand
{
    private readonly IEventService _eventService;
    private readonly IOrganizationService _organizationService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly ICountNewSmSeatsRequiredQuery _countNewSmSeatsRequiredQuery;
    private readonly IUpdateSecretsManagerSubscriptionCommand _updateSecretsManagerSubscriptionCommand;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IGroupRepository _groupRepository;

    public UpdateOrganizationUserCommand(
        IEventService eventService,
        IOrganizationService organizationService,
        IOrganizationRepository organizationRepository,
        IOrganizationUserRepository organizationUserRepository,
        ICountNewSmSeatsRequiredQuery countNewSmSeatsRequiredQuery,
        IUpdateSecretsManagerSubscriptionCommand updateSecretsManagerSubscriptionCommand,
        ICollectionRepository collectionRepository,
        IGroupRepository groupRepository)
    {
        _eventService = eventService;
        _organizationService = organizationService;
        _organizationRepository = organizationRepository;
        _organizationUserRepository = organizationUserRepository;
        _countNewSmSeatsRequiredQuery = countNewSmSeatsRequiredQuery;
        _updateSecretsManagerSubscriptionCommand = updateSecretsManagerSubscriptionCommand;
        _collectionRepository = collectionRepository;
        _groupRepository = groupRepository;
    }

    /// <summary>
    /// Update an organization user.
    /// </summary>
    /// <param name="user">The modified user to save.</param>
    /// <param name="savingUserId">The userId of the currently logged in user who is making the change.</param>
    /// <param name="collectionAccess">The user's updated collection access. If set to null, this removes all collection access.</param>
    /// <param name="groupAccess">The user's updated group access. If set to null, groups are not updated.</param>
    /// <exception cref="BadRequestException"></exception>
    public async Task UpdateUserAsync(OrganizationUser user, Guid? savingUserId,
        IEnumerable<CollectionAccessSelection>? collectionAccess, IEnumerable<Guid>? groupAccess)
    {
        // Avoid multiple enumeration
        collectionAccess = collectionAccess?.ToList();
        groupAccess = groupAccess?.ToList();

        if (user.Id.Equals(default(Guid)))
        {
            throw new BadRequestException("Invite the user first.");
        }

        var originalUser = await _organizationUserRepository.GetByIdAsync(user.Id);
        if (originalUser == null)
        {
            throw new NotFoundException("User not found.");
        }

        if (user.OrganizationId != originalUser.OrganizationId)
        {
            throw new BadRequestException("You cannot change a member's organization id.");
        }

        if (collectionAccess?.Any() == true)
        {
            await ValidateCollectionAccessAsync(originalUser, collectionAccess.ToList());
        }

        if (groupAccess?.Any() == true)
        {
            await ValidateGroupAccessAsync(originalUser, groupAccess.ToList());
        }

        if (savingUserId.HasValue)
        {
            await _organizationService.ValidateOrganizationUserUpdatePermissions(user.OrganizationId, user.Type, originalUser.Type, user.GetPermissions());
        }

        await _organizationService.ValidateOrganizationCustomPermissionsEnabledAsync(user.OrganizationId, user.Type);

        if (user.Type != OrganizationUserType.Owner &&
            !await _organizationService.HasConfirmedOwnersExceptAsync(user.OrganizationId, new[] { user.Id }))
        {
            throw new BadRequestException("Organization must have at least one confirmed owner.");
        }

        // If the organization is using Flexible Collections, prevent use of any deprecated permissions
        var organization = await _organizationRepository.GetByIdAsync(user.OrganizationId);
        if (organization.FlexibleCollections && user.AccessAll)
        {
            throw new BadRequestException("The AccessAll property has been deprecated by collection enhancements. Assign the user to collections instead.");
        }

        if (organization.FlexibleCollections && collectionAccess?.Any() == true)
        {
            var invalidAssociations = collectionAccess.Where(cas => cas.Manage && (cas.ReadOnly || cas.HidePasswords));
            if (invalidAssociations.Any())
            {
                throw new BadRequestException("The Manage property is mutually exclusive and cannot be true while the ReadOnly or HidePasswords properties are also true.");
            }
        }
        // End Flexible Collections

        // Only autoscale (if required) after all validation has passed so that we know it's a valid request before
        // updating Stripe
        if (!originalUser.AccessSecretsManager && user.AccessSecretsManager)
        {
            var additionalSmSeatsRequired = await _countNewSmSeatsRequiredQuery.CountNewSmSeatsRequiredAsync(user.OrganizationId, 1);
            if (additionalSmSeatsRequired > 0)
            {
                var update = new SecretsManagerSubscriptionUpdate(organization, true)
                    .AdjustSeats(additionalSmSeatsRequired);
                await _updateSecretsManagerSubscriptionCommand.UpdateSubscriptionAsync(update);
            }
        }

        if (user.AccessAll)
        {
            // We don't need any collections if we're flagged to have all access.
            collectionAccess = new List<CollectionAccessSelection>();
        }
        await _organizationUserRepository.ReplaceAsync(user, collectionAccess);

        if (groupAccess != null)
        {
            await _organizationUserRepository.UpdateGroupsAsync(user.Id, groupAccess);
        }

        await _eventService.LogOrganizationUserEventAsync(user, EventType.OrganizationUser_Updated);
    }

    private async Task ValidateCollectionAccessAsync(OrganizationUser originalUser,
        ICollection<CollectionAccessSelection> collectionAccess)
    {
        var collections = await _collectionRepository
            .GetManyByManyIdsAsync(collectionAccess.Select(c => c.Id));
        var collectionIds = collections.Select(c => c.Id);

        var missingCollectionId = collectionAccess
            .FirstOrDefault(cas => !collectionIds.Contains(cas.Id));
        if (missingCollectionId != default)
        {
            throw new BadRequestException($"Invalid collection id {missingCollectionId}.");
        }

        var invalidCollection = collections.FirstOrDefault(c => c.OrganizationId != originalUser.OrganizationId);
        if (invalidCollection != default)
        {
            // Use generic error message to avoid enumeration
            throw new BadRequestException($"Invalid collection id {invalidCollection.Id}.");
        }
    }

    private async Task ValidateGroupAccessAsync(OrganizationUser originalUser,
        ICollection<Guid> groupAccess)
    {
        var groups = await _groupRepository.GetManyByManyIds(groupAccess);
        var groupIds = groups.Select(g => g.Id);

        var missingGroupId = groupAccess.FirstOrDefault(gId => !groupIds.Contains(gId));
        if (missingGroupId != default)
        {
            throw new BadRequestException($"Invalid group id {missingGroupId}.");
        }

        var invalidGroup = groups.FirstOrDefault(g => g.OrganizationId != originalUser.OrganizationId);
        if (invalidGroup != default)
        {
            // Use generic error message to avoid enumeration
            throw new BadRequestException($"Invalid group id {invalidGroup.Id}.");
        }
    }
}
