﻿namespace Bit.Infrastructure.EntityFramework.Repositories.Queries;

public class OrganizationUserReadManagedIdsByOrganizationIdQuery : IQuery<Guid>
{
    private readonly Guid _organizationId;

    public OrganizationUserReadManagedIdsByOrganizationIdQuery(Guid organizationId)
    {
        _organizationId = organizationId;
    }

    public IQueryable<Guid> Run(DatabaseContext dbContext)
    {
        var query = from ou in dbContext.OrganizationUsers
                    join u in dbContext.Users on ou.UserId equals u.Id
                    where ou.OrganizationId == _organizationId &&
                          dbContext.OrganizationDomains
                              .Any(od => od.OrganizationId == _organizationId &&
                                         od.VerifiedDate != null &&
                                         od.DomainName == u.Email.Substring(u.Email.IndexOf('@') + 1))
                    select ou.Id;

        return query;
    }
}
