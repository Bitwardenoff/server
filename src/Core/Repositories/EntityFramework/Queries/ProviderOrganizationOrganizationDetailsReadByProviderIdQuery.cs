using System;
using System.Linq;
using Bit.Core.Models.Data;

namespace Bit.Core.Repositories.EntityFramework.Queries
{
    public class ProviderOrganizationOrganizationDetailsReadByProviderIdQuery : IQuery<ProviderOrganizationOrganizationDetails>
    {
        private readonly Guid _providerId;
        public ProviderOrganizationOrganizationDetailsReadByProviderIdQuery(Guid providerId)
        {
            _providerId = providerId;
        }

        public IQueryable<ProviderOrganizationOrganizationDetails> Run(DatabaseContext dbContext)
        {
            var query = from po in dbContext.ProviderOrganizations
                join o in dbContext.Organizations
                    on po.OrganizationId equals o.Id
                where po.ProviderId == _providerId
                select new { po, o };
            return query.Select(x => new ProviderOrganizationOrganizationDetails() 
            {
                Id = x.po.Id,
                ProviderId = x.po.ProviderId,
                OrganizationId  = x.po.OrganizationId,
                OrganizationName = x.o.Name,
                Key = x.po.Key,
                Settings = x.po.Settings,
                CreationDate = x.po.CreationDate,
                RevisionDate = x.po.RevisionDate,
            });
        }
    }
}
