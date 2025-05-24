using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallRss.Models;

namespace SmallRss.Data;

public class UserAccountRepository(ILogger<UserAccountRepository> logger, SqliteDataContext context) : IUserAccountRepository
{
    public async Task<UserAccount> GetAsync(ClaimsPrincipal user)
    {
        var userAccount = await FindAsync(user);
        return userAccount ?? throw new ArgumentException($"Cannot find user with identity: {user?.Identity?.Name}", nameof(user));
    }

    public Task<UserAccount?> FindAsync(ClaimsPrincipal user)
    {
        var email = user?.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email))
        {
            logger.LogInformation($"Could not get account identifier from principal: {user?.Identity?.Name}:[{string.Join(',', (user?.Claims ?? Enumerable.Empty<Claim>()).Select(c => $"{c.Type}={c.Value}"))}]");
            throw new ArgumentException($"Cannot find identifier claim from user: {user?.Identity?.Name}", nameof(user));
        }

        return FindByEmailAsync(email);
    }

    public async Task<UserAccount?> FindByEmailAsync(string email)
    {
        var userAccountSetting = await context.UserAccountSettings!.SingleOrDefaultAsync(uas =>
            uas.SettingType == "Email" &&
            uas.SettingValue == email);

        return userAccountSetting == null ? null : await GetByIdAsync(userAccountSetting.UserAccountId);
    }

    public async Task<UserAccount?> FindByUserHandleAsync(byte[] userHandle)
    {
        var userAccountSetting = await context.UserAccountSettings!.SingleOrDefaultAsync(uas =>
            uas.SettingType != null &&
            uas.SettingType.StartsWith("UserAccountCredential.") &&
            uas.SettingName == "UserHandle" &&
            uas.SettingValue == Convert.ToBase64String(userHandle));

        return userAccountSetting == null ? null : await GetByIdAsync(userAccountSetting.UserAccountId);
    }

    public async Task UpdateAsync(UserAccount userAccount)
    {
        var userAccountSettings = await context.UserAccountSettings!.Where(uas => uas.UserAccountId == userAccount.Id).ToListAsync();

        context.UserAccountSettings!.RemoveRange(userAccountSettings.Where(uas => uas.SettingType?.StartsWith("UserAccountCredential.") ?? false));
        for (var userAccountCredential = 0; userAccountCredential < userAccount.UserAccountCredentials.Count; userAccountCredential++)
        {
            var settingType = $"UserAccountCredential.{userAccountCredential}";
            context.UserAccountSettings!.AddRange(
                new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = settingType, SettingName = "CredentialId", SettingValue = Convert.ToBase64String(userAccount.UserAccountCredentials[userAccountCredential].CredentialId) },
                new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = settingType, SettingName = "PublicKey", SettingValue = Convert.ToBase64String(userAccount.UserAccountCredentials[userAccountCredential].PublicKey) },
                new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = settingType, SettingName = "UserHandle", SettingValue = Convert.ToBase64String(userAccount.UserAccountCredentials[userAccountCredential].UserHandle) },
                new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = settingType, SettingName = "SignatureCount", SettingValue = Convert.ToString(userAccount.UserAccountCredentials[userAccountCredential].SignatureCount) }
            );
        }

        var userAccountSetting = userAccountSettings.FirstOrDefault(uas => uas.SettingType == "ShowAllItems" && uas.SettingName == "ShowAllItems");
        if (userAccountSetting == null)
            await context.UserAccountSettings!.AddAsync(new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "ShowAllItems", SettingName = "ShowAllItems", SettingValue = Convert.ToString(userAccount.ShowAllItems) });
        else
            userAccountSetting.SettingValue = Convert.ToString(userAccount.ShowAllItems);

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

    public async Task<UserAccount> CreateAsync(string email, byte[] credentialId, byte[] publicKey, byte[] userHandle)
    {
        logger.LogInformation($"Creating account for email: {email}");
        var userAccount = new UserAccount { LastLogin = DateTime.UtcNow };
        var newUser = await context.UserAccounts!.AddAsync(userAccount);
        await context.SaveChangesAsync();
        logger.LogInformation($"Created account with id {userAccount.Id}");

        logger.LogInformation($"Creating account settings for id {userAccount.Id}");
        await context.UserAccountSettings!.AddRangeAsync(
            new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "Email", SettingName = "Email", SettingValue = email },
            new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "UserAccountCredential.0", SettingName = "CredentialId", SettingValue = Convert.ToBase64String(credentialId) },
            new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "UserAccountCredential.0", SettingName = "PublicKey", SettingValue = Convert.ToBase64String(publicKey) },
            new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "UserAccountCredential.0", SettingName = "UserHandle", SettingValue = Convert.ToBase64String(userHandle) },
            new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "UserAccountCredential.0", SettingName = "SignatureCount", SettingValue = Convert.ToString((uint)0) },
            new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "ShowAllItems", SettingName = "ShowAllItems", SettingValue = Convert.ToString(userAccount.ShowAllItems) },
            new UserAccountSetting { UserAccountId = userAccount.Id, SettingType = "RaindropRefreshToken", SettingName = "RaindropRefreshToken", SettingValue = userAccount.RaindropRefreshToken }
        );
        await context.SaveChangesAsync();

        return await GetByIdAsync(newUser.Entity.Id) ?? throw new InvalidOperationException($"Could not create user account with email: [{email}]");
    }

    private async Task<UserAccount?> GetByIdAsync(int userAccountId)
    {
        var userAccount = await context.UserAccounts!.FindAsync(userAccountId);
        if (userAccount == null)
            return null;
        var userAccountSettings = await context.UserAccountSettings!.Where(uas => uas.UserAccountId == userAccountId).ToListAsync();

        var email = userAccountSettings.FirstOrDefault(uas => uas.SettingType == "Email");
        userAccount.Email = email?.SettingValue ?? "";

        var userAccountCredential = 0;
        do
        {
            var userCredentials = userAccountSettings.Where(uas => uas.SettingType == $"UserAccountCredential.{userAccountCredential}").ToList();
            if (userCredentials.Count != 4)
                break;

            var credentialId = userCredentials.SingleOrDefault(uc => uc.SettingName == "CredentialId")?.SettingValue;
            if (credentialId == null)
                break;
            var publicKey = userCredentials.SingleOrDefault(uc => uc.SettingName == "PublicKey")?.SettingValue;
            if (publicKey == null)
                break;
            var userHandle = userCredentials.SingleOrDefault(uc => uc.SettingName == "UserHandle")?.SettingValue;
            if (userHandle == null)
                break;
            var signatureCount = userCredentials.SingleOrDefault(uc => uc.SettingName == "SignatureCount")?.SettingValue;
            if (signatureCount == null)
                break;

            userAccount.UserAccountCredentials.Add(new()
            {
                CredentialId = Convert.FromBase64String(credentialId),
                PublicKey = Convert.FromBase64String(publicKey),
                UserHandle = Convert.FromBase64String(userHandle),
                SignatureCount = Convert.ToUInt32(signatureCount)
            });

            userAccountCredential++;
        } while (true);

        foreach (var expandedGroup in userAccountSettings.Where(uas => uas.SettingType == "ExpandedGroup"))
            userAccount.ExpandedGroups.Add(expandedGroup.SettingValue ?? "");
        foreach (var savedLayout in userAccountSettings.Where(uas => uas.SettingType == "SavedLayout"))
            userAccount.SavedLayout.Add(savedLayout.SettingName ?? "", savedLayout.SettingValue ?? "");
        var showAllItemsSetting = userAccountSettings.FirstOrDefault(uas => uas.SettingType == "ShowAllItems");
        userAccount.ShowAllItems = showAllItemsSetting != null && Convert.ToBoolean(showAllItemsSetting.SettingValue);
        var raindropRefreshToken = userAccountSettings.FirstOrDefault(uas => uas.SettingType == "RaindropRefreshToken");
        userAccount.RaindropRefreshToken = raindropRefreshToken == null ? "" : raindropRefreshToken.SettingValue ?? "";

        return userAccount;
    }
}