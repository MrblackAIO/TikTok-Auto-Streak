using TiktokStreakSaver.Services.Storage;

namespace TiktokStreakSaver.Services;

/// <summary>
/// Service for managing TikTok session state and profile info
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class SessionService
{
    private readonly IAppStorage _storage = AppStorageProvider.Current;

    private const string SessionValidKey = "session_valid";
    private const string SessionLastCheckKey = "session_last_check";
    private const string DisplayNameKey = "session_display_name";
    private const string LoginUserAgentKey = "session_login_ua";
    private const string ProfileImagePathKey = "session_profile_photo";

    private const string TikTokUsernameKey = "session_tiktok_username";
    private const string TikTokNicknameKey = "session_tiktok_nickname";
    private const string TikTokBioKey = "session_tiktok_bio";
    private const string TikTokFollowersKey = "session_tiktok_followers";
    private const string TikTokFollowingKey = "session_tiktok_following";

    /// <summary>
    /// Get whether the session was valid on last check
    /// </summary>
    public bool IsSessionValid()
    {
#if IOS
        if (Platforms.iOS.Services.IosSessionFileStorage.ReadValid(out _))
            return true;
#endif
        return _storage.GetBool(SessionValidKey, false);
    }

    public bool TrySetSessionValid(bool valid, out string? error)
    {
        error = null;
        var ticks = DateTime.Now.Ticks;
        _storage.SetBool(SessionValidKey, valid);
        _storage.SetLong(SessionLastCheckKey, ticks);

#if IOS
        if (!Platforms.iOS.Services.IosSessionFileStorage.WriteValid(valid, ticks))
        {
            error = "Session state did not persist on this device.";
            System.Diagnostics.Debug.WriteLine($"TrySetSessionValid file write failed for valid={valid}");
            return false;
        }
#endif

        if (_storage.GetBool(SessionValidKey, !valid) != valid
#if IOS
            && !Platforms.iOS.Services.IosSessionFileStorage.ReadValid(out _)
#endif
           )
        {
            error = "Session state did not persist on this device.";
            System.Diagnostics.Debug.WriteLine($"TrySetSessionValid read-back failed for valid={valid}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Set whether the session is valid
    /// </summary>
    public void SetSessionValid(bool valid)
    {
        TrySetSessionValid(valid, out _);
    }

    /// <summary>
    /// Get when the session was last checked
    /// </summary>
    public DateTime? GetLastCheckTime()
    {
        var ticks = _storage.GetLong(SessionLastCheckKey, 0L);
        return ticks > 0 ? new DateTime(ticks) : null;
    }

    /// <summary>
    /// Get the user's display name (defaults to TikTok nickname if set, otherwise "User")
    /// </summary>
    public string GetDisplayName()
    {
        var nick = GetTikTokNickname();
        if (!string.IsNullOrWhiteSpace(nick))
            return nick;
        return _storage.GetString(DisplayNameKey, "User");
    }

    /// <summary>
    /// Set the user's display name
    /// </summary>
    public void SetDisplayName(string name)
    {
        _storage.SetString(DisplayNameKey, string.IsNullOrWhiteSpace(name) ? "User" : name.Trim());
    }

    /// <summary>
    /// Get the user agent string used during the last login session
    /// </summary>
    public string? GetLoginUserAgent()
    {
        var ua = _storage.GetString(LoginUserAgentKey, string.Empty);
        return string.IsNullOrEmpty(ua) ? null : ua;
    }

    /// <summary>
    /// Store the user agent string used during login for session consistency
    /// </summary>
    public void SetLoginUserAgent(string userAgent)
    {
        _storage.SetString(LoginUserAgentKey, userAgent ?? string.Empty);
    }

    /// <summary>
    /// Get the path to the user's local profile photo
    /// </summary>
    public string GetProfileImagePath()
    {
        return _storage.GetString(ProfileImagePathKey, string.Empty);
    }

    /// <summary>
    /// Set the path to the user's local profile photo
    /// </summary>
    public void SetProfileImagePath(string path)
    {
        _storage.SetString(ProfileImagePathKey, path ?? string.Empty);
    }

    /// <summary>
    /// TikTok Handle / Unique ID (e.g. "username")
    /// </summary>
    public string GetTikTokUsername() => _storage.GetString(TikTokUsernameKey, string.Empty);
    public void SetTikTokUsername(string username) => _storage.SetString(TikTokUsernameKey, username ?? string.Empty);

    /// <summary>
    /// TikTok Display Name / Nickname
    /// </summary>
    public string GetTikTokNickname() => _storage.GetString(TikTokNicknameKey, string.Empty);
    public void SetTikTokNickname(string nickname) => _storage.SetString(TikTokNicknameKey, nickname ?? string.Empty);

    /// <summary>
    /// TikTok Profile Bio / Signature
    /// </summary>
    public string GetTikTokBio() => _storage.GetString(TikTokBioKey, string.Empty);
    public void SetTikTokBio(string bio) => _storage.SetString(TikTokBioKey, bio ?? string.Empty);

    /// <summary>
    /// TikTok Followers count
    /// </summary>
    public int GetTikTokFollowers() => (int)_storage.GetLong(TikTokFollowersKey, 0L);
    public void SetTikTokFollowers(int count) => _storage.SetLong(TikTokFollowersKey, count);

    /// <summary>
    /// TikTok Following count
    /// </summary>
    public int GetTikTokFollowing() => (int)_storage.GetLong(TikTokFollowingKey, 0L);
    public void SetTikTokFollowing(int count) => _storage.SetLong(TikTokFollowingKey, count);

    /// <summary>
    /// Clear session data (logout). Also physically destroys WebView cookies
    /// to guarantee a clean logout.
    /// </summary>
    public void ClearSession()
    {
        _storage.SetBool(SessionValidKey, false);
        _storage.Remove(SessionLastCheckKey);
        _storage.Remove(TikTokUsernameKey);
        _storage.Remove(TikTokNicknameKey);
        _storage.Remove(TikTokBioKey);
        _storage.Remove(TikTokFollowersKey);
        _storage.Remove(TikTokFollowingKey);
        _storage.Remove(ProfileImagePathKey);

        TikTokWebViewHelper.ClearAllCookies();
#if IOS
        Platforms.iOS.Services.IosSessionFileStorage.Clear();
        Platforms.iOS.Services.CookieSyncService.ClearExportedCookies();
        _storage.SetBool(AppConstants.AuthRequiredKey, false);
#endif
    }
}
