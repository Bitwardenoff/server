﻿using Bit.Commercial.Core.SecretsManager.Commands.AccessPolicies;
using Bit.Core.Exceptions;
using Bit.Core.SecretsManager.Entities;
using Bit.Core.SecretsManager.Repositories;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace Bit.Commercial.Core.Test.SecretsManager.AccessPolicies;

[SutProviderCustomize]
public class DeleteAccessPolicyCommandTests
{
    [Theory]
    [BitAutoData]
    public async Task DeleteAccessPolicy_Throws_NotFoundException(Guid data,
        SutProvider<DeleteAccessPolicyCommand> sutProvider)
    {
        sutProvider.GetDependency<IAccessPolicyRepository>().GetByIdAsync(data).ReturnsNull();
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => sutProvider.Sut.DeleteAsync(data));
        await sutProvider.GetDependency<IAccessPolicyRepository>().DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [Theory]
    [BitAutoData]
    public async Task DeleteAccessPolicy_Success(Guid data,
        SutProvider<DeleteAccessPolicyCommand> sutProvider)
    {
        sutProvider.GetDependency<IAccessPolicyRepository>().GetByIdAsync(data)
            .Returns(new UserProjectAccessPolicy { Id = data });

        await sutProvider.Sut.DeleteAsync(data);

        await sutProvider.GetDependency<IAccessPolicyRepository>().Received(1).DeleteAsync(Arg.Is(data));
    }
}
