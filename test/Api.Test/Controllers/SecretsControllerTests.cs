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
        public async void GetSecretsByOrganization_ReturnsEmptyList(SutProvider<SecretsController> sutProvider, Guid id)
        {
            var result = await sutProvider.Sut.GetSecretsByOrganizationAsync(id);

            await sutProvider.GetDependency<ISecretRepository>().Received(1)
                         .GetManyByOrganizationIdAsync(Arg.Is(AssertHelper.AssertPropertyEqual(id)));

            Assert.Empty(result.Data);
        }

        [Theory]
        [BitAutoData]
        public async void GetSecret_NotFound(SutProvider<SecretsController> sutProvider)
        {
            await Assert.ThrowsAsync<NotFoundException>(() => sutProvider.Sut.GetSecretAsync(Guid.NewGuid()));
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
        public async void CreateSecret_Success(SutProvider<SecretsController> sutProvider, SecretCreateRequestModel data, Guid organizationId)
        {
            var resultSecret = data.ToSecret(organizationId);

            sutProvider.GetDependency<ICreateSecretCommand>().CreateAsync(default).ReturnsForAnyArgs(resultSecret);

            var result = await sutProvider.Sut.CreateSecretAsync(organizationId, data);
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

        [Theory]
        [BitAutoData]
        public async void BulkDeleteSecret_Success(SutProvider<SecretsController> sutProvider, List<Guid> data)
        {
            var mockResult = new List<Tuple<Guid, string>>();
            foreach (var id in data)
            {
                mockResult.Add(new Tuple<Guid, string>(id, ""));
            }
            sutProvider.GetDependency<IDeleteSecretCommand>().DeleteSecrets(data).ReturnsForAnyArgs(mockResult);

            var results = await sutProvider.Sut.BulkDeleteAsync(data);
            await sutProvider.GetDependency<IDeleteSecretCommand>().Received(1)
                         .DeleteSecrets(Arg.Is(data));
            Assert.Equal(data.Count, results.Data.Count());
        }

        [Theory]
        [BitAutoData]
        public async void BulkDeleteSecret_NoGuids_ThrowsArgumentNullException(SutProvider<SecretsController> sutProvider)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => sutProvider.Sut.BulkDeleteAsync(new List<Guid>()));
        }
    }
}
