﻿using Bit.Core.AdminConsole.Entities;
using Bit.Core.Entities;
using Stripe;

namespace Bit.Core.Models.Business;

public abstract class SubscriptionUpdate
{
    protected abstract List<string> PlanIds { get; }

    public abstract List<SubscriptionItemOptions> RevertItemsOptions(Subscription subscription);
    public abstract List<SubscriptionItemOptions> UpgradeItemsOptions(Subscription subscription);

    public bool UpdateNeeded(Subscription subscription)
    {
        var upgradeItemsOptions = UpgradeItemsOptions(subscription);
        foreach (var upgradeItemOptions in upgradeItemsOptions)
        {
            var upgradeQuantity = upgradeItemOptions.Quantity ?? 0;
            var existingQuantity = SubscriptionItem(subscription, upgradeItemOptions.Plan)?.Quantity ?? 0;
            if (upgradeQuantity != existingQuantity)
            {
                return true;
            }
        }
        return false;
    }

    protected static SubscriptionItem SubscriptionItem(Subscription subscription, string planId) =>
        planId == null ? null : subscription.Items?.Data?.FirstOrDefault(i => i.Plan.Id == planId);
}

public abstract class BaseSeatSubscriptionUpdate : SubscriptionUpdate
{
    private readonly int _previousSeats;
    protected readonly StaticStore.Plan Plan;
    private readonly long? _additionalSeats;

    protected BaseSeatSubscriptionUpdate(Organization organization, StaticStore.Plan plan, long? additionalSeats, int previousSeats)
    {
        Plan = plan;
        _additionalSeats = additionalSeats;
        _previousSeats = previousSeats;
    }

    protected abstract string GetPlanId();

    protected override List<string> PlanIds => new() { GetPlanId() };

    public override List<SubscriptionItemOptions> UpgradeItemsOptions(Subscription subscription)
    {
        var item = SubscriptionItem(subscription, PlanIds.Single());
        return new()
        {
            new SubscriptionItemOptions
            {
                Id = item?.Id,
                Plan = PlanIds.Single(),
                Quantity = _additionalSeats,
                Deleted = (item?.Id != null && _additionalSeats == 0) ? true : (bool?)null,
            }
        };
    }

    public override List<SubscriptionItemOptions> RevertItemsOptions(Subscription subscription)
    {

        var item = SubscriptionItem(subscription, PlanIds.Single());
        return new()
        {
            new SubscriptionItemOptions
            {
                Id = item?.Id,
                Plan = PlanIds.Single(),
                Quantity = _previousSeats,
                Deleted = _previousSeats == 0 ? true : (bool?)null,
            }
        };
    }
}

public class SeatSubscriptionUpdate : BaseSeatSubscriptionUpdate
{
    public SeatSubscriptionUpdate(Organization organization, StaticStore.Plan plan, long? additionalSeats)
        : base(organization, plan, additionalSeats, organization.Seats.GetValueOrDefault())
    { }

    protected override string GetPlanId() => Plan.PasswordManager.StripeSeatPlanId;
}

public class SmSeatSubscriptionUpdate : BaseSeatSubscriptionUpdate
{
    public SmSeatSubscriptionUpdate(Organization organization, StaticStore.Plan plan, long? additionalSeats)
        : base(organization, plan, additionalSeats, organization.SmSeats.GetValueOrDefault())
    { }

    protected override string GetPlanId() => Plan.SecretsManager.StripeSeatPlanId;
}

public class ServiceAccountSubscriptionUpdate : SubscriptionUpdate
{
    private long? _prevServiceAccounts;
    private readonly StaticStore.Plan _plan;
    private readonly long? _additionalServiceAccounts;
    protected override List<string> PlanIds => new() { _plan.SecretsManager.StripeServiceAccountPlanId };

    public ServiceAccountSubscriptionUpdate(Organization organization, StaticStore.Plan plan, long? additionalServiceAccounts)
    {
        _plan = plan;
        _additionalServiceAccounts = additionalServiceAccounts;
        _prevServiceAccounts = organization.SmServiceAccounts ?? 0;
    }

