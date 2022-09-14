﻿using System.ComponentModel.DataAnnotations;
using Bit.Core.Entities;
using Bit.Core.Utilities;

namespace Bit.Api.SecretManagerFeatures.Models.Request
{
    public class ProjectCreateRequestModel
    {
        [Required]
        [EncryptedString]
        public string Name { get; set; }

        public Project ToProject(Guid organizationId)
        {
            return new Project()
            {
                OrganizationId = organizationId,
                Name = this.Name,
            };
        }
    }
}
