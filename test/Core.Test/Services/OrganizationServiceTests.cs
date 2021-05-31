﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Models.Data;
using Bit.Core.Models.Table;
using Bit.Core.Models.Business;
using Bit.Core.Repositories;
using Bit.Core.Services;
using NSubstitute;
using Xunit;
using Bit.Core.Test.AutoFixture;
using Bit.Core.Exceptions;
using Bit.Core.Enums;
using Bit.Core.Test.AutoFixture.Attributes;
using Bit.Core.Test.AutoFixture.OrganizationFixtures;
using System.Text.Json;
using Bit.Core.Test.AutoFixture.OrganizationUserFixtures;
using Organization = Bit.Core.Models.Table.Organization;
using OrganizationUser = Bit.Core.Models.Table.OrganizationUser;
using Policy = Bit.Core.Models.Table.Policy;

namespace Bit.Core.Test.Services
{
    public class OrganizationServiceTests
    {
        // [Fact]
        [Theory, PaidOrganizationAutoData]
        public async Task OrgImportCreateNewUsers(SutProvider<OrganizationService> sutProvider, Guid userId,
            Organization org, List<OrganizationUserUserDetails> existingUsers, List<ImportedOrganizationUser> newUsers)
        {
            org.UseDirectory = true;
            org.Seats = 10;
            newUsers.Add(new ImportedOrganizationUser
            {
                Email = existingUsers.First().Email,
                ExternalId = existingUsers.First().ExternalId
            });
            var expectedNewUsersCount = newUsers.Count - 1;

            sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(org.Id).Returns(org);
            sutProvider.GetDependency<IOrganizationUserRepository>().GetManyDetailsByOrganizationAsync(org.Id)
                .Returns(existingUsers);
            sutProvider.GetDependency<IOrganizationUserRepository>().GetCountByOrganizationIdAsync(org.Id)
                .Returns(existingUsers.Count);

            await sutProvider.Sut.ImportAsync(org.Id, userId, null, newUsers, null, false);

            await sutProvider.GetDependency<IOrganizationUserRepository>().DidNotReceiveWithAnyArgs()
                .UpsertAsync(default);
            await sutProvider.GetDependency<IOrganizationUserRepository>().Received(1)
                .UpsertManyAsync(Arg.Is<IEnumerable<OrganizationUser>>(users => users.Count() == 0));
            await sutProvider.GetDependency<IOrganizationUserRepository>().DidNotReceiveWithAnyArgs()
                .CreateAsync(default);

            // Create new users
            await sutProvider.GetDependency<IOrganizationUserRepository>().Received(1)
                .CreateManyAsync(Arg.Is<IEnumerable<OrganizationUser>>(users => users.Count() == expectedNewUsersCount));
            await sutProvider.GetDependency<IMailService>().Received(1)
                .BulkSendOrganizationInviteEmailAsync(org.Name,
                Arg.Is<IEnumerable<(OrganizationUser, string)>>(messages => messages.Count() == expectedNewUsersCount));
            
            // Send events
            await sutProvider.GetDependency<IEventService>().Received(1)
                .LogOrganizationUserEventsAsync(Arg.Is<IEnumerable<(OrganizationUser, EventType, DateTime?)>>(events =>
                events.Count() == expectedNewUsersCount));
            await sutProvider.GetDependency<IReferenceEventService>().Received(1)
                .RaiseEventAsync(Arg.Is<ReferenceEvent>(referenceEvent =>
                referenceEvent.Type == ReferenceEventType.InvitedUsers && referenceEvent.Id == org.Id &&
                referenceEvent.Users == expectedNewUsersCount));
        }

