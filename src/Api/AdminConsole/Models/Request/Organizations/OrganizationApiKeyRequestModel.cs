﻿using Bit.Api.Auth.Models.Request.Accounts;
using Bit.Core.AdminConsole.Enums;
using Bit.Core.Enums;

namespace Bit.Api.AdminConsole.Models.Request.Organizations;

public class OrganizationApiKeyRequestModel : SecretVerificationRequestModel
{
    public OrganizationApiKeyType Type { get; set; }
}
