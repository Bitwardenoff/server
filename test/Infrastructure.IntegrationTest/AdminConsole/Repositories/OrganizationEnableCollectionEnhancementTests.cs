﻿using Bit.Core.AdminConsole.Entities;
using Bit.Core.AdminConsole.Repositories;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Models.Data;
using Bit.Core.Repositories;
using Xunit;

namespace Bit.Infrastructure.IntegrationTest.AdminConsole.Repositories;

public class OrganizationEnableCollectionEnhancementTests
{
    [DatabaseTheory, DatabaseData]
    public async Task Migrate_User_WithAccessAll_GivesCanEditAccessToAllCollections(
        IUserRepository userRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationUserRepository organizationUserRepository,
        ICollectionRepository collectionRepository)
    {
        var user = await CreateUser(userRepository);
        var organization = await CreateOrganization(organizationRepository);
        var orgUser = await CreateOrganizationUser(user, organization, OrganizationUserType.User, accessAll: true, organizationUserRepository);
        var collection1 = await CreateCollection(organization, collectionRepository);
        var collection2 = await CreateCollection(organization, collectionRepository);
        var collection3 = await CreateCollection(organization, collectionRepository);

        await organizationRepository.EnableCollectionEnhancements(organization.Id);

        var (updatedOrgUser, collectionAccessSelections) = await organizationUserRepository.GetDetailsByIdWithCollectionsAsync(orgUser.Id);

        Assert.False(updatedOrgUser.AccessAll);

        Assert.Equal(3, collectionAccessSelections.Count);
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection1.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: false });
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection2.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: false });
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection3.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: false });
    }

    [DatabaseTheory, DatabaseData]
    public async Task Migrate_Group_WithAccessAll_GivesCanEditAccessToAllCollections(
        IGroupRepository groupRepository,
        IOrganizationRepository organizationRepository,
        ICollectionRepository collectionRepository)
    {
        var organization = await CreateOrganization(organizationRepository);
        var group = await CreateGroup(organization, accessAll: true, groupRepository);
        var collection1 = await CreateCollection(organization, collectionRepository);
        var collection2 = await CreateCollection(organization, collectionRepository);
        var collection3 = await CreateCollection(organization, collectionRepository);

        await organizationRepository.EnableCollectionEnhancements(organization.Id);

        var (updatedGroup, collectionAccessSelections) = await groupRepository.GetByIdWithCollectionsAsync(group.Id);

        Assert.False(updatedGroup.AccessAll);

        Assert.Equal(3, collectionAccessSelections.Count);
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection1.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: false });
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection2.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: false });
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection3.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: false });
    }

    [DatabaseTheory, DatabaseData]
    public async Task Migrate_Manager_WithAccessAll_GivesCanManageAccessToAllCollections(
        IUserRepository userRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationUserRepository organizationUserRepository,
        ICollectionRepository collectionRepository)
    {
        var user = await CreateUser(userRepository);
        var organization = await CreateOrganization(organizationRepository);
        var orgUser = await CreateOrganizationUser(user, organization, OrganizationUserType.Manager, accessAll: true, organizationUserRepository);
        var collection1 = await CreateCollection(organization, collectionRepository);
        var collection2 = await CreateCollection(organization, collectionRepository);
        var collection3 = await CreateCollection(organization, collectionRepository);

        await organizationRepository.EnableCollectionEnhancements(organization.Id);

        var (updatedOrgUser, collectionAccessSelections) = await organizationUserRepository.GetDetailsByIdWithCollectionsAsync(orgUser.Id);

        Assert.False(updatedOrgUser.AccessAll);
        Assert.Equal(OrganizationUserType.User, updatedOrgUser.Type);

        Assert.Equal(3, collectionAccessSelections.Count);
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection1.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: true });
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection2.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: true });
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection3.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: true });
    }

    [DatabaseTheory, DatabaseData]
    public async Task Migrate_Manager_WithoutAccessAll_GivesCanManageAccessToAssignedCollections(
        IUserRepository userRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationUserRepository organizationUserRepository,
        ICollectionRepository collectionRepository)
    {
        var user = await CreateUser(userRepository);
        var organization = await CreateOrganization(organizationRepository);
        var orgUser = await CreateOrganizationUser(user, organization, OrganizationUserType.Manager, accessAll: false, organizationUserRepository);
        var collection1 = await CreateCollection(organization, collectionRepository, null, [new CollectionAccessSelection { Id = orgUser.Id, HidePasswords = true, ReadOnly = false, Manage = false }]);
        var collection2 = await CreateCollection(organization, collectionRepository, null, [new CollectionAccessSelection { Id = orgUser.Id, HidePasswords = false, ReadOnly = false, Manage = false }]);
        var collection3 = await CreateCollection(organization, collectionRepository); // no access

        await organizationRepository.EnableCollectionEnhancements(organization.Id);

        var (updatedOrgUser, collectionAccessSelections) = await organizationUserRepository.GetDetailsByIdWithCollectionsAsync(orgUser.Id);

        Assert.Equal(OrganizationUserType.User, updatedOrgUser.Type);

        Assert.Equal(2, collectionAccessSelections.Count);
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection1.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: true });
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection2.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: true });
        Assert.DoesNotContain(collectionAccessSelections, cas =>
            cas.Id == collection3.Id);
    }

    [DatabaseTheory, DatabaseData]
    public async Task Migrate_Manager_WithoutAccessAll_WithGroupWithAccessAll_GivesCanManageAccessToAllCollections(
        IUserRepository userRepository,
        IGroupRepository groupRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationUserRepository organizationUserRepository,
        ICollectionRepository collectionRepository)
    {
        var user = await CreateUser(userRepository);
        var organization = await CreateOrganization(organizationRepository);
        var orgUser = await CreateOrganizationUser(user, organization, OrganizationUserType.Manager, accessAll: false, organizationUserRepository);

        // Use 2 groups to make sure we account for overlapping access
        var group1 = await CreateGroup(organization, accessAll: true, groupRepository, orgUser);
        var group2 = await CreateGroup(organization, accessAll: true, groupRepository, orgUser);

        var collection1 = await CreateCollection(organization, collectionRepository);
        var collection2 = await CreateCollection(organization, collectionRepository);
        var collection3 = await CreateCollection(organization, collectionRepository);

        await organizationRepository.EnableCollectionEnhancements(organization.Id);

        var (updatedOrgUser, collectionAccessSelections) = await organizationUserRepository.GetDetailsByIdWithCollectionsAsync(orgUser.Id);

        Assert.Equal(OrganizationUserType.User, updatedOrgUser.Type);

        Assert.Equal(3, collectionAccessSelections.Count);
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection1.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: true });
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection2.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: true });
        Assert.Contains(collectionAccessSelections, cas =>
            cas.Id == collection3.Id &&
            cas is { HidePasswords: false, ReadOnly: false, Manage: true });
    }

    private async Task<User> CreateUser(IUserRepository userRepository)
    {
        return await userRepository.CreateAsync(new User
        {
            Name = "Test User",
            Email = $"test+{Guid.NewGuid()}@email.com",
            ApiKey = "TEST",
            SecurityStamp = "stamp",
        });
    }

    private async Task<Group> CreateGroup(Organization organization, bool accessAll, IGroupRepository groupRepository,
        OrganizationUser? orgUser = null)
    {
        var group = await groupRepository.CreateAsync(new Group
        {
            Name = $"Test Group {Guid.NewGuid()}",
            OrganizationId = organization.Id,
            AccessAll = accessAll
        });

        if (orgUser != null)
        {
            await groupRepository.UpdateUsersAsync(group.Id, [orgUser.Id]);
        }

        return group;
    }

    private async Task<Organization> CreateOrganization(IOrganizationRepository organizationRepository)
    {
        return await organizationRepository.CreateAsync(new Organization
        {
            Name = $"Test Org {Guid.NewGuid()}",
            BillingEmail = "Billing Email", // TODO: EF does not enforce this being NOT NULL
            Plan = "Test Plan", // TODO: EF does not enforce this being NOT NULl
        });
    }

    private async Task<OrganizationUser> CreateOrganizationUser(User user, Organization organization,
        OrganizationUserType type, bool accessAll, IOrganizationUserRepository organizationUserRepository)
    {
        return await organizationUserRepository.CreateAsync(new OrganizationUser
        {
            OrganizationId = organization.Id,
            UserId = user.Id,
            Status = OrganizationUserStatusType.Confirmed,
            Type = type,
            AccessAll = accessAll
        });
    }

    private async Task<Collection> CreateCollection(Organization organization, ICollectionRepository collectionRepository,
        IEnumerable<CollectionAccessSelection>? groups = null, IEnumerable<CollectionAccessSelection>? users = null)
    {
        var collection = new Collection { Name = $"Test collection {Guid.NewGuid()}", OrganizationId = organization.Id };
        await collectionRepository.CreateAsync(collection, groups: groups, users: users);
        return collection;
    }
}