    public override List<SubscriptionItemOptions> UpgradeItemsOptions(Subscription subscription)
    {
        var item = SubscriptionItem(subscription, PlanIds.Single());
        _prevServiceAccounts = item?.Quantity ?? 0;
        return new()
        {
            new SubscriptionItemOptions
            {
                Id = item?.Id,
                Plan = PlanIds.Single(),
                Quantity = _additionalServiceAccounts,
                Deleted = (item?.Id != null && _additionalServiceAccounts == 0) ? true : (bool?)null,
            }
        };
    }

    public override List<SubscriptionItemOptions> RevertItemsOptions(Subscription subscription)
    {
        var item = SubscriptionItem(subscription, PlanIds.Single());
        return new()
        {
            new SubscriptionItemOptions
            {
                Id = item?.Id,
                Plan = PlanIds.Single(),
                Quantity = _prevServiceAccounts,
                Deleted = _prevServiceAccounts == 0 ? true : (bool?)null,
            }
        };
    }
}

public class StorageSubscriptionUpdate : SubscriptionUpdate
{
    private long? _prevStorage;
    private readonly string _plan;
    private readonly long? _additionalStorage;
    protected override List<string> PlanIds => new() { _plan };

    public StorageSubscriptionUpdate(string plan, long? additionalStorage)
    {
        _plan = plan;
        _additionalStorage = additionalStorage;
    }

    public override List<SubscriptionItemOptions> UpgradeItemsOptions(Subscription subscription)
    {
        var item = SubscriptionItem(subscription, PlanIds.Single());
        _prevStorage = item?.Quantity ?? 0;
        return new()
        {
            new SubscriptionItemOptions
            {
                Id = item?.Id,
                Plan = _plan,
                Quantity = _additionalStorage,
                Deleted = (item?.Id != null && _additionalStorage == 0) ? true : (bool?)null,
            }
        };
    }

    public override List<SubscriptionItemOptions> RevertItemsOptions(Subscription subscription)
    {
        if (!_prevStorage.HasValue)
        {
            throw new Exception("Unknown previous value, must first call UpgradeItemsOptions");
        }

        var item = SubscriptionItem(subscription, PlanIds.Single());
        return new()
        {
            new SubscriptionItemOptions
            {
                Id = item?.Id,
                Plan = _plan,
                Quantity = _prevStorage.Value,
                Deleted = _prevStorage.Value == 0 ? true : (bool?)null,
            }
        };
    }
}

public class SponsorOrganizationSubscriptionUpdate : SubscriptionUpdate
{
    private readonly string _existingPlanStripeId;
    private readonly string _sponsoredPlanStripeId;
    private readonly bool _applySponsorship;
    protected override List<string> PlanIds => new() { _existingPlanStripeId, _sponsoredPlanStripeId };

    public SponsorOrganizationSubscriptionUpdate(StaticStore.Plan existingPlan, StaticStore.SponsoredPlan sponsoredPlan, bool applySponsorship)
    {
        _existingPlanStripeId = existingPlan.PasswordManager.StripePlanId;
        _sponsoredPlanStripeId = sponsoredPlan?.StripePlanId;
        _applySponsorship = applySponsorship;
    }

    public override List<SubscriptionItemOptions> RevertItemsOptions(Subscription subscription)
    {
        var result = new List<SubscriptionItemOptions>();
        if (!string.IsNullOrWhiteSpace(AddStripePlanId))
        {
            result.Add(new SubscriptionItemOptions
            {
                Id = AddStripeItem(subscription)?.Id,
                Plan = AddStripePlanId,
                Quantity = 0,
                Deleted = true,
            });
        }

        if (!string.IsNullOrWhiteSpace(RemoveStripePlanId))
        {
            result.Add(new SubscriptionItemOptions
            {
                Id = RemoveStripeItem(subscription)?.Id,
                Plan = RemoveStripePlanId,
                Quantity = 1,
                Deleted = false,
            });
        }
        return result;
    }

