﻿using System;
using System.Reflection;
using AutoFixture;
using TableModel = Bit.Core.Models.Table;
using Bit.Core.Enums;
using AutoFixture.Kernel;
using Bit.Core.Test.AutoFixture.OrganizationFixtures;
using Bit.Infrastructure.EntityFramework.Repositories;
using Bit.Core.Test.AutoFixture.EntityFrameworkRepositoryFixtures;
using AutoFixture.Xunit2;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;

namespace Bit.Core.Test.AutoFixture.PolicyFixtures
{
    internal class Policy : ICustomization
    {
        public PolicyType Type { get; set; }

        public Policy(PolicyType type)
        {
            Type = type;
        }
        
        public void Customize(IFixture fixture)
        {
            fixture.Customize<Core.Models.Table.Policy>(composer => composer
                .With(o => o.OrganizationId, Guid.NewGuid())
                .With(o => o.Type, Type)
                .With(o => o.Enabled, true));
        }
    }

    public class PolicyAttribute : CustomizeAttribute
    {
        private readonly PolicyType _type;

        public PolicyAttribute(PolicyType type)
        {
            _type = type;
        }

        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new Policy(_type);
        }
    }
    
    internal class PolicyBuilder: ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (context == null) 
            {
                throw new ArgumentNullException(nameof(context));
            }

            var type = request as Type;
            if (type == null || type != typeof(TableModel.Policy))
            {
                return new NoSpecimen();
            }

            var fixture = new Fixture();
            var obj = fixture.WithAutoNSubstitutions().Create<TableModel.Policy>();
            return obj;
        }
    }

    internal class EfPolicy: ICustomization 
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(new IgnoreVirtualMembersCustomization());
            fixture.Customizations.Add(new GlobalSettingsBuilder());
            fixture.Customizations.Add(new PolicyBuilder());
            fixture.Customizations.Add(new OrganizationBuilder());
            fixture.Customizations.Add(new EfRepositoryListBuilder<PolicyRepository>());
            fixture.Customizations.Add(new EfRepositoryListBuilder<OrganizationRepository>());
        }
    }

    internal class EfPolicyApplicableToUser : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(new IgnoreVirtualMembersCustomization());
            fixture.Customizations.Add(new GlobalSettingsBuilder());
            fixture.Customizations.Add(new PolicyBuilder());
            fixture.Customizations.Add(new OrganizationBuilder());
            fixture.Customizations.Add(new EfRepositoryListBuilder<PolicyRepository>());
            fixture.Customizations.Add(new EfRepositoryListBuilder<UserRepository>());
            fixture.Customizations.Add(new EfRepositoryListBuilder<OrganizationRepository>());
            fixture.Customizations.Add(new EfRepositoryListBuilder<OrganizationUserRepository>());
            fixture.Customizations.Add(new EfRepositoryListBuilder<ProviderRepository>());
            fixture.Customizations.Add(new EfRepositoryListBuilder<ProviderUserRepository>());
            fixture.Customizations.Add(new EfRepositoryListBuilder<ProviderOrganizationRepository>());
        }
    }

    internal class EfPolicyAutoDataAttribute : CustomAutoDataAttribute
    {
        public EfPolicyAutoDataAttribute() : base(new SutProviderCustomization(), new EfPolicy())
        { }
    }

    internal class EfPolicyApplicableToUserInlineAutoDataAttribute : InlineCustomAutoDataAttribute
    {
        public EfPolicyApplicableToUserInlineAutoDataAttribute(params object[] values) : base(new[] { typeof(SutProviderCustomization), typeof(EfPolicyApplicableToUser) }, values)
        { }
    }

    internal class InlineEfPolicyAutoDataAttribute : InlineCustomAutoDataAttribute
    {
        public InlineEfPolicyAutoDataAttribute(params object[] values) : base(new[] { typeof(SutProviderCustomization),
            typeof(EfPolicy) }, values)
        { }
    }
}
