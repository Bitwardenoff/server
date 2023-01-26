﻿using Bit.Api.SecretsManager.Controllers;
using Bit.Core.SecretsManager.Commands.Projects.Interfaces;
using Bit.Core.SecretsManager.Entities;
using Bit.Core.Services;
using Bit.Core.Test.SecretsManager.AutoFixture.ProjectsFixture;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using NSubstitute;
using Xunit;

namespace Bit.Api.Test.SecretsManager.Controllers;

[ControllerCustomize(typeof(ProjectsController))]
[SutProviderCustomize]
[ProjectCustomize]
[JsonDocumentCustomize]
public class ProjectsControllerTests
{
    [Theory]
    [BitAutoData]
    public async void BulkDeleteProjects_Success(SutProvider<ProjectsController> sutProvider, List<Project> data)
    {
        sutProvider.GetDependency<IUserService>().GetProperUserId(default).ReturnsForAnyArgs(Guid.NewGuid());
        var ids = data.Select(project => project.Id)?.ToList();
        var mockResult = new List<Tuple<Project, string>>();
        foreach (var project in data)
        {
            mockResult.Add(new Tuple<Project, string>(project, ""));
        }
        sutProvider.GetDependency<IDeleteProjectCommand>().DeleteProjects(ids, default).ReturnsForAnyArgs(mockResult);

        var results = await sutProvider.Sut.BulkDeleteProjectsAsync(ids);
        await sutProvider.GetDependency<IDeleteProjectCommand>().Received(1)
                     .DeleteProjects(Arg.Is(ids), Arg.Any<Guid>());
        Assert.Equal(data.Count, results.Data.Count());
    }

    [Theory]
    [BitAutoData]
    public async void BulkDeleteProjects_NoGuids_ThrowsArgumentNullException(SutProvider<ProjectsController> sutProvider)
    {
        sutProvider.GetDependency<IUserService>().GetProperUserId(default).ReturnsForAnyArgs(Guid.NewGuid());
        await Assert.ThrowsAsync<ArgumentNullException>(() => sutProvider.Sut.BulkDeleteProjectsAsync(new List<Guid>()));
    }
}