    public override List<SubscriptionItemOptions> UpgradeItemsOptions(Subscription subscription)
    {
        var result = new List<SubscriptionItemOptions>();
        if (RemoveStripeItem(subscription) != null)
        {
            result.Add(new SubscriptionItemOptions
            {
                Id = RemoveStripeItem(subscription)?.Id,
                Plan = RemoveStripePlanId,
                Quantity = 0,
                Deleted = true,
            });
        }

        if (!string.IsNullOrWhiteSpace(AddStripePlanId))
        {
            result.Add(new SubscriptionItemOptions
            {
                Id = AddStripeItem(subscription)?.Id,
                Plan = AddStripePlanId,
                Quantity = 1,
                Deleted = false,
            });
        }
        return result;
    }

    private string RemoveStripePlanId => _applySponsorship ? _existingPlanStripeId : _sponsoredPlanStripeId;
    private string AddStripePlanId => _applySponsorship ? _sponsoredPlanStripeId : _existingPlanStripeId;
    private Stripe.SubscriptionItem RemoveStripeItem(Subscription subscription) =>
        _applySponsorship ?
            SubscriptionItem(subscription, _existingPlanStripeId) :
            SubscriptionItem(subscription, _sponsoredPlanStripeId);
    private Stripe.SubscriptionItem AddStripeItem(Subscription subscription) =>
        _applySponsorship ?
            SubscriptionItem(subscription, _sponsoredPlanStripeId) :
            SubscriptionItem(subscription, _existingPlanStripeId);

}

public class SecretsManagerSubscribeUpdate : SubscriptionUpdate
{
    private readonly StaticStore.Plan _plan;
    private readonly long? _additionalSeats;
    private readonly long? _additionalServiceAccounts;
    private readonly int _previousSeats;
    private readonly int _previousServiceAccounts;
    protected override List<string> PlanIds => new() { _plan.SecretsManager.StripeSeatPlanId, _plan.SecretsManager.StripeServiceAccountPlanId };
    public SecretsManagerSubscribeUpdate(Organization organization, StaticStore.Plan plan, long? additionalSeats, long? additionalServiceAccounts)
    {
        _plan = plan;
        _additionalSeats = additionalSeats;
        _additionalServiceAccounts = additionalServiceAccounts;
        _previousSeats = organization.SmSeats.GetValueOrDefault();
        _previousServiceAccounts = organization.SmServiceAccounts.GetValueOrDefault();
    }

    public override List<SubscriptionItemOptions> RevertItemsOptions(Subscription subscription)
    {
        var updatedItems = new List<SubscriptionItemOptions>();

        RemovePreviousSecretsManagerItems(updatedItems);

        return updatedItems;
    }

    public override List<SubscriptionItemOptions> UpgradeItemsOptions(Subscription subscription)
    {
        var updatedItems = new List<SubscriptionItemOptions>();

        AddNewSecretsManagerItems(updatedItems);

        return updatedItems;
    }

    private void AddNewSecretsManagerItems(List<SubscriptionItemOptions> updatedItems)
    {
        if (_additionalSeats > 0)
        {
            updatedItems.Add(new SubscriptionItemOptions
            {
                Price = _plan.SecretsManager.StripeSeatPlanId,
                Quantity = _additionalSeats
            });
        }

        if (_additionalServiceAccounts > 0)
        {
            updatedItems.Add(new SubscriptionItemOptions
            {
                Price = _plan.SecretsManager.StripeServiceAccountPlanId,
                Quantity = _additionalServiceAccounts
            });
        }
    }

    private void RemovePreviousSecretsManagerItems(List<SubscriptionItemOptions> updatedItems)
    {
        updatedItems.Add(new SubscriptionItemOptions
        {
            Price = _plan.SecretsManager.StripeSeatPlanId,
            Quantity = _previousSeats,
            Deleted = _previousSeats == 0 ? true : (bool?)null,
        });

        updatedItems.Add(new SubscriptionItemOptions
        {
            Price = _plan.SecretsManager.StripeServiceAccountPlanId,
            Quantity = _previousServiceAccounts,
            Deleted = _previousServiceAccounts == 0 ? true : (bool?)null,
        });
    }
}
