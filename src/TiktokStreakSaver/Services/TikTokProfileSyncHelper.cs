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
(function() {
    try {
        let profile = {
            username: '',
            nickname: '',
            avatarUrl: '',
            bio: '',
            followers: 0,
            following: 0
        };

        // 1. SSR Hydration fallback
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

        // 2. Secondary fallback: inspect existing DOM anchors & images
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
    /// Fetches profile information directly from TikTok's authenticated passport endpoint
    /// using session cookies stored in the device's CookieManager or storage.
    /// Bypasses JavaScript evaluation and returns true user profile information.
    /// </summary>
    public static async Task<bool> FetchAndSaveProfileViaHttpAsync(SessionService sessionService, string? userAgent = null)
    {
        if (sessionService == null) return false;

        string cookies = string.Empty;
#if ANDROID
        try
        {
            var cookieManager = Android.Webkit.CookieManager.Instance;
            if (cookieManager != null)
            {
                var c1 = cookieManager.GetCookie("https://www.tiktok.com") ?? string.Empty;
                var c2 = cookieManager.GetCookie("https://tiktok.com") ?? string.Empty;
                cookies = string.IsNullOrWhiteSpace(c1) ? c2 : c1;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileSync] Cookie retrieval error: {ex.Message}");
        }
#endif

        if (string.IsNullOrWhiteSpace(cookies) || !cookies.Contains("sessionid="))
        {
            System.Diagnostics.Debug.WriteLine("[ProfileSync] No active sessionid cookie available for HTTP profile sync");
            return false;
        }

        try
        {
            using var handler = new System.Net.Http.HttpClientHandler
            {
                UseCookies = false
            };
            using var client = new System.Net.Http.HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(8);

            var ua = !string.IsNullOrEmpty(userAgent)
                ? userAgent
                : (sessionService.GetLoginUserAgent() ?? AppConstants.DesktopChromeUserAgent);

            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://www.tiktok.com/passport/web/account/info/");
            request.Headers.Add("Cookie", cookies);
            request.Headers.Add("User-Agent", ua);
            request.Headers.Add("Accept", "application/json, text/plain, */*");
            request.Headers.Add("Referer", "https://www.tiktok.com/");

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileSync] HTTP passport request failed with code: {response.StatusCode}");
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var dataProp))
            {
                string username = string.Empty;
                if (dataProp.TryGetProperty("unique_id", out var uidProp) && !string.IsNullOrEmpty(uidProp.GetString()))
                    username = uidProp.GetString()!.Trim().TrimStart('@');
                else if (dataProp.TryGetProperty("user_id", out var idProp) && !string.IsNullOrEmpty(idProp.GetString()))
                    username = idProp.GetString()!.Trim().TrimStart('@');

                if (!string.IsNullOrEmpty(username))
                {
                    sessionService.SetTikTokUsername(username);

                    string nickname = username;
                    if (dataProp.TryGetProperty("nickname", out var nickProp) && !string.IsNullOrEmpty(nickProp.GetString()))
                    {
                        nickname = nickProp.GetString()!.Trim();
                        sessionService.SetTikTokNickname(nickname);
                        sessionService.SetDisplayName(nickname);
                    }

                    if (dataProp.TryGetProperty("avatar_url", out var avProp) && !string.IsNullOrEmpty(avProp.GetString()))
                    {
                        var avatarUrl = avProp.GetString()!;
                        await DownloadAndSaveAvatarAsync(avatarUrl, sessionService);
                    }

                    System.Diagnostics.Debug.WriteLine($"[ProfileSync] Successfully extracted profile handle @{username} via HTTP passport endpoint");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileSync] HTTP profile extraction error: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Executes profile extraction (HTTP first, falling back to WebView script) and persists extracted data.
    /// </summary>
    public static async Task<bool> ExtractAndSaveProfileAsync(WebView webView, SessionService sessionService)
    {
        if (sessionService == null) return false;

        // 1. Try direct HTTP extraction first (instant and reliable)
        if (await FetchAndSaveProfileViaHttpAsync(sessionService))
            return true;

        if (webView == null) return false;

        // 2. Fallback to synchronous DOM extraction inside WebView
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
