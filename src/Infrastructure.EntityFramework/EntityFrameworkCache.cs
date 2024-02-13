﻿using Bit.Infrastructure.EntityFramework.Models;
using Bit.Infrastructure.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Infrastructure.EntityFramework;

public class EntityFrameworkCache : IDistributedCache
{
    private readonly TimeSpan _expiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
    private DateTimeOffset _lastExpirationScan;
    private readonly Action _deleteExpiredCachedItemsDelegate;
    private readonly TimeSpan _defaultSlidingExpiration = TimeSpan.FromMinutes(20);
    private readonly object _mutex = new();
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EntityFrameworkCache(IServiceScopeFactory serviceScopeFactory)
    {
        _deleteExpiredCachedItemsDelegate = DeleteExpiredCacheItems;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public byte[] Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = GetDatabaseContext(scope);
        var cache = dbContext.Cache
            .Where(c => c.Id == key && DateTime.UtcNow <= c.ExpiresAtTime)
            .SingleOrDefault();

        if (cache == null)
        {
            return null;
        }

        if (UpdateCacheExpiration(cache))
        {
            dbContext.SaveChanges();
        }

        ScanForExpiredItemsIfRequired();
        return cache?.Value;
    }

    public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        token.ThrowIfCancellationRequested();

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = GetDatabaseContext(scope);
        var cache = await dbContext.Cache
            .Where(c => c.Id == key && DateTime.UtcNow <= c.ExpiresAtTime)
            .SingleOrDefaultAsync(cancellationToken: token);

        if (cache == null)
        {
            return null;
        }

        if (UpdateCacheExpiration(cache))
        {
            await dbContext.SaveChangesAsync(token);
        }

        ScanForExpiredItemsIfRequired();
        return cache?.Value;
    }

    public void Refresh(string key) => Get(key);

    public Task RefreshAsync(string key, CancellationToken token = default) => GetAsync(key, token);

    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        using var scope = _serviceScopeFactory.CreateScope();
        GetDatabaseContext(scope).Cache
            .Where(c => c.Id == key)
            .ExecuteDelete();

        ScanForExpiredItemsIfRequired();
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        token.ThrowIfCancellationRequested();
        using var scope = _serviceScopeFactory.CreateScope();
        await GetDatabaseContext(scope).Cache
            .Where(c => c.Id == key)
            .ExecuteDeleteAsync(cancellationToken: token);

        ScanForExpiredItemsIfRequired();
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = GetDatabaseContext(scope);
        var cache = dbContext.Cache.Find(key);
        SetCache(cache, key, value, options);
        dbContext.SaveChanges();

        // TODO: Catch duplicate key exception on insert

        ScanForExpiredItemsIfRequired();
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);

        token.ThrowIfCancellationRequested();

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = GetDatabaseContext(scope);
        var cache = await dbContext.Cache.FindAsync(new object[] { key }, cancellationToken: token);
        SetCache(cache, key, value, options);
        await dbContext.SaveChangesAsync(token);

        // TODO: Catch duplicate key exception on insert

        ScanForExpiredItemsIfRequired();
    }

    private void SetCache(Cache cache, string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var utcNow = DateTime.UtcNow;

        // resolve options
        if (!options.AbsoluteExpiration.HasValue &&
            !options.AbsoluteExpirationRelativeToNow.HasValue &&
            !options.SlidingExpiration.HasValue)
        {
            options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = _defaultSlidingExpiration
            };
        }

        if (cache == null)
        {
            // do an insert
            cache = new Cache { Id = key };
        }

        var slidingExpiration = (long?)options.SlidingExpiration?.TotalSeconds;

        // calculate absolute expiration
        DateTimeOffset? absoluteExpiration = null;
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            absoluteExpiration = utcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }
        else if (options.AbsoluteExpiration.HasValue)
        {
            if (options.AbsoluteExpiration.Value <= utcNow)
            {
                throw new InvalidOperationException("The absolute expiration value must be in the future.");
            }

            absoluteExpiration = options.AbsoluteExpiration.Value;
        }

        // set values on cache
        cache.Value = value;
        cache.SlidingExpirationInSeconds = slidingExpiration;
        cache.AbsoluteExpiration = absoluteExpiration;
        if (slidingExpiration.HasValue)
        {
            cache.ExpiresAtTime = utcNow.AddSeconds(slidingExpiration.Value);
        }
        else if (absoluteExpiration.HasValue)
        {
            cache.ExpiresAtTime = absoluteExpiration.Value;
        }
        else
        {
            throw new InvalidOperationException("Either absolute or sliding expiration needs to be provided.");
        }
    }

    private bool UpdateCacheExpiration(Cache cache)
    {
        var utcNow = DateTime.UtcNow;
        if (cache.SlidingExpirationInSeconds.HasValue && (cache.AbsoluteExpiration.HasValue || cache.AbsoluteExpiration != cache.ExpiresAtTime))
        {
            if ((cache.AbsoluteExpiration.Value - utcNow).TotalSeconds <= cache.SlidingExpirationInSeconds)
            {
                cache.ExpiresAtTime = cache.AbsoluteExpiration.Value;
            }
            else
            {
                cache.ExpiresAtTime = utcNow.AddSeconds(cache.SlidingExpirationInSeconds.Value);
            }
            return true;
        }
        return false;
    }

    private void ScanForExpiredItemsIfRequired()
    {
        lock (_mutex)
        {
            var utcNow = DateTime.UtcNow;
            if ((utcNow - _lastExpirationScan) > _expiredItemsDeletionInterval)
            {
                _lastExpirationScan = utcNow;
                Task.Run(_deleteExpiredCachedItemsDelegate);
            }
        }
    }

    private void DeleteExpiredCacheItems()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        GetDatabaseContext(scope).Cache
            .Where(c => DateTime.UtcNow > c.ExpiresAtTime)
            .ExecuteDelete();
    }

    private DatabaseContext GetDatabaseContext(IServiceScope serviceScope)
    {
        return serviceScope.ServiceProvider.GetRequiredService<DatabaseContext>();
    }
}
