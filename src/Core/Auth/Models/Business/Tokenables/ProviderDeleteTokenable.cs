﻿using Bit.Core.AdminConsole.Entities.Provider;
using Newtonsoft.Json;

namespace Bit.Core.Auth.Models.Business.Tokenables;

public class ProviderDeleteTokenable : Tokens.ExpiringTokenable
{
    public const string ClearTextPrefix = "";
    public const string DataProtectorPurpose = "OrgDeleteDataProtector";
    public const string TokenIdentifier = "OrgDelete";
    public string Identifier { get; set; } = TokenIdentifier;
    public Guid Id { get; set; }
    [JsonConstructor]
    public ProviderDeleteTokenable(DateTime expirationDate)
    {
        ExpirationDate = expirationDate;
    }

    public ProviderDeleteTokenable(Provider provider, int hoursTillExpiration)
    {
        Id = provider.Id;
        ExpirationDate = DateTime.UtcNow.AddHours(hoursTillExpiration);
    }

    public bool IsValid(Provider provider)
    {
        return Id == provider.Id;
    }

    protected override bool TokenIsValid() => Identifier == TokenIdentifier && Id != default;
}
