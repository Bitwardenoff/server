﻿using System.Text.Json;
using Bit.Infrastructure.EntityFramework.Repositories;
using Bit.IntegrationTestCommon.Factories;
using Bit.Scim.Models;
using Bit.Scim.Utilities;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Bit.Scim.IntegrationTest.Controllers.v2
{
    public class GroupsControllerTests : IClassFixture<ScimApplicationFactory>
    {
        private readonly ScimApplicationFactory _factory;

        private readonly Guid _testUserId1 = Guid.NewGuid();
        private readonly Guid _testUserId2 = Guid.NewGuid();
        private readonly Guid _testUserId3 = Guid.NewGuid();
        private readonly Guid _testGroupId1 = Guid.NewGuid();
        private readonly Guid _testGroupId2 = Guid.NewGuid();
        private readonly Guid _testGroupId3 = Guid.NewGuid();
        private readonly Guid _testOrganizationId1 = Guid.NewGuid();
        private readonly Guid _testOrganizationUserId1 = Guid.NewGuid();
        private readonly Guid _testOrganizationUserId2 = Guid.NewGuid();
        private readonly Guid _testOrganizationUserId3 = Guid.NewGuid();

        public GroupsControllerTests(ScimApplicationFactory factory)
        {
            _factory = factory;

            var databaseContext = factory.GetDatabaseContext();
            ReinitializeDbForTests(databaseContext);
        }

        [Fact]
        public async Task Get_Success()
        {
            var organizationId = _testOrganizationId1;
            var id = _testGroupId1;

            var context = await _factory.GetAsync(organizationId, id);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            var responseModel = JsonSerializer.Deserialize<ScimGroupResponseModel>(context.Response.Body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            Assert.Equal(_testGroupId1.ToString(), responseModel.Id);
            Assert.Equal("Test Group 1", responseModel.DisplayName);
            Assert.Equal("A", responseModel.ExternalId);
            //Assert.Equal(DateTime.Now.Date, responseModel.Meta.Created.Value.Date);
            //Assert.Equal(DateTime.Now.Date, responseModel.Meta.LastModified.Value.Date);
            Assert.Equal(new List<string> { ScimConstants.Scim2SchemaGroup }, responseModel.Schemas);
        }

        [Fact]
        public async Task Get_NotFound()
        {
            var organizationId = _testOrganizationId1;
            var id = Guid.NewGuid();
            var context = await _factory.GetAsync(organizationId, id);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

            var responseModel = JsonSerializer.Deserialize<ScimErrorResponseModel>(context.Response.Body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            Assert.Equal(404, responseModel.Status);
            Assert.Equal("Group not found.", responseModel.Detail);
            Assert.Equal(new List<string> { ScimConstants.Scim2SchemaError }, responseModel.Schemas);
        }

        [Fact]
        public async Task GetList_Success()
        {
            var organizationId = _testOrganizationId1;
            var context = await _factory.GetListAsync(organizationId, null, 2, 1);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            var responseModel = JsonSerializer.Deserialize<ScimListResponseModel<ScimGroupResponseModel>>(context.Response.Body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            Assert.Equal(2, responseModel.ItemsPerPage);
            Assert.Equal(3, responseModel.TotalResults);
            Assert.Equal(1, responseModel.StartIndex);

            Assert.Equal(2, responseModel.Resources.Count);
            //Assert.Equal(DateTime.Now.Date, group.Meta.Created.Value.Date);
            //Assert.Equal(DateTime.Now.Date, group.Meta.LastModified.Value.Date);
            Assert.Equal(new List<string> { ScimConstants.Scim2SchemaListResponse }, responseModel.Schemas);
        }

        [Fact]
        public async Task GetList_SearchDisplayName_Success()
        {
            var organizationId = _testOrganizationId1;
            var context = await _factory.GetListAsync(organizationId, "displayName eq Test Group 2", 10, 1);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            var responseModel = JsonSerializer.Deserialize<ScimListResponseModel<ScimGroupResponseModel>>(context.Response.Body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            Assert.Single(responseModel.Resources);
            Assert.Equal(10, responseModel.ItemsPerPage);
            Assert.Equal(1, responseModel.TotalResults);
            Assert.Equal(1, responseModel.StartIndex);

            var group = responseModel.Resources.Single();
            Assert.Equal(_testGroupId2.ToString(), group.Id);
            Assert.Equal("Test Group 2", group.DisplayName);
            Assert.Equal("B", group.ExternalId);
            //Assert.Equal(DateTime.Now.Date, group.Meta.Created.Value.Date);
            //Assert.Equal(DateTime.Now.Date, group.Meta.LastModified.Value.Date);
            Assert.Equal(new List<string> { ScimConstants.Scim2SchemaListResponse }, responseModel.Schemas);
        }

        [Fact]
        public async Task GetList_SearchExternalId_Success()
        {
            var organizationId = _testOrganizationId1;
            var context = await _factory.GetListAsync(organizationId, "externalId eq C", 10, 1);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            var responseModel = JsonSerializer.Deserialize<ScimListResponseModel<ScimGroupResponseModel>>(context.Response.Body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            Assert.Single(responseModel.Resources);
            Assert.Equal(10, responseModel.ItemsPerPage);
            Assert.Equal(1, responseModel.TotalResults);
            Assert.Equal(1, responseModel.StartIndex);

            var group = responseModel.Resources.Single();
            Assert.Equal(_testGroupId3.ToString(), group.Id);
            Assert.Equal("Test Group 3", group.DisplayName);
            Assert.Equal("C", group.ExternalId);
            //Assert.Equal(DateTime.Now.Date, group.Meta.Created.Value.Date);
            //Assert.Equal(DateTime.Now.Date, group.Meta.LastModified.Value.Date);
            Assert.Equal(new List<string> { ScimConstants.Scim2SchemaListResponse }, responseModel.Schemas);
        }

        [Fact]
        public async Task Post_Success()
        {
            var organizationId = _testOrganizationId1;
            var model = new ScimGroupRequestModel
            {
                DisplayName = "New Group",
                ExternalId = null,
                Members = null,
                Schemas = null
            };

            var context = await _factory.PostAsync(organizationId, model);

            Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Equal(4, databaseContext.Groups.Count());
            Assert.True(databaseContext.Groups.Any(g => g.Name == "New Group"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Post_InvalidDisplayName_BadRequest(string displayName)
        {
            var organizationId = _testOrganizationId1;
            var model = new ScimGroupRequestModel
            {
                DisplayName = displayName,
                ExternalId = null,
                Members = null,
                Schemas = null
            };

            var context = await _factory.PostAsync(organizationId, model);

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        }

        [Fact]
        public async Task Post_ExistingExternalId_Conflict()
        {
            var organizationId = _testOrganizationId1;
            var model = new ScimGroupRequestModel
            {
                DisplayName = "New Group",
                ExternalId = "A",
                Members = null,
                Schemas = null
            };

            var context = await _factory.PostAsync(organizationId, model);

            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Equal(3, databaseContext.Groups.Count());
            Assert.False(databaseContext.Groups.Any(g => g.Name == "New Group"));
        }

        [Fact]
        public async Task Put_ChangeName_Success()
        {
            var newGroupName = "Test Group 1 New Name";
            var organizationId = _testOrganizationId1;
            var id = _testGroupId1;
            var model = new ScimGroupRequestModel
            {
                DisplayName = newGroupName,
                ExternalId = "AA",
                Members = new List<ScimGroupRequestModel.GroupMembersModel>(),
                Schemas = new List<string>()
            };

            var context = await _factory.PutAsync(organizationId, id, model);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Equal(3, databaseContext.Groups.Count());

            var firstGroup = databaseContext.Groups.FirstOrDefault(g => g.Id == id);
            Assert.Equal(newGroupName, firstGroup.Name);
        }

        [Fact]
        public async Task Put_NotFound()
        {
            var newGroupName = "Test Group 1 New Name";
            var organizationId = _testOrganizationId1;
            var id = Guid.NewGuid();
            var model = new ScimGroupRequestModel
            {
                DisplayName = newGroupName,
                ExternalId = "AA",
                Members = new List<ScimGroupRequestModel.GroupMembersModel>(),
                Schemas = new List<string>()
            };

            var context = await _factory.PutAsync(organizationId, id, model);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

            var responseModel = JsonSerializer.Deserialize<ScimErrorResponseModel>(context.Response.Body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            Assert.Equal(404, responseModel.Status);
            Assert.Equal("Group not found.", responseModel.Detail);
            Assert.Equal(new List<string> { ScimConstants.Scim2SchemaError }, responseModel.Schemas);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Equal(3, databaseContext.Groups.Count());
            Assert.True(databaseContext.Groups.FirstOrDefault(g => g.Id == id) == null);
        }

        [Fact]
        public async Task Patch_ReplaceDisplayName_Success()
        {
            var organizationId = _testOrganizationId1;
            var id = _testGroupId1;
            var model = new ScimPatchModel
            {
                Operations = new List<ScimPatchModel.OperationModel>()
                {
                    new ScimPatchModel.OperationModel { Op = "replace", Value = JsonDocument.Parse("{\"displayName\":\"Patch Display Name\"}").RootElement  },
                },
                Schemas = new List<string>()
            };

            var context = await _factory.PatchAsync(organizationId, id, model);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Equal(3, databaseContext.Groups.Count());

            var group = databaseContext.Groups.FirstOrDefault(g => g.Id == id);
            Assert.Equal("Patch Display Name", group.Name);
        }

        [Fact]
        public async Task Patch_ReplaceMembers_Success()
        {
            var organizationId = _testOrganizationId1;
            var id = _testGroupId1;
            var model = new ScimPatchModel
            {
                Operations = new List<ScimPatchModel.OperationModel>()
                {
                    new ScimPatchModel.OperationModel { Op = "replace", Path = "members", Value = JsonDocument.Parse($"[{{\"value\":\"{_testOrganizationUserId2}\"}}]").RootElement  },
                },
                Schemas = new List<string>()
            };

            var context = await _factory.PatchAsync(organizationId, id, model);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Single(databaseContext.GroupUsers);

            var groupUser = databaseContext.GroupUsers.FirstOrDefault();
            Assert.Equal(_testOrganizationUserId2, groupUser.OrganizationUserId);
        }

        [Fact]
        public async Task Patch_AddSingleMember_Success()
        {
            var organizationId = _testOrganizationId1;
            var id = _testGroupId1;
            var model = new ScimPatchModel
            {
                Operations = new List<ScimPatchModel.OperationModel>()
                {
                    new ScimPatchModel.OperationModel { Op = "add", Path = $"members[value eq {_testOrganizationUserId2}", Value = JsonDocument.Parse("{}").RootElement },
                },
                Schemas = new List<string>()
            };

            var context = await _factory.PatchAsync(organizationId, id, model);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Equal(2, databaseContext.GroupUsers.Count());

            var groupUser1 = databaseContext.GroupUsers.FirstOrDefault();
            Assert.Equal(_testOrganizationUserId1, groupUser1.OrganizationUserId);

            var groupUser2 = databaseContext.GroupUsers.Skip(1).FirstOrDefault();
            Assert.Equal(_testOrganizationUserId2, groupUser2.OrganizationUserId);
        }

        [Fact]
        public async Task Patch_AddListMembers_Success()
        {
            var organizationId = _testOrganizationId1;
            var id = _testGroupId2;
            var model = new ScimPatchModel
            {
                Operations = new List<ScimPatchModel.OperationModel>()
                {
                    new ScimPatchModel.OperationModel { Op = "add", Path = "members", Value = JsonDocument.Parse($"[{{\"value\":\"{_testOrganizationUserId2}\"}},{{\"value\":\"{_testOrganizationUserId3}\"}}]").RootElement },
                },
                Schemas = new List<string>()
            };

            var context = await _factory.PatchAsync(organizationId, id, model);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Equal(3, databaseContext.GroupUsers.Count());
        }

        [Fact]
        public async Task Patch_RemoveSingleMember_Success()
        {
            var organizationId = _testOrganizationId1;
            var id = _testGroupId1;
            var model = new ScimPatchModel
            {
                Operations = new List<ScimPatchModel.OperationModel>()
                {
                    new ScimPatchModel.OperationModel { Op = "remove", Path = $"members[value eq {_testOrganizationUserId1}", Value = JsonDocument.Parse("{}").RootElement },
                },
                Schemas = new List<string>()
            };

            var context = await _factory.PatchAsync(organizationId, id, model);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Empty(databaseContext.GroupUsers);
        }

        [Fact]
        public async Task Patch_RemoveListMembers_Success()
        {
            var organizationId = _testOrganizationId1;
            var id = _testGroupId1;
            var model = new ScimPatchModel
            {
                Operations = new List<ScimPatchModel.OperationModel>()
                {
                    new ScimPatchModel.OperationModel { Op = "remove", Path = "members", Value = JsonDocument.Parse($"[{{\"value\":\"{_testOrganizationUserId1}\"}}]").RootElement },
                },
                Schemas = new List<string>()
            };

            var context = await _factory.PatchAsync(organizationId, id, model);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Empty(databaseContext.GroupUsers);
        }

        [Fact]
        public async Task Patch_NotFound()
        {
            var organizationId = _testOrganizationId1;
            var id = Guid.NewGuid();
            var model = new Models.ScimPatchModel
            {
                Operations = new List<ScimPatchModel.OperationModel>(),
                Schemas = new List<string>()
            };

            var context = await _factory.PatchAsync(organizationId, id, model);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

            var responseModel = JsonSerializer.Deserialize<ScimErrorResponseModel>(context.Response.Body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            Assert.Equal(404, responseModel.Status);
            Assert.Equal("Group not found.", responseModel.Detail);
            Assert.Equal(new List<string> { ScimConstants.Scim2SchemaError }, responseModel.Schemas);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Equal(3, databaseContext.Groups.Count());
            Assert.True(databaseContext.Groups.FirstOrDefault(g => g.Id == id) == null);
        }

        [Fact]
        public async Task Delete_Success()
        {
            var organizationId = _testOrganizationId1;
            var id = _testGroupId3;

            var context = await _factory.DeleteAsync(organizationId, id);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Equal(2, databaseContext.Groups.Count());
            Assert.True(databaseContext.Groups.FirstOrDefault(g => g.Id == id) == null);
        }

        [Fact]
        public async Task Delete_NotFound()
        {
            var organizationId = _testOrganizationId1;
            var id = Guid.NewGuid();

            var context = await _factory.DeleteAsync(organizationId, id);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

            var responseModel = JsonSerializer.Deserialize<ScimErrorResponseModel>(context.Response.Body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            Assert.Equal(404, responseModel.Status);
            Assert.Equal("Group not found.", responseModel.Detail);
            Assert.Equal(new List<string> { ScimConstants.Scim2SchemaError }, responseModel.Schemas);

            var databaseContext = _factory.GetDatabaseContext();
            Assert.Equal(3, databaseContext.Groups.Count());
            Assert.True(databaseContext.Groups.FirstOrDefault(g => g.Id == id) == null);
        }

        private void InitializeDbForTests(DatabaseContext databaseContext)
        {
            databaseContext.Organizations.AddRange(GetSeedingOrganizations());
            databaseContext.Groups.AddRange(GetSeedingGroups());
            databaseContext.Users.AddRange(GetSeedingUsers());
            databaseContext.OrganizationUsers.AddRange(GetSeedingOrganizationUsers());
            databaseContext.GroupUsers.AddRange(GetSeedingGroupUsers());
            databaseContext.SaveChanges();
        }

        private void ReinitializeDbForTests(DatabaseContext databaseContext)
        {
            databaseContext.Organizations.RemoveRange(databaseContext.Organizations);
            databaseContext.Groups.RemoveRange(databaseContext.Groups);
            databaseContext.Users.RemoveRange(databaseContext.Users);
            databaseContext.OrganizationUsers.RemoveRange(databaseContext.OrganizationUsers);
            databaseContext.GroupUsers.RemoveRange(databaseContext.GroupUsers);
            InitializeDbForTests(databaseContext);
        }

        private List<Infrastructure.EntityFramework.Models.User> GetSeedingUsers()
        {
            return new List<Infrastructure.EntityFramework.Models.User>()
            {
                new Infrastructure.EntityFramework.Models.User { Id = _testUserId1, Name = "Test User 1", ApiKey = "", Email = "", SecurityStamp = "" },
                new Infrastructure.EntityFramework.Models.User { Id = _testUserId2, Name = "Test User 2", ApiKey = "", Email = "", SecurityStamp = "" },
                new Infrastructure.EntityFramework.Models.User { Id = _testUserId3, Name = "Test User 3", ApiKey = "", Email = "", SecurityStamp = "" }
            };
        }

        private List<Infrastructure.EntityFramework.Models.Group> GetSeedingGroups()
        {
            return new List<Infrastructure.EntityFramework.Models.Group>()
            {
                new Infrastructure.EntityFramework.Models.Group { Id = _testGroupId1, OrganizationId = _testOrganizationId1, Name = "Test Group 1", ExternalId = "A" },
                new Infrastructure.EntityFramework.Models.Group { Id = _testGroupId2, OrganizationId = _testOrganizationId1, Name = "Test Group 2", ExternalId = "B" },
                new Infrastructure.EntityFramework.Models.Group { Id = _testGroupId3, OrganizationId = _testOrganizationId1, Name = "Test Group 3", ExternalId = "C" }
            };
        }

        private List<Infrastructure.EntityFramework.Models.Organization> GetSeedingOrganizations()
        {
            return new List<Infrastructure.EntityFramework.Models.Organization>()
            {
                new Infrastructure.EntityFramework.Models.Organization { Id = _testOrganizationId1, Name = "Test Organization 1", UseGroups = true }
            };
        }

        private List<Infrastructure.EntityFramework.Models.OrganizationUser> GetSeedingOrganizationUsers()
        {
            return new List<Infrastructure.EntityFramework.Models.OrganizationUser>()
            {
                new Infrastructure.EntityFramework.Models.OrganizationUser { Id = _testOrganizationUserId1, OrganizationId = _testOrganizationId1, UserId = _testUserId1, Status = Core.Enums.OrganizationUserStatusType.Confirmed },
                new Infrastructure.EntityFramework.Models.OrganizationUser { Id = _testOrganizationUserId2, OrganizationId = _testOrganizationId1, UserId = _testUserId2, Status = Core.Enums.OrganizationUserStatusType.Confirmed },
                new Infrastructure.EntityFramework.Models.OrganizationUser { Id = _testOrganizationUserId3, OrganizationId = _testOrganizationId1, UserId = _testUserId3, Status = Core.Enums.OrganizationUserStatusType.Confirmed }
            };
        }

        private List<Infrastructure.EntityFramework.Models.GroupUser> GetSeedingGroupUsers()
        {
            return new List<Infrastructure.EntityFramework.Models.GroupUser>()
            {
                new Infrastructure.EntityFramework.Models.GroupUser { GroupId = _testGroupId1, OrganizationUserId = _testOrganizationUserId1 }
            };
        }
    }
}
