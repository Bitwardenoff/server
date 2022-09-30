﻿using Bit.Scim.Commands.Groups;
using Bit.Scim.Commands.Groups.Interfaces;

namespace Bit.Scim.Utilities;

public static class ScimServiceCollectionExtensions
{
    public static void AddScimCommands(this IServiceCollection services)
    {
        services.AddScoped<IPutGroupCommand, PutGroupCommand>();
    }
}
