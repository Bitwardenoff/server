﻿using System.Text.Json;
using AutoFixture;
using AutoFixture.Kernel;
using Bit.Core.Entities;
using Bit.Core.Models.Data;
using Bit.Core.Test.AutoFixture.EntityFrameworkRepositoryFixtures;
using Bit.Core.Test.AutoFixture.OrganizationFixtures;
using Bit.Core.Test.AutoFixture.OrganizationUserFixtures;
using Bit.Core.Test.AutoFixture.Relays;
using Bit.Core.Test.AutoFixture.UserFixtures;
using Bit.Infrastructure.EntityFramework.Repositories;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using Core.Models.Data;

namespace Bit.Core.Test.AutoFixture.CipherFixtures
{
    internal class OrganizationCipher : ICustomization
    {
        public Guid? OrganizationId { get; set; }
        public void Customize(IFixture fixture)
        {
            fixture.Customize<Cipher>(composer => composer
                .With(c => c.OrganizationId, OrganizationId ?? Guid.NewGuid())
                .Without(c => c.UserId));
            fixture.Customize<CipherDetails>(composer => composer
                .With(c => c.OrganizationId, Guid.NewGuid())
                .Without(c => c.UserId));
        }
    }

    internal class UserCipher : ICustomization
    {
        public Guid? UserId { get; set; }
        public void Customize(IFixture fixture)
        {
            fixture.Customize<Cipher>(composer => composer
                .With(c => c.UserId, UserId ?? Guid.NewGuid())
                .Without(c => c.OrganizationId));
            fixture.Customize<CipherDetails>(composer => composer
                .With(c => c.UserId, Guid.NewGuid())
                .Without(c => c.OrganizationId));
        }
    }

    internal class CipherBuilder : ISpecimenBuilder
    {
        public bool OrganizationOwned { get; set; }
        public object Create(object request, ISpecimenContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var type = request as Type;
            if (type == null || (type != typeof(Cipher) && type != typeof(List<Cipher>)))
            {
                return new NoSpecimen();
            }

            var fixture = new Fixture();
            fixture.Customizations.Insert(0, new MaxLengthStringRelay());
            fixture.Customizations.Add(new IgnoreVirtualMembersCustomization());

            if (!OrganizationOwned)
            {
                fixture.Customize<Cipher>(composer => composer
                        .Without(c => c.OrganizationId));
            }

            // Can't test valid Favorites and Folders without creating those values inide each test, 
            // since we won't have any UserIds until the test is running & creating data
            fixture.Customize<Cipher>(c => c
                .Without(e => e.Favorites)
                .Without(e => e.Folders));
            //
            var serializerOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            if (type == typeof(Cipher))
            {
                var obj = fixture.WithAutoNSubstitutions().Create<Cipher>();
                var cipherData = fixture.WithAutoNSubstitutions().Create<CipherLoginData>();
                var cipherAttachements = fixture.WithAutoNSubstitutions().Create<List<CipherAttachment>>();
                obj.Data = JsonSerializer.Serialize(cipherData, serializerOptions);
                obj.Attachments = JsonSerializer.Serialize(cipherAttachements, serializerOptions);

                return obj;
            }
            if (type == typeof(List<Cipher>))
            {
                var ciphers = fixture.WithAutoNSubstitutions().CreateMany<Cipher>().ToArray();
                for (var i = 0; i < ciphers.Count(); i++)
                {
                    var cipherData = fixture.WithAutoNSubstitutions().Create<CipherLoginData>();
                    var cipherAttachements = fixture.WithAutoNSubstitutions().Create<List<CipherAttachment>>();
                    ciphers[i].Data = JsonSerializer.Serialize(cipherData, serializerOptions);
                    ciphers[i].Attachments = JsonSerializer.Serialize(cipherAttachements, serializerOptions);
                }

                return ciphers;
            }

            return new NoSpecimen();
        }
    }

    internal class EfCipher : ICustomization
    {
        public bool OrganizationOwned { get; set; }
        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(new GlobalSettingsBuilder());
            fixture.Customizations.Add(new CipherBuilder()
            {
                OrganizationOwned = OrganizationOwned
            });
            fixture.Customizations.Add(new UserBuilder());
            fixture.Customizations.Add(new OrganizationBuilder());
            fixture.Customizations.Add(new OrganizationUserBuilder());
            fixture.Customizations.Add(new EfRepositoryListBuilder<CipherRepository>());
            fixture.Customizations.Add(new EfRepositoryListBuilder<OrganizationRepository>());
            fixture.Customizations.Add(new EfRepositoryListBuilder<OrganizationUserRepository>());
            fixture.Customizations.Add(new EfRepositoryListBuilder<UserRepository>());
            fixture.Customizations.Add(new EfRepositoryListBuilder<CollectionRepository>());
        }
    }

    internal class UserCipherCustomizeAttribute : BitCustomizeAttribute
    {
        public override ICustomization GetCustomization() => new UserCipher();
    }

    internal class OrganizationCipherCustomizeAttribute : BitCustomizeAttribute
    {
        public override ICustomization GetCustomization() => new OrganizationCipher();
    }

    internal class EfUserCipherAutoDataAttribute : CustomAutoDataAttribute
    {
        public EfUserCipherAutoDataAttribute() : base(new SutProviderCustomization(), new EfCipher())
        { }
    }

    internal class EfOrganizationCipherAutoDataAttribute : CustomAutoDataAttribute
    {
        public EfOrganizationCipherAutoDataAttribute() : base(new SutProviderCustomization(), new EfCipher()
        {
            OrganizationOwned = true,
        })
        { }
    }
}
