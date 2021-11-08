﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bit.Core.Models.Api
{
    public class SecretVerificationRequestModel : IValidatableObject
    {
        [StringLength(300)]
        public string MasterPasswordHash { get; set; }
        public string OTP { get; set; }

        public string Secret => SuppliedMasterPassword() ? MasterPasswordHash : OTP;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(MasterPasswordHash) && string.IsNullOrEmpty(OTP))
            {
                yield return new ValidationResult("MasterPasswordHash or OTP must be supplied.");
            }
        }

        private bool SuppliedMasterPassword()
        {
            return !string.IsNullOrEmpty(MasterPasswordHash);
        }
    }
}
