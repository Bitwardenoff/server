﻿using AutoMapper;

namespace Bit.Infrastructure.EntityFramework.Models;

public class BaseAccessPolicy : Core.Entities.BaseAccessPolicy
{
    public string Discriminator { get; set; }
}

public class AccessPolicyMapperProfile : Profile
{
    public AccessPolicyMapperProfile()
    {
        CreateMap<Core.Entities.AccessPolicy, AccessPolicy>().ReverseMap();
    }
}

public class AccessPolicy : BaseAccessPolicy
{
}

public class UserProjectAccessPolicy : AccessPolicy
{
    public Guid? OrganizationUserId { get; set; }
    public virtual OrganizationUser OrganizationUser { get; set; }
    public Guid? GrantedProjectId { get; set; }
    public virtual Project GrantedProject { get; set; }
}

public class UserServiceAccountAccessPolicy : AccessPolicy
{
    public Guid? OrganizationUserId { get; set; }
    public virtual OrganizationUser OrganizationUser { get; set; }
    public Guid? GrantedServiceAccountId { get; set; }
    public virtual ServiceAccount GrantedServiceAccount { get; set; }
}

public class GroupProjectAccessPolicy : AccessPolicy
{
    public Guid? GroupId { get; set; }
    public virtual Group Group { get; set; }
    public Guid? GrantedProjectId { get; set; }
    public virtual Project GrantedProject { get; set; }
}

public class GroupServiceAccountAccessPolicy : AccessPolicy
{
    public Guid? GroupId { get; set; }
    public virtual Group Group { get; set; }
    public Guid? GrantedServiceAccountId { get; set; }
    public virtual ServiceAccount GrantedServiceAccount { get; set; }
}

public class ServiceAccountProjectAccessPolicy : AccessPolicy
{
    public Guid? ServiceAccountId { get; set; }
    public virtual ServiceAccount ServiceAccount { get; set; }
    public Guid? GrantedProjectId { get; set; }
    public virtual Project GrantedProject { get; set; }
}
