﻿using Bit.Commercial.Core.SecretManagerFeatures.ServiceAccounts;
using Bit.Core.Entities;
using Bit.Core.Repositories;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using Bit.Test.Common.Helpers;
using NSubstitute;
using Xunit;

namespace Bit.Commercial.Core.Test.SecretManagerFeatures.Secrets;

[SutProviderCustomize]
public class CreateServiceAccountCommandTests
{
    [Theory]
    [BitAutoData]
    public async Task CreateAsync_CallsCreate(ServiceAccount data,
      SutProvider<CreateServiceAccountCommand> sutProvider)
    {
        await sutProvider.Sut.CreateAsync(data);

        await sutProvider.GetDependency<IServiceAccountRepository>().Received(1)
            .CreateAsync(Arg.Is(AssertHelper.AssertPropertyEqual(data)));
    }
}
