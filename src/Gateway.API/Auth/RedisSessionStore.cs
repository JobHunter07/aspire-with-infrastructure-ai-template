using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace Gateway.API.Auth;

public class RedisSessionStore(IDistributedCache distributedCache, ILogger<RedisSessionStore> logger) : ITicketStore
{
    private const string KeyPrefix = "_oauth2_proxy-";

    private readonly DistributedCacheEntryOptions _cacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
    };

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = KeyPrefix + Guid.NewGuid();
        var serializedTicket = TicketSerializer.Default.Serialize(ticket);
        await distributedCache.SetAsync(key, serializedTicket, _cacheEntryOptions);
        logger.LogInformation("Storing auth ticket with key {authTicketKey}", key);
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        logger.LogInformation("Renew ticket with key {authTicketKey} and schema {authSchema}", key,
            ticket.AuthenticationScheme);
        await distributedCache.SetAsync(key, TicketSerializer.Default.Serialize(ticket), _cacheEntryOptions);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        logger.LogInformation("Getting ticket with {authTicketKey}", key);
        var cachedMember = await distributedCache.GetAsync(key);
        if (cachedMember == null)
        {
            return null;
        }

        return TicketSerializer.Default.Deserialize(cachedMember);
    }

    public async Task RemoveAsync(string key)
    {
        var ticket = await distributedCache.GetAsync(key);
        if (ticket != null)
        {
            await distributedCache.RemoveAsync(key);
            logger.LogInformation("Removing ticket with key {authTicketKey}", key);
        }
    }
}
