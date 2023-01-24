﻿using Bit.Commercial.Core.SecretManagerFeatures.AccessTokens;
using Bit.Core.Context;
using Bit.Core.Entities;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using Bit.Test.Common.Helpers;
using NSubstitute;
using Xunit;

namespace Bit.Commercial.Core.Test.SecretManagerFeatures.AccessTokens;

[SutProviderCustomize]
public class CreateServiceAccountCommandTests
{
    [Theory]
    [BitAutoData]
    public async Task CreateAsync_NoServiceAccountId_ThrowsBadRequestException(ApiKey data, Guid userId,
        SutProvider<CreateAccessTokenCommand> sutProvider)
    {
        data.ServiceAccountId = null;

        await Assert.ThrowsAsync<BadRequestException>(() => sutProvider.Sut.CreateAsync(data, userId));

        await sutProvider.GetDependency<IApiKeyRepository>().DidNotReceiveWithAnyArgs().CreateAsync(default);
    }

    [Theory]
    [BitAutoData]
    public async Task CreateAsync_User_NoAccess(ApiKey data, Guid userId, ServiceAccount saData,
        SutProvider<CreateAccessTokenCommand> sutProvider)
    {
        data.ServiceAccountId = saData.Id;

        sutProvider.GetDependency<IServiceAccountRepository>().GetByIdAsync(saData.Id).Returns(saData);
        sutProvider.GetDependency<IServiceAccountRepository>().UserHasWriteAccessToServiceAccount(saData.Id, userId).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sutProvider.Sut.CreateAsync(data, userId));

        await sutProvider.GetDependency<IApiKeyRepository>().DidNotReceiveWithAnyArgs().CreateAsync(default);
    }

    [Theory]
    [BitAutoData]
    public async Task CreateAsync_User_Success(ApiKey data, Guid userId, ServiceAccount saData,
        SutProvider<CreateAccessTokenCommand> sutProvider)
    {
        data.ServiceAccountId = saData.Id;
        sutProvider.GetDependency<IServiceAccountRepository>().GetByIdAsync(saData.Id).Returns(saData);
        sutProvider.GetDependency<IServiceAccountRepository>().UserHasWriteAccessToServiceAccount(saData.Id, userId).Returns(true);

        await sutProvider.Sut.CreateAsync(data, userId);

        await sutProvider.GetDependency<IApiKeyRepository>().Received(1)
            .CreateAsync(Arg.Is(AssertHelper.AssertPropertyEqual(data)));
    }

    [Theory]
    [BitAutoData]
    public async Task CreateAsync_Admin_Succeeds(ApiKey data, Guid userId, ServiceAccount saData,
        SutProvider<CreateAccessTokenCommand> sutProvider)
    {
        data.ServiceAccountId = saData.Id;

        sutProvider.GetDependency<IServiceAccountRepository>().GetByIdAsync(saData.Id).Returns(saData);
        sutProvider.GetDependency<ICurrentContext>().OrganizationAdmin(saData.OrganizationId).Returns(true);

        await sutProvider.Sut.CreateAsync(data, userId);

        await sutProvider.GetDependency<IApiKeyRepository>().Received(1)
            .CreateAsync(Arg.Is(AssertHelper.AssertPropertyEqual(data)));
    }
}
