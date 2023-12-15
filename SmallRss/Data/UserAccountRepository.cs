using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Data;

public class UserAccountRepository(ILogger<UserAccountRepository> logger, SqliteDataContext context) : IUserAccountRepository
{
    public async Task<UserAccount> FindOrCreateAsync(ClaimsPrincipal user)
    {
        var userIdentifier = (user?.FindFirst("name")?.Value) ?? throw new ArgumentException($"Cannot find identifier claim from user: {user}", nameof(user));
        var userAccountSetting = await context.UserAccountSettings!.SingleOrDefaultAsync(uas =>
            uas.SettingType == "AuthenticationId" &&
            uas.SettingValue == userIdentifier);

        var userAccount = userAccountSetting == null ? null : await GetByIdAsync(userAccountSetting.UserAccountId);
        return userAccount ?? await CreateAsync(userIdentifier);
    }

    public async Task UpdateAsync(UserAccount userAccount)
    {
        var userAccountSettings = await context.UserAccountSettings!.Where(uas => uas.UserAccountId == userAccount.Id).ToListAsync();
        var userAccountSetting = userAccountSettings.FirstOrDefault(uas => uas.SettingType == "ShowAllItems" && uas.SettingName == "ShowAllItems");
        if (userAccountSetting == null)
            await context.UserAccountSettings!.AddAsync(new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "ShowAllItems", SettingName = "ShowAllItems", SettingValue = Convert.ToString(userAccount.ShowAllItems) });
        else
            userAccountSetting.SettingValue = Convert.ToString(userAccount.ShowAllItems);

        userAccountSetting = userAccountSettings.FirstOrDefault(uas => uas.SettingType == "PocketAccessToken" && uas.SettingName == "PocketAccessToken");
        if (userAccountSetting == null)
            await context.UserAccountSettings!.AddAsync(new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "PocketAccessToken", SettingName = "PocketAccessToken", SettingValue = userAccount.PocketAccessToken });
        else
            userAccountSetting.SettingValue = userAccount.PocketAccessToken;

        userAccountSetting = userAccountSettings.FirstOrDefault(uas => uas.SettingType == "RaindropRefreshToken" && uas.SettingName == "RaindropRefreshToken");
        if (userAccountSetting == null)
            await context.UserAccountSettings!.AddAsync(new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "RaindropRefreshToken", SettingName = "RaindropRefreshToken", SettingValue = userAccount.RaindropRefreshToken });
        else
            userAccountSetting.SettingValue = userAccount.RaindropRefreshToken;

        context.UserAccountSettings!.RemoveRange(userAccountSettings.Where(uas => uas.SettingType == "ExpandedGroup"));
        await context.UserAccountSettings.AddRangeAsync(userAccount.ExpandedGroups.Select(g => new UserAccountSetting
        {
            UserAccountId = userAccount.Id,
            SettingType = "ExpandedGroup",
            SettingName = "ExpandedGroup",
            SettingValue = g
        }));

        context.UserAccountSettings.RemoveRange(userAccountSettings.Where(uas => uas.SettingType == "SavedLayout"));
        await context.UserAccountSettings.AddRangeAsync(userAccount.SavedLayout.Select(l => new UserAccountSetting
        {
            UserAccountId = userAccount.Id,
            SettingType = "SavedLayout",
            SettingName = l.Key,
            SettingValue = l.Value
        }));

        await context.SaveChangesAsync();
    }

    private async Task<UserAccount> CreateAsync(string authenticationId)
    {
        logger.LogInformation($"Creating account for auth: {authenticationId}");
        var userAccount = new UserAccount {LastLogin = DateTime.UtcNow};
        await context.UserAccounts!.AddAsync(userAccount);
        await context.SaveChangesAsync();
        logger.LogInformation($"Created account with id {userAccount.Id}");

        logger.LogInformation($"Creating account settings for id {userAccount.Id}");
        await context.UserAccountSettings!.AddRangeAsync(
            new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "AuthenticationId", SettingName = "AuthenticationId", SettingValue = authenticationId },
            new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "ShowAllItems", SettingName = "ShowAllItems", SettingValue = Convert.ToString(userAccount.ShowAllItems) },
            new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "PocketAccessToken", SettingName = "PocketAccessToken", SettingValue = userAccount.PocketAccessToken },
            new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "RaindropRefreshToken", SettingName = "RaindropRefreshToken", SettingValue = userAccount.RaindropRefreshToken }
        );
        await context.SaveChangesAsync();

        return userAccount;
    }

    private async Task<UserAccount?> GetByIdAsync(int userAccountId)
    {
        var userAccount = await context.UserAccounts!.FindAsync(userAccountId);
        if (userAccount == null)
            return null;
        var userAccountSettings = await context.UserAccountSettings!.Where(uas => uas.UserAccountId == userAccountId).ToListAsync();
        
        foreach (var authId in userAccountSettings.Where(uas => uas.SettingType == "AuthenticationId"))
            userAccount.AuthenticationIds.Add(authId.SettingValue ?? "");
        foreach (var expandedGroup in userAccountSettings.Where(uas => uas.SettingType == "ExpandedGroup"))
            userAccount.ExpandedGroups.Add(expandedGroup.SettingValue ?? "");
        foreach (var savedLayout in userAccountSettings.Where(uas => uas.SettingType == "SavedLayout"))
            userAccount.SavedLayout.Add(savedLayout.SettingName ?? "", savedLayout.SettingValue ?? "");
        var showAllItemsSetting = userAccountSettings.FirstOrDefault(uas => uas.SettingType == "ShowAllItems");
        userAccount.ShowAllItems = showAllItemsSetting != null && Convert.ToBoolean(showAllItemsSetting.SettingValue);
        var pocketAccessToken = userAccountSettings.FirstOrDefault(uas => uas.SettingType == "PocketAccessToken");
        userAccount.PocketAccessToken = pocketAccessToken == null ? string.Empty : pocketAccessToken.SettingValue ?? "";
        var raindropRefreshToken = userAccountSettings.FirstOrDefault(uas => uas.SettingType == "RaindropRefreshToken");
        userAccount.RaindropRefreshToken = raindropRefreshToken == null ? string.Empty : raindropRefreshToken.SettingValue ?? "";
        
        return userAccount;
    }
}