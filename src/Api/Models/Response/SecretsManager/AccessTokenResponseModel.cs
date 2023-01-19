﻿using Bit.Core.Entities;
using Bit.Core.Models.Api;

namespace Bit.Api.Models.Response.SecretsManager;

public class AccessTokenResponseModel : ResponseModel
{
    public AccessTokenResponseModel(ApiKey apiKey, string obj = "accessToken")
        : base(obj)
    {
        Id = apiKey.Id;
        Name = apiKey.Name;
        Scopes = apiKey.GetScopes();

        ExpireAt = apiKey.ExpireAt;
        CreationDate = apiKey.CreationDate;
        RevisionDate = apiKey.RevisionDate;
    }

    public Guid Id { get; }
    public string Name { get; }
    public ICollection<string> Scopes { get; }

    public DateTime? ExpireAt { get; }
    public DateTime CreationDate { get; }
    public DateTime RevisionDate { get; }
}