        [Theory, PaidOrganizationAutoData]
        public async Task OrgImportCreateNewUsersAndMarryExistingUser(SutProvider<OrganizationService> sutProvider,
            Guid userId, Organization org, List<OrganizationUserUserDetails> existingUsers,
            List<ImportedOrganizationUser> newUsers)
        {
            org.UseDirectory = true;
            var reInvitedUser = existingUsers.First();
            reInvitedUser.ExternalId = null;
            newUsers.Add(new ImportedOrganizationUser
            {
                Email = reInvitedUser.Email,
                ExternalId = reInvitedUser.Email,
            });
            var expectedNewUsersCount = newUsers.Count - 1;

            sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(org.Id).Returns(org);
            sutProvider.GetDependency<IOrganizationUserRepository>().GetManyDetailsByOrganizationAsync(org.Id)
                .Returns(existingUsers);
            sutProvider.GetDependency<IOrganizationUserRepository>().GetCountByOrganizationIdAsync(org.Id)
                .Returns(existingUsers.Count);
            sutProvider.GetDependency<IOrganizationUserRepository>().GetByIdAsync(reInvitedUser.Id)
                .Returns(new OrganizationUser { Id = reInvitedUser.Id });

            await sutProvider.Sut.ImportAsync(org.Id, userId, null, newUsers, null, false);

            await sutProvider.GetDependency<IOrganizationUserRepository>().DidNotReceiveWithAnyArgs()
                .UpsertAsync(default);
            await sutProvider.GetDependency<IOrganizationUserRepository>().DidNotReceiveWithAnyArgs()
                .CreateAsync(default);
            await sutProvider.GetDependency<IOrganizationUserRepository>().DidNotReceiveWithAnyArgs()
                .CreateAsync(default, default);

            // Upserted existing user
            await sutProvider.GetDependency<IOrganizationUserRepository>().Received(1)
                .UpsertManyAsync(Arg.Is<IEnumerable<OrganizationUser>>(users => users.Count() == 1));

            // Created and invited new users
            await sutProvider.GetDependency<IOrganizationUserRepository>().Received(1)
                .CreateManyAsync(Arg.Is<IEnumerable<OrganizationUser>>(users => users.Count() == expectedNewUsersCount));
            await sutProvider.GetDependency<IMailService>().Received(1)
                .BulkSendOrganizationInviteEmailAsync(org.Name,
                Arg.Is<IEnumerable<(OrganizationUser, string)>>(messages => messages.Count() == expectedNewUsersCount));

            // Sent events
            await sutProvider.GetDependency<IEventService>().Received(1)
                .LogOrganizationUserEventsAsync(Arg.Is<IEnumerable<(OrganizationUser, EventType, DateTime?)>>(events =>
                events.Where(e => e.Item2 == EventType.OrganizationUser_Invited).Count() == expectedNewUsersCount));
            await sutProvider.GetDependency<IReferenceEventService>().Received(1)
                .RaiseEventAsync(Arg.Is<ReferenceEvent>(referenceEvent => 
                referenceEvent.Type == ReferenceEventType.InvitedUsers && referenceEvent.Id == org.Id && 
                referenceEvent.Users == expectedNewUsersCount));
        }

        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task UpgradePlan_OrganizationIsNull_Throws(Guid organizationId, OrganizationUpgrade upgrade,
                SutProvider<OrganizationService> sutProvider)
        {
            sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organizationId).Returns(Task.FromResult<Organization>(null));
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => sutProvider.Sut.UpgradePlanAsync(organizationId, upgrade));
        }

        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task UpgradePlan_GatewayCustomIdIsNull_Throws(Organization organization, OrganizationUpgrade upgrade,
                SutProvider<OrganizationService> sutProvider)
        {
            organization.GatewayCustomerId = string.Empty;
            sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organization.Id).Returns(organization);
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.UpgradePlanAsync(organization.Id, upgrade));
            Assert.Contains("no payment method", exception.Message);
        }

        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task UpgradePlan_AlreadyInPlan_Throws(Organization organization, OrganizationUpgrade upgrade,
                SutProvider<OrganizationService> sutProvider)
        {
            upgrade.Plan = organization.PlanType;
            sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organization.Id).Returns(organization);
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.UpgradePlanAsync(organization.Id, upgrade));
            Assert.Contains("already on this plan", exception.Message);
        }

        [Theory, PaidOrganizationAutoData]
        public async Task UpgradePlan_UpgradeFromPaidPlan_Throws(Organization organization, OrganizationUpgrade upgrade,
                SutProvider<OrganizationService> sutProvider)
        {
            sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organization.Id).Returns(organization);
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.UpgradePlanAsync(organization.Id, upgrade));
            Assert.Contains("can only upgrade", exception.Message);
        }

        [Theory]
        [FreeOrganizationUpgradeAutoData]
        public async Task UpgradePlan_Passes(Organization organization, OrganizationUpgrade upgrade,
                SutProvider<OrganizationService> sutProvider)
        {
            sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organization.Id).Returns(organization);
            await sutProvider.Sut.UpgradePlanAsync(organization.Id, upgrade);
            await sutProvider.GetDependency<IOrganizationRepository>().Received(1).ReplaceAsync(organization);
        }

        [Theory]
        [OrganizationInviteAutoData]
        public async Task InviteUser_NoEmails_Throws(Organization organization, OrganizationUser invitor,
            OrganizationUserInvite invite, SutProvider<OrganizationService> sutProvider)
        {
            invite.Emails = null;
            sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organization.Id).Returns(organization);
            await Assert.ThrowsAsync<NotFoundException>(
                () => sutProvider.Sut.InviteUserAsync(organization.Id, invitor.UserId, null, invite));
        }

        [Theory]
        [OrganizationInviteAutoData(
            inviteeUserType: (int)OrganizationUserType.Owner,
            invitorUserType: (int)OrganizationUserType.Admin
        )]
        public async Task InviteUser_NonOwnerConfiguringOwner_Throws(Organization organization, OrganizationUserInvite invite,
            OrganizationUser invitor, SutProvider<OrganizationService> sutProvider)
        {
            var organizationRepository = sutProvider.GetDependency<IOrganizationRepository>();
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();

            organizationRepository.GetByIdAsync(organization.Id).Returns(organization);
            organizationUserRepository.GetManyByUserAsync(invitor.Id).Returns(new List<OrganizationUser> { invitor });

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.InviteUserAsync(organization.Id, invitor.UserId, null, invite));
            Assert.Contains("only an owner", exception.Message.ToLowerInvariant());
        }

        [Theory]
        [OrganizationInviteAutoData(
            inviteeUserType: (int)OrganizationUserType.Custom,
            invitorUserType: (int)OrganizationUserType.Admin
        )]
        public async Task InviteUser_NonAdminConfiguringAdmin_Throws(Organization organization, OrganizationUserInvite invite,
            OrganizationUser invitor, SutProvider<OrganizationService> sutProvider)
        {
            var organizationRepository = sutProvider.GetDependency<IOrganizationRepository>();
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();

            organizationRepository.GetByIdAsync(organization.Id).Returns(organization);
            organizationUserRepository.GetManyByUserAsync(invitor.Id).Returns(new List<OrganizationUser> { invitor });

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.InviteUserAsync(organization.Id, invitor.UserId, null, invite));
            Assert.Contains("only owners and admins", exception.Message.ToLowerInvariant());
        }

        [Theory]
        [OrganizationInviteAutoData(
            inviteeUserType: (int)OrganizationUserType.Manager,
            invitorUserType: (int)OrganizationUserType.Custom
        )]
        public async Task InviteUser_CustomUserWithoutManageUsersConfiguringUser_Throws(Organization organization, OrganizationUserInvite invite,
            OrganizationUser invitor, SutProvider<OrganizationService> sutProvider)
        {
            invitor.Permissions = JsonSerializer.Serialize(new Permissions() { ManageUsers = false },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });

            var organizationRepository = sutProvider.GetDependency<IOrganizationRepository>();
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();

            organizationRepository.GetByIdAsync(organization.Id).Returns(organization);
            organizationUserRepository.GetManyByUserAsync(invitor.UserId.Value).Returns(new List<OrganizationUser> { invitor });

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.InviteUserAsync(organization.Id, invitor.UserId, null, invite));
            Assert.Contains("account does not have permission", exception.Message.ToLowerInvariant());
        }

        [Theory]
        [OrganizationInviteAutoData(
            inviteeUserType: (int)OrganizationUserType.Admin,
            invitorUserType: (int)OrganizationUserType.Custom
        )]
        public async Task InviteUser_CustomUserConfiguringAdmin_Throws(Organization organization, OrganizationUserInvite invite,
            OrganizationUser invitor, SutProvider<OrganizationService> sutProvider)
        {
            invitor.Permissions = JsonSerializer.Serialize(new Permissions() { ManageUsers = true },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });

            var organizationRepository = sutProvider.GetDependency<IOrganizationRepository>();
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();

            organizationRepository.GetByIdAsync(organization.Id).Returns(organization);
            organizationUserRepository.GetManyByUserAsync(invitor.UserId.Value).Returns(new List<OrganizationUser> { invitor });

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.InviteUserAsync(organization.Id, invitor.UserId, null, invite));
            Assert.Contains("can not manage admins", exception.Message.ToLowerInvariant());
        }

        [Theory]
        [OrganizationInviteAutoData(
            inviteeUserType: (int)OrganizationUserType.User,
            invitorUserType: (int)OrganizationUserType.Owner
        )]
        public async Task InviteUser_NoPermissionsObject_Passes(Organization organization, OrganizationUserInvite invite,
            OrganizationUser invitor, SutProvider<OrganizationService> sutProvider)
        {
            invite.Permissions = null;
            var organizationRepository = sutProvider.GetDependency<IOrganizationRepository>();
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            var eventService = sutProvider.GetDependency<IEventService>();

            organizationRepository.GetByIdAsync(organization.Id).Returns(organization);
            organizationUserRepository.GetManyByUserAsync(invitor.UserId.Value).Returns(new List<OrganizationUser> { invitor });

            await sutProvider.Sut.InviteUserAsync(organization.Id, invitor.UserId, null, invite);
        }

        [Theory]
        [OrganizationInviteAutoData(
            inviteeUserType: (int)OrganizationUserType.User,
            invitorUserType: (int)OrganizationUserType.Custom
        )]
        public async Task InviteUser_Passes(Organization organization, OrganizationUserInvite invite,
            OrganizationUser invitor, SutProvider<OrganizationService> sutProvider)
        {
            invitor.Permissions = JsonSerializer.Serialize(new Permissions() { ManageUsers = true },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });

            var organizationRepository = sutProvider.GetDependency<IOrganizationRepository>();
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            var eventService = sutProvider.GetDependency<IEventService>();

            organizationRepository.GetByIdAsync(organization.Id).Returns(organization);
            organizationUserRepository.GetManyByUserAsync(invitor.UserId.Value).Returns(new List<OrganizationUser> { invitor });

            await sutProvider.Sut.InviteUserAsync(organization.Id, invitor.UserId, null, invite);
        }

        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task SaveUser_NoUserId_Throws(OrganizationUser user, Guid? savingUserId,
            IEnumerable<SelectionReadOnly> collections, SutProvider<OrganizationService> sutProvider)
        {
            user.Id = default(Guid);
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.SaveUserAsync(user, savingUserId, collections));
            Assert.Contains("invite the user first", exception.Message.ToLowerInvariant());
        }

        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task SaveUser_NoChangeToData_Throws(OrganizationUser user, Guid? savingUserId,
            IEnumerable<SelectionReadOnly> collections, SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            organizationUserRepository.GetByIdAsync(user.Id).Returns(user);
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.SaveUserAsync(user, savingUserId, collections));
            Assert.Contains("make changes before saving", exception.Message.ToLowerInvariant());
        }

        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task SaveUser_Passes(
            OrganizationUser oldUserData,
            OrganizationUser newUserData,
            IEnumerable<SelectionReadOnly> collections,
            [OrganizationUser(type: OrganizationUserType.Owner)]OrganizationUser savingUser,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();

            newUserData.Id = oldUserData.Id;
            newUserData.UserId = oldUserData.UserId;
            newUserData.OrganizationId = savingUser.OrganizationId = oldUserData.OrganizationId;
            organizationUserRepository.GetByIdAsync(oldUserData.Id).Returns(oldUserData);
            organizationUserRepository.GetManyByOrganizationAsync(savingUser.OrganizationId, OrganizationUserType.Owner)
                .Returns(new List<OrganizationUser> { savingUser });
            organizationUserRepository.GetManyByUserAsync(savingUser.UserId.Value).Returns(new List<OrganizationUser> { savingUser });

            await sutProvider.Sut.SaveUserAsync(newUserData, savingUser.UserId, collections);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task DeleteUser_InvalidUser(OrganizationUser organizationUser, OrganizationUser deletingUser,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();

            organizationUserRepository.GetByIdAsync(organizationUser.Id).Returns(organizationUser);

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.DeleteUserAsync(Guid.NewGuid(), organizationUser.Id, deletingUser.UserId));
            Assert.Contains("User not valid.", exception.Message);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task DeleteUser_RemoveYourself(OrganizationUser deletingUser, SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            
            organizationUserRepository.GetByIdAsync(deletingUser.Id).Returns(deletingUser);

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.DeleteUserAsync(deletingUser.OrganizationId, deletingUser.Id, deletingUser.UserId));
            Assert.Contains("You cannot remove yourself.", exception.Message);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task DeleteUser_NonOwnerRemoveOwner(
            [OrganizationUser(type: OrganizationUserType.Owner)]OrganizationUser organizationUser,
            [OrganizationUser(type: OrganizationUserType.Admin)]OrganizationUser deletingUser,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            
            organizationUser.OrganizationId = deletingUser.OrganizationId;
            organizationUserRepository.GetByIdAsync(organizationUser.Id).Returns(organizationUser);
            organizationUserRepository.GetManyByUserAsync(deletingUser.UserId.Value).Returns(new[] { deletingUser });

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.DeleteUserAsync(deletingUser.OrganizationId, organizationUser.Id, deletingUser.UserId));
            Assert.Contains("Only owners can delete other owners.", exception.Message);
        }

        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task DeleteUser_LastOwner(
            [OrganizationUser(type: OrganizationUserType.Owner)]OrganizationUser organizationUser,
            OrganizationUser deletingUser,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            
            organizationUser.OrganizationId = deletingUser.OrganizationId;
            organizationUserRepository.GetByIdAsync(organizationUser.Id).Returns(organizationUser);
            organizationUserRepository.GetManyByOrganizationAsync(deletingUser.OrganizationId, OrganizationUserType.Owner)
                .Returns(new[] { organizationUser });

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.DeleteUserAsync(deletingUser.OrganizationId, organizationUser.Id, null));
            Assert.Contains("Organization must have at least one confirmed owner.", exception.Message);
        }

        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task DeleteUser_Success(
            OrganizationUser organizationUser,
            [OrganizationUser(OrganizationUserStatusType.Confirmed, OrganizationUserType.Owner)]OrganizationUser deletingUser,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            
            organizationUser.OrganizationId = deletingUser.OrganizationId;
            organizationUserRepository.GetByIdAsync(organizationUser.Id).Returns(organizationUser);
            organizationUserRepository.GetByIdAsync(deletingUser.Id).Returns(deletingUser);
            organizationUserRepository.GetManyByUserAsync(deletingUser.UserId.Value).Returns(new[] { deletingUser });
            organizationUserRepository.GetManyByOrganizationAsync(deletingUser.OrganizationId, OrganizationUserType.Owner)
                .Returns(new[] {deletingUser, organizationUser});

            await sutProvider.Sut.DeleteUserAsync(deletingUser.OrganizationId, organizationUser.Id, deletingUser.UserId);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task DeleteUsers_FilterInvalid(OrganizationUser organizationUser, OrganizationUser deletingUser,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            var organizationUsers = new[] { organizationUser };
            var organizationUserIds = organizationUsers.Select(u => u.Id);
            organizationUserRepository.GetManyAsync(default).ReturnsForAnyArgs(organizationUsers);

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.DeleteUsersAsync(deletingUser.OrganizationId, organizationUserIds, deletingUser.UserId));
            Assert.Contains("Users invalid.", exception.Message);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task DeleteUsers_RemoveYourself(
            [OrganizationUser(OrganizationUserStatusType.Confirmed, OrganizationUserType.Owner)]OrganizationUser orgUser,
            OrganizationUser deletingUser,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            var organizationUsers = new[] { deletingUser };
            var organizationUserIds = organizationUsers.Select(u => u.Id);
            organizationUserRepository.GetManyAsync(default).ReturnsForAnyArgs(organizationUsers);
            organizationUserRepository.GetManyByOrganizationAsync(default, default).ReturnsForAnyArgs(new[] {orgUser});

            var result = await sutProvider.Sut.DeleteUsersAsync(deletingUser.OrganizationId, organizationUserIds, deletingUser.UserId);
            Assert.Contains("You cannot remove yourself.", result[0].Item2);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task DeleteUsers_NonOwnerRemoveOwner(
            [OrganizationUser(type: OrganizationUserType.Admin)]OrganizationUser deletingUser,
            [OrganizationUser(type: OrganizationUserType.Owner)]OrganizationUser orgUser1,
            [OrganizationUser(OrganizationUserStatusType.Confirmed)]OrganizationUser orgUser2,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            
            orgUser1.OrganizationId = orgUser2.OrganizationId = deletingUser.OrganizationId;
            var organizationUsers = new[] { orgUser1 };
            var organizationUserIds = organizationUsers.Select(u => u.Id);
            organizationUserRepository.GetManyAsync(default).ReturnsForAnyArgs(organizationUsers);
            organizationUserRepository.GetManyByOrganizationAsync(default, default).ReturnsForAnyArgs(new[] {orgUser2});

            var result = await sutProvider.Sut.DeleteUsersAsync(deletingUser.OrganizationId, organizationUserIds, deletingUser.UserId);
            Assert.Contains("Only owners can delete other owners.", result[0].Item2);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task DeleteUsers_LastOwner(
            [OrganizationUser(status: OrganizationUserStatusType.Confirmed, OrganizationUserType.Owner)]OrganizationUser orgUser,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();

            var organizationUsers = new[] { orgUser };
            var organizationUserIds = organizationUsers.Select(u => u.Id);
            organizationUserRepository.GetManyAsync(default).ReturnsForAnyArgs(organizationUsers);
            organizationUserRepository.GetManyByOrganizationAsync(orgUser.OrganizationId, OrganizationUserType.Owner).Returns(organizationUsers);

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.DeleteUsersAsync(orgUser.OrganizationId, organizationUserIds, null));
            Assert.Contains("Organization must have at least one confirmed owner.", exception.Message);
        }

        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task DeleteUsers_Success(
            [OrganizationUser(OrganizationUserStatusType.Confirmed, OrganizationUserType.Owner)]OrganizationUser deletingUser,
            [OrganizationUser(type: OrganizationUserType.Owner)]OrganizationUser orgUser1, OrganizationUser orgUser2,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            
            orgUser1.OrganizationId = orgUser2.OrganizationId = deletingUser.OrganizationId;
            var organizationUsers = new[] { orgUser1, orgUser2 };
            var organizationUserIds = organizationUsers.Select(u => u.Id);
            organizationUserRepository.GetManyAsync(default).ReturnsForAnyArgs(organizationUsers);
            organizationUserRepository.GetByIdAsync(deletingUser.Id).Returns(deletingUser);
            organizationUserRepository.GetManyByUserAsync(deletingUser.UserId.Value).Returns(new[] { deletingUser });
            organizationUserRepository.GetManyByOrganizationAsync(deletingUser.OrganizationId, OrganizationUserType.Owner)
                .Returns(new[] {deletingUser, orgUser1});

            await sutProvider.Sut.DeleteUsersAsync(deletingUser.OrganizationId, organizationUserIds, deletingUser.UserId);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task ConfirmUser_InvalidStatus(OrganizationUser confirmingUser,
            [OrganizationUser(OrganizationUserStatusType.Invited)]OrganizationUser orgUser, string key,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            var userService = Substitute.For<IUserService>();

            organizationUserRepository.GetByIdAsync(orgUser.Id).Returns(orgUser);

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.ConfirmUserAsync(orgUser.OrganizationId, orgUser.Id, key, confirmingUser.Id, userService));
            Assert.Contains("User not valid.", exception.Message);
        }

        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task ConfirmUser_WrongOrganization(OrganizationUser confirmingUser,
            [OrganizationUser(OrganizationUserStatusType.Accepted)]OrganizationUser orgUser, string key,
            SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            var userService = Substitute.For<IUserService>();

            organizationUserRepository.GetByIdAsync(orgUser.Id).Returns(orgUser);

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.ConfirmUserAsync(confirmingUser.OrganizationId, orgUser.Id, key, confirmingUser.Id, userService));
            Assert.Contains("User not valid.", exception.Message);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task ConfirmUser_AlreadyAdmin(Organization org, OrganizationUser confirmingUser,
            [OrganizationUser(OrganizationUserStatusType.Accepted, OrganizationUserType.Admin)]OrganizationUser orgUser, User user,
            string key, SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            var organizationRepository = sutProvider.GetDependency<IOrganizationRepository>();
            var userService = Substitute.For<IUserService>();
            var userRepository = sutProvider.GetDependency<IUserRepository>();

            org.PlanType = PlanType.Free;
            orgUser.OrganizationId = confirmingUser.OrganizationId = org.Id;
            orgUser.UserId = user.Id;
            organizationUserRepository.GetManyAsync(default).ReturnsForAnyArgs(new[] {orgUser});
            organizationUserRepository.GetCountByFreeOrganizationAdminUserAsync(orgUser.UserId.Value).Returns(1);
            organizationRepository.GetByIdAsync(org.Id).Returns(org);
            userRepository.GetManyAsync(default).ReturnsForAnyArgs(new[] {user});

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.ConfirmUserAsync(orgUser.OrganizationId, orgUser.Id, key, confirmingUser.Id, userService));
            Assert.Contains("User can only be an admin of one free organization.", exception.Message);
        }

        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task ConfirmUser_SingleOrgPolicy(Organization org, OrganizationUser confirmingUser,
            [OrganizationUser(OrganizationUserStatusType.Accepted)]OrganizationUser orgUser, User user,
            OrganizationUser orgUserAnotherOrg, [Policy(PolicyType.SingleOrg)]Policy singleOrgPolicy,
            string key, SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            var organizationRepository = sutProvider.GetDependency<IOrganizationRepository>();
            var policyRepository = sutProvider.GetDependency<IPolicyRepository>();
            var userRepository = sutProvider.GetDependency<IUserRepository>();
            var userService = Substitute.For<IUserService>();

            org.PlanType = PlanType.EnterpriseAnnually;
            orgUser.Status = OrganizationUserStatusType.Accepted;
            orgUser.OrganizationId = confirmingUser.OrganizationId = org.Id;
            orgUser.UserId = orgUserAnotherOrg.UserId = user.Id;
            organizationUserRepository.GetManyAsync(default).ReturnsForAnyArgs(new[] {orgUser});
            organizationUserRepository.GetManyByManyUsersAsync(default).ReturnsForAnyArgs(new[] {orgUserAnotherOrg});
            organizationRepository.GetByIdAsync(org.Id).Returns(org);
            userRepository.GetManyAsync(default).ReturnsForAnyArgs(new[] {user});
            policyRepository.GetManyByOrganizationIdAsync(org.Id).Returns(new[] {singleOrgPolicy});

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.ConfirmUserAsync(orgUser.OrganizationId, orgUser.Id, key, confirmingUser.Id, userService));
            Assert.Contains("User is a member of another organization.", exception.Message);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task ConfirmUser_TwoFactorPolicy(Organization org, OrganizationUser confirmingUser,
            [OrganizationUser(OrganizationUserStatusType.Accepted)]OrganizationUser orgUser, User user,
            OrganizationUser orgUserAnotherOrg, [Policy(PolicyType.TwoFactorAuthentication)]Policy twoFactorPolicy,
            string key, SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            var organizationRepository = sutProvider.GetDependency<IOrganizationRepository>();
            var policyRepository = sutProvider.GetDependency<IPolicyRepository>();
            var userRepository = sutProvider.GetDependency<IUserRepository>();
            var userService = Substitute.For<IUserService>();

            org.PlanType = PlanType.EnterpriseAnnually;
            orgUser.OrganizationId = confirmingUser.OrganizationId = org.Id;
            orgUser.UserId = orgUserAnotherOrg.UserId = user.Id;
            organizationUserRepository.GetManyAsync(default).ReturnsForAnyArgs(new[] {orgUser});
            organizationUserRepository.GetManyByManyUsersAsync(default).ReturnsForAnyArgs(new[] {orgUserAnotherOrg});
            organizationRepository.GetByIdAsync(org.Id).Returns(org);
            userRepository.GetManyAsync(default).ReturnsForAnyArgs(new[] {user});
            policyRepository.GetManyByOrganizationIdAsync(org.Id).Returns(new[] {twoFactorPolicy});

            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.ConfirmUserAsync(orgUser.OrganizationId, orgUser.Id, key, confirmingUser.Id, userService));
            Assert.Contains("User does not have two-step login enabled.", exception.Message);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task ConfirmUser_Success(Organization org, OrganizationUser confirmingUser,
            [OrganizationUser(OrganizationUserStatusType.Accepted)]OrganizationUser orgUser, User user,
            [Policy(PolicyType.TwoFactorAuthentication)]Policy twoFactorPolicy,
            [Policy(PolicyType.SingleOrg)]Policy singleOrgPolicy, string key, SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            var organizationRepository = sutProvider.GetDependency<IOrganizationRepository>();
            var policyRepository = sutProvider.GetDependency<IPolicyRepository>();
            var userRepository = sutProvider.GetDependency<IUserRepository>();
            var userService = Substitute.For<IUserService>();

            org.PlanType = PlanType.EnterpriseAnnually;
            orgUser.OrganizationId = confirmingUser.OrganizationId = org.Id;
            orgUser.UserId = user.Id;
            organizationUserRepository.GetManyAsync(default).ReturnsForAnyArgs(new[] {orgUser});
            organizationRepository.GetByIdAsync(org.Id).Returns(org);
            userRepository.GetManyAsync(default).ReturnsForAnyArgs(new[] {user});
            policyRepository.GetManyByOrganizationIdAsync(org.Id).Returns(new[] {twoFactorPolicy, singleOrgPolicy});
            userService.TwoFactorIsEnabledAsync(user).Returns(true);

            await sutProvider.Sut.ConfirmUserAsync(orgUser.OrganizationId, orgUser.Id, key, confirmingUser.Id, userService);
        }
        
        [Theory, CustomAutoData(typeof(SutProviderCustomization))]
        public async Task ConfirmUsers_Success(Organization org,
            OrganizationUser confirmingUser,
            [OrganizationUser(OrganizationUserStatusType.Accepted)]OrganizationUser orgUser1,
            [OrganizationUser(OrganizationUserStatusType.Accepted)]OrganizationUser orgUser2,
            [OrganizationUser(OrganizationUserStatusType.Accepted)]OrganizationUser orgUser3,
            OrganizationUser anotherOrgUser, User user1, User user2, User user3,
            [Policy(PolicyType.TwoFactorAuthentication)]Policy twoFactorPolicy,
            [Policy(PolicyType.SingleOrg)]Policy singleOrgPolicy, string key, SutProvider<OrganizationService> sutProvider)
        {
            var organizationUserRepository = sutProvider.GetDependency<IOrganizationUserRepository>();
            var organizationRepository = sutProvider.GetDependency<IOrganizationRepository>();
            var policyRepository = sutProvider.GetDependency<IPolicyRepository>();
            var userRepository = sutProvider.GetDependency<IUserRepository>();
            var userService = Substitute.For<IUserService>();

            org.PlanType = PlanType.EnterpriseAnnually;
            orgUser1.OrganizationId = orgUser2.OrganizationId = orgUser3.OrganizationId = confirmingUser.OrganizationId = org.Id;
            orgUser1.UserId = user1.Id;
            orgUser2.UserId = user2.Id;
            orgUser3.UserId = user3.Id;
            anotherOrgUser.UserId = user3.Id;
            var orgUsers = new[] {orgUser1, orgUser2, orgUser3};
            organizationUserRepository.GetManyAsync(default).ReturnsForAnyArgs(orgUsers);
            organizationRepository.GetByIdAsync(org.Id).Returns(org);
            userRepository.GetManyAsync(default).ReturnsForAnyArgs(new[] {user1, user2, user3});
            policyRepository.GetManyByOrganizationIdAsync(org.Id).Returns(new[] {twoFactorPolicy, singleOrgPolicy});
            userService.TwoFactorIsEnabledAsync(user1).Returns(true);
            userService.TwoFactorIsEnabledAsync(user2).Returns(false);
            userService.TwoFactorIsEnabledAsync(user3).Returns(true);
            organizationUserRepository.GetManyByManyUsersAsync(default)
                .ReturnsForAnyArgs(new[] {orgUser1, orgUser2, orgUser3, anotherOrgUser});

            var keys = orgUsers.ToDictionary(ou => ou.Id, _ => key);
            var result = await sutProvider.Sut.ConfirmUsersAsync(confirmingUser.OrganizationId, keys, confirmingUser.Id, userService);
            Assert.Contains("", result[0].Item2);
            Assert.Contains("User does not have two-step login enabled.", result[1].Item2);
            Assert.Contains("User is a member of another organization.", result[2].Item2);
        }
    }
}
