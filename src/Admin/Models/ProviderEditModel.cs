﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Bit.Core.Enums.Provider;
using Bit.Core.Models.Data;
using Bit.Core.Models.Table.Provider;

namespace Bit.Admin.Models
{
    public class ProviderEditModel : ProviderViewModel
    {
        public ProviderEditModel() { }

        public ProviderEditModel(Provider provider, IEnumerable<ProviderUserUserDetails> providerUsers)
            : base(provider, providerUsers)
        {
            Name = provider.Name;
            BusinessName = provider.BusinessName;
            BillingEmail = provider.BillingEmail;
            Enabled = provider.Enabled;
        }

        public bool Enabled { get; set; }
        public string BillingEmail { get; set; }
        public string BusinessName { get; set; }
        public string Name { get; set; }
        [Display(Name = "Events")]
        
        public Provider ToProvider(Provider existingProvider)
        {
            existingProvider.Name = Name;
            existingProvider.BusinessName = BusinessName;
            existingProvider.BillingEmail = BillingEmail?.ToLowerInvariant()?.Trim();
            existingProvider.Enabled = Enabled;
            return existingProvider;
        }
    }
}
