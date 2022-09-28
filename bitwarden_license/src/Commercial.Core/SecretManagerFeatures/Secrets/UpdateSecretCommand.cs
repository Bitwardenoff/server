﻿using Bit.Core.Entities;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Core.SecretManagerFeatures.Secrets.Interfaces;

namespace Bit.Commercial.Core.SecretManagerFeatures.Secrets;

public class UpdateSecretCommand : IUpdateSecretCommand
{
    private readonly ISecretRepository _secretRepository;

    public UpdateSecretCommand(ISecretRepository secretRepository)
    {
        _secretRepository = secretRepository;
    }

    public async Task<Secret> UpdateAsync(Secret secret)
    {
        if (secret.Id == default(Guid))
        {
            throw new BadRequestException("Cannot update secret, secret does not exist.");
        }

        var existingSecret = await _secretRepository.GetByIdAsync(secret.Id);
        if (existingSecret == null)
        {
            throw new NotFoundException();
        }

        secret.OrganizationId = existingSecret.OrganizationId;
        secret.CreationDate = existingSecret.CreationDate;
        secret.DeletedDate = existingSecret.DeletedDate;
        secret.RevisionDate = DateTime.UtcNow;

        await _secretRepository.ReplaceAsync(secret);
        return secret;
    }
}
