﻿using Bit.Core.Entities;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Scim.Commands.Groups.Interfaces;
using Bit.Scim.Context;
using Bit.Scim.Models;

namespace Bit.Scim.Commands.Groups;

public class PutGroupCommand : IPutGroupCommand
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupService _groupService;
    private readonly IScimContext _scimContext;

    public PutGroupCommand(
        IGroupRepository groupRepository,
        IGroupService groupService,
        IScimContext scimContext)
    {
        _groupRepository = groupRepository;
        _groupService = groupService;
        _scimContext = scimContext;
    }

    public async Task<ScimGroupResponseModel> PutGroupAsync(Guid organizationId, Guid id, ScimGroupRequestModel model)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null || group.OrganizationId != organizationId)
        {
            throw new NotFoundException("Group not found.");
        }

        group.Name = model.DisplayName;
        await _groupService.SaveAsync(group);
        await UpdateGroupMembersAsync(group, model, false);

        return new ScimGroupResponseModel(group);
    }

    private async Task UpdateGroupMembersAsync(Group group, ScimGroupRequestModel model, bool skipIfEmpty)
    {
        if (_scimContext.RequestScimProvider != Core.Enums.ScimProviderType.Okta)
        {
            return;
        }

        if (model.Members == null)
        {
            return;
        }

        var memberIds = new List<Guid>();
        foreach (var id in model.Members.Select(i => i.Value))
        {
            if (Guid.TryParse(id, out var guidId))
            {
                memberIds.Add(guidId);
            }
        }

        if (!memberIds.Any() && skipIfEmpty)
        {
            return;
        }

        await _groupRepository.UpdateUsersAsync(group.Id, memberIds);
    }
}
