﻿using System.Collections.Generic;
using Bit.Core.Repositories.EntityFramework;
using Bit.Core.Settings;
using Bit.Test.Common.Helpers.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Bit.Core.Test.Helpers.Factories
{
    public static class DatabaseOptionsFactory
    {
        public static List<DbContextOptions<DatabaseContext>> Options { get; } = new List<DbContextOptions<DatabaseContext>>();

        static DatabaseOptionsFactory()
        {
            var globalSettings = GlobalSettingsFactory.GlobalSettings;
            if (!string.IsNullOrWhiteSpace(GlobalSettingsFactory.GlobalSettings.PostgreSql?.ConnectionString))
            {
                Options.Add(new DbContextOptionsBuilder<DatabaseContext>().UseNpgsql(globalSettings.PostgreSql.ConnectionString).Options);
            }
            if (!string.IsNullOrWhiteSpace(GlobalSettingsFactory.GlobalSettings.MySql?.ConnectionString))
            {
                var mySqlConnectionString = globalSettings.MySql.ConnectionString;
                Options.Add(new DbContextOptionsBuilder<DatabaseContext>().UseMySql(mySqlConnectionString, ServerVersion.AutoDetect(mySqlConnectionString)).Options);
            }
        }
    }
}
