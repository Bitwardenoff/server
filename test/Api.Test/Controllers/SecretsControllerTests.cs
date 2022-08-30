﻿using Bit.Api.Controllers;
using Bit.Api.SecretManagerFeatures.Models.Request;
using Bit.Core.Entities;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Core.SecretManagerFeatures.Secrets.Interfaces;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using Bit.Test.Common.Helpers;
using NSubstitute;
using Xunit;

namespace Bit.Api.Test.Controllers
{
    [ControllerCustomize(typeof(SecretsController))]
    [SutProviderCustomize]
    [JsonDocumentCustomize]
    public class SecretsControllerTests
    {
        [Theory]
        [BitAutoData]
        public async void GetSecretsByOrganization_ThrowsNotFound(SutProvider<SecretsController> sutProvider)
        {
            var exception = await Assert.ThrowsAsync<NotFoundException>(() => sutProvider.Sut.GetSecretsByOrganizationAsync(Guid.NewGuid()));
        }

        [Theory]
        [BitAutoData]
        public async void GetSecret_NotFound(SutProvider<SecretsController> sutProvider)
        {
            await Assert.ThrowsAsync<NotFoundException>(() => sutProvider.Sut.GetSecretAsync(Guid.NewGuid()));
        }

        [Theory]
        [BitAutoData]
        public async void CreateSecret_MismatchedOrgId_Throws(SutProvider<SecretsController> sutProvider, SecretCreateRequestModel data)
        {
            var exception = await Assert.ThrowsAsync<BadRequestException>(() => sutProvider.Sut.CreateSecretAsync(Guid.NewGuid(), data));
            Assert.Contains("Organization ID does not match.", exception.Message);
        }

        [Theory]
        [BitAutoData]
        public async void GetSecret_Success(SutProvider<SecretsController> sutProvider, Secret resultSecret)
        {
            sutProvider.GetDependency<ISecretRepository>().GetByIdAsync(default).ReturnsForAnyArgs(resultSecret);

            var result = await sutProvider.Sut.GetSecretAsync(resultSecret.Id);

            await sutProvider.GetDependency<ISecretRepository>().Received(1)
                         .GetByIdAsync(Arg.Is(AssertHelper.AssertPropertyEqual(resultSecret.Id)));
        }

        [Theory]
        [BitAutoData]
        public async void GetSecretsByOrganization_Success(SutProvider<SecretsController> sutProvider, Secret resultSecret)
        {
            sutProvider.GetDependency<ISecretRepository>().GetManyByOrganizationIdAsync(default).ReturnsForAnyArgs(new List<Secret>() { resultSecret });

            var result = await sutProvider.Sut.GetSecretsByOrganizationAsync(resultSecret.OrganizationId);

            await sutProvider.GetDependency<ISecretRepository>().Received(1)
                         .GetManyByOrganizationIdAsync(Arg.Is(AssertHelper.AssertPropertyEqual(resultSecret.OrganizationId)));
        }

        [Theory]
        [BitAutoData]
        public async void CreateSecret_Success(SutProvider<SecretsController> sutProvider, SecretCreateRequestModel data)
        {
            var resultSecret = data.ToSecret();

            sutProvider.GetDependency<ICreateSecretCommand>().CreateAsync(default).ReturnsForAnyArgs(resultSecret);

            var result = await sutProvider.Sut.CreateSecretAsync(data.OrganizationId, data);
            await sutProvider.GetDependency<ICreateSecretCommand>().Received(1)
                         .CreateAsync(Arg.Any<Secret>());
        }

        [Theory]
        [BitAutoData]
        public async void UpdateSecret_Success(SutProvider<SecretsController> sutProvider, SecretUpdateRequestModel data, Guid secretId)
        {
            var resultSecret = data.ToSecret(secretId);
            sutProvider.GetDependency<IUpdateSecretCommand>().UpdateAsync(default).ReturnsForAnyArgs(resultSecret);

            var result = await sutProvider.Sut.UpdateSecretAsync(secretId, data);
            await sutProvider.GetDependency<IUpdateSecretCommand>().Received(1)
                         .UpdateAsync(Arg.Any<Secret>());
        }
    }
}
