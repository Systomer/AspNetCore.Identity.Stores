﻿using AspNetCore.Identity.Stores.Repositories;
using AspNetCore.Identity.Stores.Shared.Extensions;
using Azure.Data.Tables;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AspNetCore.Identity.Stores.AzureStorageAccount.Repositories;

internal class UserLoginsTable<TUser, TUserLogin, TKey> : TableStorage, IUserLoginsTable<TUser, TUserLogin, TKey>
    where TUser : IdentityUser<TKey>, new()
    where TUserLogin : IdentityUserLogin<TKey>, new()
    where TKey : IEquatable<TKey>
{
    private const string PartitionKey = "UserLogin";

    public UserLoginsTable(IDataProtectionProvider dataProtectionProvider, IOptions<IdentityStoresOptions> options) : base(dataProtectionProvider, options)
    {
    }

    public Task<IdentityResult> AddAsync(TUserLogin userLogin, CancellationToken cancellationToken)
    {
        return AddAsync(PartitionKey, GetHashKey(userLogin), userLogin, cancellationToken: cancellationToken);
    }

    public Task<IdentityResult> DeleteAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        return DeleteAsync(PartitionKey, GetHashKey(loginProvider, providerKey), cancellationToken: cancellationToken);
    }

    public async Task<IList<TUserLogin>> GetAsync(TUser user, CancellationToken cancellationToken)
    {
        string filter = TableClient.CreateQueryFilter($"PartitionKey eq {PartitionKey} and UserId eq {user.Id}");
        return (await QueryAsync<TUserLogin>(filter: filter, cancellationToken: cancellationToken)).ToList();
    }

    public async Task<TUser?> GetAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        var userLogin = await QueryAsync<TUserLogin>(PartitionKey, GetHashKey(loginProvider, providerKey), cancellationToken: cancellationToken);
        if (userLogin is not null)
            return await QueryAsync<TUser>(UsersTable<TUser,TKey>.PartitionKey, ConvertToString(userLogin.UserId), cancellationToken: cancellationToken);
        else
            return null;
    }

    public Task DeleteUserLoginsAsync(TUser user, CancellationToken cancellationToken)
    {
        string filter = TableClient.CreateQueryFilter($"PartitionKey eq {PartitionKey} and UserId eq {user.Id}");
        return DeleteBulkAsync(filter, cancellationToken);
    }

    private static string GetHashKey(TUserLogin userLogin) => GetHashKey(userLogin.LoginProvider, userLogin.ProviderKey);
    private static string GetHashKey(string loginProvider, string providerKey) => $"{loginProvider}-{providerKey}".GetHashString();
}
