﻿using AutoMapper;
using Bit.Core.Repositories;
using Bit.Infrastructure.EntityFramework.Models;
using Bit.Infrastructure.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Commercial.Infrastructure.EntityFramework.Repositories;

public class SecretRepository : Repository<Core.Entities.Secret, Secret, Guid>, ISecretRepository
{
    public SecretRepository(IServiceScopeFactory serviceScopeFactory, IMapper mapper)
        : base(serviceScopeFactory, mapper, db => db.Secret)
    { }

    public override async Task<Core.Entities.Secret> GetByIdAsync(Guid id)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var secret = await dbContext.Secret
                                    .Where(c => c.Id == id && c.DeletedDate == null)
                                    .FirstOrDefaultAsync();
            return Mapper.Map<Core.Entities.Secret>(secret);
        }
    }

    public async Task<IEnumerable<Core.Entities.Secret>> GetManyByIds(IEnumerable<Guid> ids)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var secrets = await dbContext.Secret
                                    .Where(c => ids.Contains(c.Id) && c.DeletedDate == null)
                                    .ToListAsync();
            return Mapper.Map<List<Core.Entities.Secret>>(secrets);
        }
    }

    public async Task<IEnumerable<Core.Entities.Secret>> GetManyByOrganizationIdAsync(Guid organizationId)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var secrets = await dbContext.Secret
                                    .Where(c => c.OrganizationId == organizationId && c.DeletedDate == null)
                                    .Include("Projects")
                                    .OrderBy(c => c.RevisionDate)
                                    .ToListAsync();

            return Mapper.Map<List<Core.Entities.Secret>>(secrets);
        }
    }

    public async Task<IEnumerable<Core.Entities.Secret>> GetManyByProjectIdAsync(Guid projectId)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var secrets = await dbContext.Secret
                .Where(s => s.Projects.Any(p => p.Id == projectId) && s.DeletedDate == null).Include("Projects")
                .OrderBy(s => s.RevisionDate).ToListAsync();

            return Mapper.Map<List<Core.Entities.Secret>>(secrets);
        }
    }

    public override async Task<Core.Entities.Secret> CreateAsync(Core.Entities.Secret secret)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            secret.SetNewId();
            var entity = Mapper.Map<Secret>(secret);

            if (secret.Projects?.Count > 0)
            {
                foreach (var p in entity.Projects)
                {
                    dbContext.Attach(p);
                }
            }

            await dbContext.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            secret.Id = entity.Id;
            return secret;
        }
    }

    public async Task SoftDeleteManyByIdAsync(IEnumerable<Guid> ids)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var utcNow = DateTime.UtcNow;
            var secrets = dbContext.Secret.Where(c => ids.Contains(c.Id));
            await secrets.ForEachAsync(secret =>
            {
                dbContext.Attach(secret);
                secret.DeletedDate = utcNow;
                secret.RevisionDate = utcNow;
            });
            await dbContext.SaveChangesAsync();
        }
    }
}
