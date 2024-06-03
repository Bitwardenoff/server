﻿using System.ComponentModel.DataAnnotations;
using Bit.Api.Models.Request.Organizations;

namespace Bit.Api.Auth.Models.Request.Accounts;

public class UpdateTdeOffboardingPasswordRequestModel : OrganizationUserResetPasswordRequestModel
{
    [StringLength(50)]
    public string MasterPasswordHint { get; set; }
}
