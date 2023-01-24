﻿using System.Net;
using System.Net.Http.Headers;
using Bit.Api.IntegrationTest.Factories;
using Bit.Api.IntegrationTest.Helpers;
using Bit.Api.Models.Response;
using Bit.Api.SecretsManager.Models.Request;
using Bit.Api.SecretsManager.Models.Response;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.SecretsManager.Entities;
using Bit.Core.SecretsManager.Repositories;
using Bit.Test.Common.Helpers;
using Xunit;

namespace Bit.Api.IntegrationTest.SecretsManager.Controllers;

public class ServiceAccountsControllerTest : IClassFixture<ApiApplicationFactory>, IAsyncLifetime
{
    private const string _mockEncryptedString =
        "2.3Uk+WNBIoU5xzmVFNcoWzz==|1MsPIYuRfdOHfu/0uY6H2Q==|/98sp4wb6pHP1VTZ9JcNCYgQjEUMFPlqJgCwRk1YXKg=";

    private const string _mockNewName =
        "2.3AZ+WNBIoU5xzmVFNcoWzz==|1MsPIYuRfdOHfu/0uY6H2Q==|/98xy4wb6pHP1VTZ9JcNCYgQjEUMFPlqJgCwRk1YXKg=";

    private readonly IAccessPolicyRepository _accessPolicyRepository;
    private readonly HttpClient _client;
    private readonly ApiApplicationFactory _factory;
    private readonly IServiceAccountRepository _serviceAccountRepository;
    private Organization _organization = null!;


    public ServiceAccountsControllerTest(ApiApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _serviceAccountRepository = _factory.GetService<IServiceAccountRepository>();
        _accessPolicyRepository = _factory.GetService<IAccessPolicyRepository>();
    }

    public async Task InitializeAsync()
    {
        var ownerEmail = $"integration-test{Guid.NewGuid()}@bitwarden.com";
        await _factory.LoginWithNewAccount(ownerEmail);
        (_organization, _) =
            await OrganizationTestHelpers.SignUpAsync(_factory, ownerEmail: ownerEmail, billingEmail: ownerEmail);
        var tokens = await _factory.LoginAsync(ownerEmail);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.Token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetServiceAccountsByOrganization_Admin()
    {
        var serviceAccountIds = await SetupGetServiceAccountsByOrganizationAsync();

        var response = await _client.GetAsync($"/organizations/{_organization.Id}/service-accounts");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListResponseModel<ServiceAccountResponseModel>>();

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Data);
        Assert.Equal(serviceAccountIds.Count, result.Data.Count());
    }

