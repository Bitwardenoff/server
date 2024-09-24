﻿#nullable enable

using AutoFixture;
using Bit.Admin.Models;
using Bit.Core.Entities;

namespace Admin.Test.Models;

public class UserModelTests
{
    [Fact]
    public void IsTwoFactorEnabled_GivenUserAndIsInLookup_WhenUserHasTwoFactorEnabled_ThenReturnsTrue()
    {
        var fixture = new Fixture();
        var user = fixture.Create<User>();
        var lookup = new List<(Guid, bool)>
        {
            (user.Id, true)
        };

        var actual = UserModel.IsTwoFactorEnabled(user, lookup);

        Assert.True(actual);
    }

    [Fact]
    public void IsTwoFactorEnabled_GivenUserAndIsInLookup_WhenUserDoesNotHaveTwoFactorEnabled_ThenReturnsFalse()
    {
        var fixture = new Fixture();
        var user = fixture.Create<User>();
        var lookup = new List<(Guid, bool)>
        {
            (Guid.NewGuid(), true)
        };

        var actual = UserModel.IsTwoFactorEnabled(user, lookup);

        Assert.False(actual);
    }

    [Fact]
    public void IsTwoFactorEnabled_GivenUserAndIsNotInLookup_WhenUserDoesNotHaveTwoFactorEnabled_ThenReturnsFalse()
    {
        var fixture = new Fixture();
        var user = fixture.Create<User>();
        var lookup = new List<(Guid, bool)>();

        var actual = UserModel.IsTwoFactorEnabled(user, lookup);

        Assert.False(actual);
    }

    [Fact]
    public void MapUserModel_GivenUser_WhenPopulated_ThenMapsToUserModel()
    {
        var fixture = new Fixture();
        var user = fixture.Create<User>();

        var actual = UserModel.MapUserModel(user, true);

        Assert.Equal(actual.Id, user.Id);
        Assert.Equal(actual.Email, user.Email);
        Assert.Equal(actual.CreationDate, user.CreationDate);
        Assert.Equal(actual.PremiumExpirationDate, user.PremiumExpirationDate);
        Assert.Equal(actual.Premium, user.Premium);
        Assert.Equal(actual.MaxStorageGb, user.MaxStorageGb);
        Assert.Equal(actual.EmailVerified, user.EmailVerified);
        Assert.True(actual.TwoFactorEnabled);
        Assert.Equal(actual.AccountRevisionDate, user.AccountRevisionDate);
        Assert.Equal(actual.RevisionDate, user.RevisionDate);
        Assert.Equal(actual.LastEmailChangeDate, user.LastEmailChangeDate);
        Assert.Equal(actual.LastKdfChangeDate, user.LastKdfChangeDate);
        Assert.Equal(actual.LastKeyRotationDate, user.LastKeyRotationDate);
        Assert.Equal(actual.LastPasswordChangeDate, user.LastPasswordChangeDate);
        Assert.Equal(actual.Gateway, user.Gateway);
        Assert.Equal(actual.GatewayCustomerId, user.GatewayCustomerId);
        Assert.Equal(actual.GatewaySubscriptionId, user.GatewaySubscriptionId);
        Assert.Equal(actual.LicenseKey, user.LicenseKey);
    }

    [Fact]
    public void MapUserModel_GivenUserWithTwoFactorEnabled_WhenPopulated_ThenMapsToUserModel()
    {
        var fixture = new Fixture();
        var user = fixture.Create<User>();
        var lookup = new List<(Guid, bool)> { (user.Id, true) };

        var actual = UserModel.MapUserModel(user, lookup);

        Assert.True(actual.TwoFactorEnabled);
    }

    [Fact]
    public void MapUserModel_GivenUserWithoutTwoFactorEnabled_WhenPopulated_ThenTwoFactorIsEnabled()
    {
        var fixture = new Fixture();
        var user = fixture.Create<User>();
        var lookup = new List<(Guid, bool)> { (user.Id, false) };

        var actual = UserModel.MapUserModel(user, lookup);

        Assert.False(actual.TwoFactorEnabled);
    }

    [Fact]
    public void MapUserModel_GivenUser_WhenNotInLookUpList_ThenTwoFactorIsDisabled()
    {
        var fixture = new Fixture();
        var user = fixture.Create<User>();
        var lookup = new List<(Guid, bool)> { (Guid.NewGuid(), true) };

        var actual = UserModel.MapUserModel(user, lookup);

        Assert.False(actual.TwoFactorEnabled);
    }
}
