﻿using Bit.Core.Entities;
using Bit.Core.Enums;

namespace Bit.Core.Repositories;

public interface IProjectRepository
{
    Task<IEnumerable<Project>> GetManyByOrganizationIdAsync(Guid organizationId, Guid userId, AccessClientType accessType);
    Task<IEnumerable<Project>> GetManyByIds(IEnumerable<Guid> ids);
    Task<Project> GetByIdAsync(Guid id);
    Task<Project> CreateAsync(Project project);
    Task ReplaceAsync(Project project);
    Task DeleteManyByIdAsync(IEnumerable<Guid> ids);
    Task<bool> UserHasWriteAccessToProject(Guid id, Guid userId);
    Task<bool> ServiceAccountHasAccessToProject(Guid id, Guid serviceAccountId);
}