    [Fact]
    public async Task GetServiceAccountsByOrganization_User_Success()
    {
        // Create a new account as a user
        var user = await LoginAsNewOrgUserAsync();

        var serviceAccountIds = await SetupGetServiceAccountsByOrganizationAsync();

        var accessPolicies = serviceAccountIds.Select(
            id => new UserServiceAccountAccessPolicy
            {
                OrganizationUserId = user.Id,
                GrantedServiceAccountId = id,
                Read = true,
                Write = false,
            }).Cast<BaseAccessPolicy>().ToList();


        await _accessPolicyRepository.CreateManyAsync(accessPolicies);


        var response = await _client.GetAsync($"/organizations/{_organization.Id}/service-accounts");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListResponseModel<ServiceAccountResponseModel>>();

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Data);
        Assert.Equal(serviceAccountIds.Count, result.Data.Count());
    }

    [Fact]
    public async Task GetServiceAccountsByOrganization_User_NoPermission()
    {
        // Create a new account as a user
        await LoginAsNewOrgUserAsync();
        await SetupGetServiceAccountsByOrganizationAsync();

        var response = await _client.GetAsync($"/organizations/{_organization.Id}/service-accounts");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListResponseModel<ServiceAccountResponseModel>>();

        Assert.NotNull(result);
        Assert.Empty(result!.Data);
    }

    [Fact]
    public async Task CreateServiceAccount_Admin()
    {
        var request = new ServiceAccountCreateRequestModel { Name = _mockEncryptedString };

        var response = await _client.PostAsJsonAsync($"/organizations/{_organization.Id}/service-accounts", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ServiceAccountResponseModel>();

        Assert.NotNull(result);
        Assert.Equal(request.Name, result!.Name);
        AssertHelper.AssertRecent(result.RevisionDate);
        AssertHelper.AssertRecent(result.CreationDate);

        var createdServiceAccount = await _serviceAccountRepository.GetByIdAsync(new Guid(result.Id));
        Assert.NotNull(result);
        Assert.Equal(request.Name, createdServiceAccount.Name);
        AssertHelper.AssertRecent(createdServiceAccount.RevisionDate);
        AssertHelper.AssertRecent(createdServiceAccount.CreationDate);
    }

    [Fact]
    public async Task CreateServiceAccount_User_NoPermissions()
    {
        // Create a new account as a user
        await LoginAsNewOrgUserAsync();

        var request = new ServiceAccountCreateRequestModel { Name = _mockEncryptedString };

        var response = await _client.PostAsJsonAsync($"/organizations/{_organization.Id}/service-accounts", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateServiceAccount_Admin()
    {
        var initialServiceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = _organization.Id,
            Name = _mockEncryptedString,
        });

        var request = new ServiceAccountUpdateRequestModel { Name = _mockNewName };

        var response = await _client.PutAsJsonAsync($"/service-accounts/{initialServiceAccount.Id}", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ServiceAccountResponseModel>();
        Assert.NotNull(result);
        Assert.Equal(request.Name, result!.Name);
        Assert.NotEqual(initialServiceAccount.Name, result.Name);
        AssertHelper.AssertRecent(result.RevisionDate);
        Assert.NotEqual(initialServiceAccount.RevisionDate, result.RevisionDate);

        var updatedServiceAccount = await _serviceAccountRepository.GetByIdAsync(initialServiceAccount.Id);
        Assert.NotNull(result);
        Assert.Equal(request.Name, updatedServiceAccount.Name);
        AssertHelper.AssertRecent(updatedServiceAccount.RevisionDate);
        AssertHelper.AssertRecent(updatedServiceAccount.CreationDate);
        Assert.NotEqual(initialServiceAccount.Name, updatedServiceAccount.Name);
        Assert.NotEqual(initialServiceAccount.RevisionDate, updatedServiceAccount.RevisionDate);
    }

    [Fact]
    public async Task UpdateServiceAccount_User_WithPermission()
    {
        // Create a new account as a user
        var user = await LoginAsNewOrgUserAsync();

        var initialServiceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = _organization.Id,
            Name = _mockEncryptedString,
        });

        await CreateUserServiceAccountAccessPolicyAsync(user.Id, initialServiceAccount.Id, true, true);

        var request = new ServiceAccountUpdateRequestModel { Name = _mockNewName };

        var response = await _client.PutAsJsonAsync($"/service-accounts/{initialServiceAccount.Id}", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ServiceAccountResponseModel>();
        Assert.NotNull(result);
        Assert.Equal(request.Name, result!.Name);
        Assert.NotEqual(initialServiceAccount.Name, result.Name);
        AssertHelper.AssertRecent(result.RevisionDate);
        Assert.NotEqual(initialServiceAccount.RevisionDate, result.RevisionDate);

        var updatedServiceAccount = await _serviceAccountRepository.GetByIdAsync(initialServiceAccount.Id);
        Assert.NotNull(result);
        Assert.Equal(request.Name, updatedServiceAccount.Name);
        AssertHelper.AssertRecent(updatedServiceAccount.RevisionDate);
        AssertHelper.AssertRecent(updatedServiceAccount.CreationDate);
        Assert.NotEqual(initialServiceAccount.Name, updatedServiceAccount.Name);
        Assert.NotEqual(initialServiceAccount.RevisionDate, updatedServiceAccount.RevisionDate);
    }

    [Fact]
    public async Task UpdateServiceAccount_User_NoPermissions()
    {
        // Create a new account as a user
        await LoginAsNewOrgUserAsync();

        var initialServiceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = _organization.Id,
            Name = _mockEncryptedString,
        });

        var request = new ServiceAccountUpdateRequestModel { Name = _mockNewName };

        var response = await _client.PutAsJsonAsync($"/service-accounts/{initialServiceAccount.Id}", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateServiceAccountAccessToken_Admin()
    {
        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = _organization.Id,
            Name = _mockEncryptedString,
        });

        var mockExpiresAt = DateTime.UtcNow.AddDays(30);
        var request = new AccessTokenCreateRequestModel
        {
            Name = _mockEncryptedString,
            EncryptedPayload = _mockEncryptedString,
            Key = _mockEncryptedString,
            ExpireAt = mockExpiresAt,
        };

        var response = await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/access-tokens", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AccessTokenCreationResponseModel>();

        Assert.NotNull(result);
        Assert.Equal(request.Name, result!.Name);
        Assert.NotNull(result.ClientSecret);
        Assert.Equal(mockExpiresAt, result.ExpireAt);
        AssertHelper.AssertRecent(result.RevisionDate);
        AssertHelper.AssertRecent(result.CreationDate);
    }

    [Fact]
    public async Task CreateServiceAccountAccessToken_User_WithPermission()
    {
        // Create a new account as a user
        var user = await LoginAsNewOrgUserAsync();

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = _organization.Id,
            Name = _mockEncryptedString,
        });

        await CreateUserServiceAccountAccessPolicyAsync(user.Id, serviceAccount.Id, true, true);

        var mockExpiresAt = DateTime.UtcNow.AddDays(30);
        var request = new AccessTokenCreateRequestModel
        {
            Name = _mockEncryptedString,
            EncryptedPayload = _mockEncryptedString,
            Key = _mockEncryptedString,
            ExpireAt = mockExpiresAt,
        };

        var response = await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/access-tokens", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AccessTokenCreationResponseModel>();

        Assert.NotNull(result);
        Assert.Equal(request.Name, result!.Name);
        Assert.NotNull(result.ClientSecret);
        Assert.Equal(mockExpiresAt, result.ExpireAt);
        AssertHelper.AssertRecent(result.RevisionDate);
        AssertHelper.AssertRecent(result.CreationDate);
    }

    [Fact]
    public async Task CreateServiceAccountAccessToken_User_NoPermission()
    {
        // Create a new account as a user
        await LoginAsNewOrgUserAsync();

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = _organization.Id,
            Name = _mockEncryptedString,
        });

        var mockExpiresAt = DateTime.UtcNow.AddDays(30);
        var request = new AccessTokenCreateRequestModel
        {
            Name = _mockEncryptedString,
            EncryptedPayload = _mockEncryptedString,
            Key = _mockEncryptedString,
            ExpireAt = mockExpiresAt,
        };

        var response = await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/access-tokens", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateServiceAccountAccessTokenExpireAtNullAsync_Admin()
    {
        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = _organization.Id,
            Name = _mockEncryptedString,
        });

        var request = new AccessTokenCreateRequestModel
        {
            Name = _mockEncryptedString,
            EncryptedPayload = _mockEncryptedString,
            Key = _mockEncryptedString,
            ExpireAt = null,
        };

        var response = await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/access-tokens", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AccessTokenCreationResponseModel>();

        Assert.NotNull(result);
        Assert.Equal(request.Name, result!.Name);
        Assert.NotNull(result.ClientSecret);
        Assert.Null(result.ExpireAt);
        AssertHelper.AssertRecent(result.RevisionDate);
        AssertHelper.AssertRecent(result.CreationDate);
    }

    [Fact]
    public async Task CreateServiceAccountAccessTokenExpireAtNullAsync_User_WithPermission()
    {
        // Create a new account as a user
        var user = await LoginAsNewOrgUserAsync();

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = _organization.Id,
            Name = _mockEncryptedString,
        });

        await CreateUserServiceAccountAccessPolicyAsync(user.Id, serviceAccount.Id, true, true);

        var request = new AccessTokenCreateRequestModel
        {
            Name = _mockEncryptedString,
            EncryptedPayload = _mockEncryptedString,
            Key = _mockEncryptedString,
            ExpireAt = null,
        };

        var response = await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/access-tokens", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AccessTokenCreationResponseModel>();

        Assert.NotNull(result);
        Assert.Equal(request.Name, result!.Name);
        Assert.NotNull(result.ClientSecret);
        Assert.Null(result.ExpireAt);
        AssertHelper.AssertRecent(result.RevisionDate);
        AssertHelper.AssertRecent(result.CreationDate);
    }

    [Fact]
    public async Task CreateServiceAccountAccessTokenExpireAtNullAsync_User_NoPermission()
    {
        // Create a new account as a user
        await LoginAsNewOrgUserAsync();

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = _organization.Id,
            Name = _mockEncryptedString,
        });

        var request = new AccessTokenCreateRequestModel
        {
            Name = _mockEncryptedString,
            EncryptedPayload = _mockEncryptedString,
            Key = _mockEncryptedString,
            ExpireAt = null,
        };

        var response = await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/access-tokens", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<List<Guid>> SetupGetServiceAccountsByOrganizationAsync()
    {
        const int serviceAccountsToCreate = 3;
        var serviceAccountIds = new List<Guid>();
        for (var i = 0; i < serviceAccountsToCreate; i++)
        {
            var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
            {
                OrganizationId = _organization.Id,
                Name = _mockEncryptedString,
            });
            serviceAccountIds.Add(serviceAccount.Id);
        }

        return serviceAccountIds;
    }

    private async Task CreateUserServiceAccountAccessPolicyAsync(Guid userId, Guid serviceAccountId, bool read,
        bool write)
    {
        var accessPolicies = new List<BaseAccessPolicy>
        {
            new UserServiceAccountAccessPolicy
            {
                OrganizationUserId = userId,
                GrantedServiceAccountId = serviceAccountId,
                Read = read,
                Write = write,
            },
        };
        await _accessPolicyRepository.CreateManyAsync(accessPolicies);
    }

    private async Task<OrganizationUser> LoginAsNewOrgUserAsync(OrganizationUserType type = OrganizationUserType.User)
    {
        var email = $"integration-test{Guid.NewGuid()}@bitwarden.com";
        await _factory.LoginWithNewAccount(email);
        var orgUser = await OrganizationTestHelpers.CreateUserAsync(_factory, _organization.Id, email, type);
        var tokens = await _factory.LoginAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.Token);
        return orgUser;
    }
}
