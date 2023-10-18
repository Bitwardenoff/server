﻿using AutoMapper;

namespace Bit.Infrastructure.EntityFramework.Models;

public class OrganizationUser : Core.AdminConsole.Entities.OrganizationUser
{
    public virtual Organization Organization { get; set; }
    public virtual User User { get; set; }
    public virtual ICollection<CollectionUser> CollectionUsers { get; set; }
    public virtual ICollection<GroupUser> GroupUsers { get; set; }
}

public class OrganizationUserMapperProfile : Profile
{
    public OrganizationUserMapperProfile()
    {
        CreateMap<Core.AdminConsole.Entities.OrganizationUser, OrganizationUser>().ReverseMap();
    }
}
