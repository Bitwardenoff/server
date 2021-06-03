﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Core.Enums.Provider;
using Bit.Core.Models.Table.Provider;

namespace Bit.Core.Repositories
{
    public interface IProviderUserRepository : IRepository<ProviderUser, Guid>
    {
        Task<int> GetCountByProviderAsync(Guid providerId, string email, bool onlyRegisteredUsers);
        Task<ICollection<ProviderUser>> GetManyAsync(IEnumerable<Guid> ids);
        Task<ICollection<ProviderUser>> GetManyByProviderAsync(Guid providerId, ProviderUserType? type = null);
        Task DeleteManyAsync(IEnumerable<Guid> userIds);
    }
}
