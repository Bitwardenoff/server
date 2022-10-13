﻿using Bit.Scim.Groups;
using Bit.Scim.Groups.Interfaces;
using Bit.Scim.Users;
using Bit.Scim.Users.Interfaces;

namespace Bit.Scim.Utilities;

public static class ScimServiceCollectionExtensions
{
    public static void AddScimGroupQueries(this IServiceCollection services)
    {
        services.AddScoped<IGetGroupsListQuery, GetGroupsListQuery>();
    }

    public static void AddScimUserQueries(this IServiceCollection services)
    {
        services.AddScoped<IGetUserQuery, GetUserQuery>();
    }
}
