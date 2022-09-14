﻿using System.ComponentModel.DataAnnotations;
using Bit.Core.Entities;
using Bit.Core.Utilities;

namespace Bit.Api.SecretManagerFeatures.Models.Request
{
    public class ProjectUpdateRequestModel
    {
        [Required]
        [EncryptedString]
        public string Name { get; set; }

        public Project ToProject(Guid id)
        {
            return new Project()
            {
                Id = id,
                Name = this.Name,
            };
        }
    }
}

