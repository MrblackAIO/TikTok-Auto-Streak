using System.Text.Json;
using TiktokStreakSaver.Services.Storage;

namespace TiktokStreakSaver.Services;

/// <summary>
/// Passive TikTok profile extraction helper.
/// Reads SSR rehydration state (__UNIVERSAL_DATA_FOR_REHYDRATION__) and DOM elements
/// without issuing active outbound HTTP fetch calls that trigger anti-bot detection.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public static class TikTokProfileSyncHelper
{
    public const string ExtractScript = @"
(async function() {
    try {
        let profile = {
            username: '',
            nickname: '',
            avatarUrl: '',
            bio: '',
            followers: 0,
            following: 0
        };

        // 1. Primary authenticated endpoint: TikTok Passport Web Account Info
        try {
            let res = await fetch('https://www.tiktok.com/passport/web/account/info/', { credentials: 'include' });
            let data = await res.json();
            if (data && data.data) {
                profile.username = data.data.unique_id || data.data.user_id || '';
                profile.nickname = data.data.nickname || profile.username;
                profile.avatarUrl = data.data.avatar_url || '';
            }
        } catch(e) {}

        // 2. If username obtained, fetch extended detail for bio, avatar, and stats
        if (profile.username) {
            try {
                let detailRes = await fetch('https://www.tiktok.com/api/user/detail/?uniqueId=' + encodeURIComponent(profile.username) + '&aid=1988', { credentials: 'include' });
                let detailData = await detailRes.json();
                if (detailData && detailData.userInfo) {
                    profile.bio = detailData.userInfo.user?.signature || '';
                    profile.nickname = detailData.userInfo.user?.nickname || profile.nickname;
                    profile.avatarUrl = detailData.userInfo.user?.avatarLarger || detailData.userInfo.user?.avatarMedium || profile.avatarUrl;
                    profile.followers = detailData.userInfo.stats?.followerCount || 0;
                    profile.following = detailData.userInfo.stats?.followingCount || 0;
                }
            } catch(e) {}
            return JSON.stringify(profile);
        }

        // 3. SSR Hydration fallback
        const rehydrateEl = document.getElementById('__UNIVERSAL_DATA_FOR_REHYDRATION__');
        if (rehydrateEl && rehydrateEl.textContent) {
            try {
                const data = JSON.parse(rehydrateEl.textContent);
                const defaultScope = data?.['__DEFAULT_SCOPE__'];
                const userDetail = defaultScope?.['webapp.user-detail'];
                const appContext = defaultScope?.['webapp.app-context'];
                const user = userDetail?.userInfo?.user || appContext?.user;
                const stats = userDetail?.userInfo?.stats;

                if (user && (user.uniqueId || user.id)) {
                    profile.username = user.uniqueId || user.id || '';
                    profile.nickname = user.nickname || profile.username;
                    profile.avatarUrl = user.avatarLarger || user.avatarMedium || user.avatarThumb || '';
                    profile.bio = user.signature || '';
                    profile.followers = stats?.followerCount || 0;
                    profile.following = stats?.followingCount || 0;
                    return JSON.stringify(profile);
                }
            } catch(e) {}
        }

        // 4. Secondary fallback: inspect existing DOM anchors & images
        const profileAnchor = document.querySelector('a[href*=""/@""]');
        if (profileAnchor) {
            const href = profileAnchor.getAttribute('href') || '';
            const match = href.match(/@([a-zA-Z0-9_.-]+)/);
            if (match && match[1]) {
                profile.username = match[1];
            }
            const avatarImg = profileAnchor.querySelector('img');
            if (avatarImg && avatarImg.src) {
                profile.avatarUrl = avatarImg.src;
            }
            
            const titleEl = document.querySelector('[data-e2e=""user-title""], [data-e2e=""user-subtitle""], [class*=""UserTitle""], h1, h2');
            if (titleEl && titleEl.textContent) {
                profile.nickname = titleEl.textContent.trim();
            } else if (profileAnchor.getAttribute('title')) {
                profile.nickname = profileAnchor.getAttribute('title').trim();
            }

            if (profile.username) {
                return JSON.stringify(profile);
            }
        }

        return '';
    } catch(e) {
        return '';
    }
})();";

    /// <summary>
    /// Executes the passive extraction script on a MAUI WebView and persists extracted profile data.
    /// </summary>
    public static async Task<bool> ExtractAndSaveProfileAsync(WebView webView, SessionService sessionService)
    {
        if (webView == null || sessionService == null) return false;

        try
        {
            var rawJson = await webView.EvaluateJavaScriptAsync(ExtractScript);
            return await ParseAndSaveJsonAsync(rawJson, sessionService);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileSync] Extraction error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Parses the raw JSON string returned by the JavaScript extraction script and persists to SessionService.
    /// </summary>
    public static async Task<bool> ParseAndSaveJsonAsync(string? rawJson, SessionService sessionService)
    {
        if (string.IsNullOrWhiteSpace(rawJson) || rawJson == "\"\"" || rawJson == "null")
            return false;

        try
        {
            string jsonString = rawJson;
            if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
            {
                try
                {
                    jsonString = JsonSerializer.Deserialize<string>(rawJson) ?? rawJson;
                }
                catch { }
            }

            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            bool updated = false;

            if (root.TryGetProperty("username", out var uProp) && !string.IsNullOrEmpty(uProp.GetString()))
            {
                var username = uProp.GetString()!.Trim().TrimStart('@');
                if (!string.IsNullOrEmpty(username))
                {
                    sessionService.SetTikTokUsername(username);
                    updated = true;
                }
            }

            if (root.TryGetProperty("nickname", out var nProp) && !string.IsNullOrEmpty(nProp.GetString()))
            {
                var nick = nProp.GetString()!.Trim();
                if (!string.IsNullOrEmpty(nick))
                {
                    sessionService.SetTikTokNickname(nick);
                    sessionService.SetDisplayName(nick);
                    updated = true;
                }
            }

            if (root.TryGetProperty("bio", out var bProp))
            {
                var bio = bProp.GetString() ?? string.Empty;
                sessionService.SetTikTokBio(bio);
            }

            if (root.TryGetProperty("followers", out var fProp) && fProp.TryGetInt32(out var followers))
            {
                sessionService.SetTikTokFollowers(followers);
            }

            if (root.TryGetProperty("following", out var fgProp) && fgProp.TryGetInt32(out var following))
            {
                sessionService.SetTikTokFollowing(following);
            }

            if (root.TryGetProperty("avatarUrl", out var aProp) && !string.IsNullOrEmpty(aProp.GetString()))
            {
                var avatarUrl = aProp.GetString()!;
                if (!string.IsNullOrEmpty(avatarUrl) && avatarUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    await DownloadAndSaveAvatarAsync(avatarUrl, sessionService);
                }
            }

            return updated;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileSync] Failed parsing profile JSON: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Downloads the user's avatar image locally and saves the path in SessionService.
    /// </summary>
    public static async Task<bool> DownloadAndSaveAvatarAsync(string avatarUrl, SessionService sessionService)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var imageBytes = await client.GetByteArrayAsync(avatarUrl);
            if (imageBytes != null && imageBytes.Length > 0)
            {
                var localPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "tiktok_avatar.jpg");
                await System.IO.File.WriteAllBytesAsync(localPath, imageBytes);
                sessionService.SetProfileImagePath(localPath);
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileSync] Failed to download avatar image: {ex.Message}");
        }

        return false;
    }
}
