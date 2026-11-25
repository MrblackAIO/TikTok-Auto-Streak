using System.Text.Json;
using TiktokStreakSaver.Models;
using TiktokStreakSaver.Services.Storage;

namespace TiktokStreakSaver.Services;

/// <summary>
/// Service for managing app settings and persistent storage
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class SettingsService
{
    private readonly IAppStorage _storage = AppStorageProvider.Current;

    private const string FriendsListKey = "friends_list";
    private const string MessageTextKey = "message_text";
    private const string CustomFriendMessagesKey = "custom_friend_messages";
    private const string LastRunKey = "last_run";
    private const string IsScheduledKey = "is_scheduled";
    private const string RunHistoryKey = "run_history";
    private const string IntervalHoursKey = "interval_hours";
    private const string SkipUnreachableUsersKey = "skip_unreachable_users";
    private const string UseFixedTimeKey = "use_fixed_time";
    private const string FixedTimeHourKey = "fixed_time_hour";
    private const string FixedTimeMinuteKey = "fixed_time_minute";
    private const string ChatPriorityKey = "chat_priority";

    // ── Resilience: retry tracking + event-driven recovery flags ──
    private const string RetryCountTodayKey = "retry_count_today";
    private const string RetryCountDateKey = "retry_count_date";
    private const string LastRunFailedKey = "last_run_failed";
    private const string LastRunFailureReasonKey = "last_run_failure_reason";
    private const string BatteryAnticipationDateKey = "battery_anticipation_date";
    private const string SendOnBatteryLowKey = "send_on_battery_low";
    private const string AutoStartConfiguredKey = "auto_start_configured";

    /// <summary>
    /// Maximum hourly retries per calendar day, in addition to the original scheduled run.
    /// </summary>
    public const int MaxRetriesPerDay = 3;

    /// <summary>Failure-reason value: the run was skipped because there was no Wi‑Fi/cellular.</summary>
    public const string FailureReasonNoNetwork = "no_network";

    /// <summary>Failure-reason value: the run completed but one or more messages failed to send.</summary>
    public const string FailureReasonSendError = "send_error";

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(AppJsonSerialization.Settings)
        {
            TypeInfoResolver = AppJsonContext.Default
        };
        return options;
    }

    /// <summary>
    /// Default message to send.
    /// </summary>
    public const string DefaultMessage = "Let's keep the streak ablaze! \uD83D\uDD25";

    /// <summary>
    /// Default interval in hours.
    /// </summary>
    public const int DefaultIntervalHours = 23;
    public const string ChatPriorityMixed = "mixed";
    public const string ChatPriorityFriendsFirst = "friends_first";
    public const string ChatPriorityGroupsFirst = "groups_first";

    #region Friends List

    private string ReadFriendsListJson()
    {
#if IOS
        var fromFile = Platforms.iOS.Services.IosFriendsFileStorage.ReadJson(migrateFromDefaults: true);
        if (!string.IsNullOrWhiteSpace(fromFile))
            return fromFile;
#endif
        return _storage.GetString(FriendsListKey, string.Empty);
    }

    private bool TryDeserializeFriends(string json, out List<FriendConfig>? friends)
    {
        friends = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            friends = new List<FriendConfig>();
            return true;
        }

        try
        {
            friends = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListFriendConfig);
            if (friends != null)
                return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TryDeserializeFriends source-gen failed: {ex.Message}");
        }

        try
        {
            friends = JsonSerializer.Deserialize<List<FriendConfig>>(json, JsonOptions);
            return friends != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TryDeserializeFriends fallback failed: {ex.Message}");
            friends = null;
            return false;
        }
    }

    private bool TryLoadFriendsList(out List<FriendConfig>? friends, out string? error)
    {
        friends = null;
        error = null;

        var json = ReadFriendsListJson();
        if (string.IsNullOrWhiteSpace(json))
        {
            friends = new List<FriendConfig>();
            return true;
        }

        if (TryDeserializeFriends(json, out friends))
            return true;

#if IOS
        TryRecoverFriendsListFromStandardDefaults();
        json = ReadFriendsListJson();
        if (!string.IsNullOrWhiteSpace(json) && TryDeserializeFriends(json, out friends))
            return true;

        json = _storage.GetString(FriendsListKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(json) && TryDeserializeFriends(json, out friends))
        {
            Platforms.iOS.Services.IosFriendsFileStorage.WriteJson(json);
            return true;
        }
#endif

        error = "Could not read saved friends list.";
        return false;
    }

    public List<FriendConfig> GetFriendsList()
    {
        if (TryLoadFriendsList(out var friends, out _))
            return friends ?? new List<FriendConfig>();

        return new List<FriendConfig>();
    }

    public bool TrySaveFriendsList(List<FriendConfig> friends, out string? error)
    {
        error = null;
        var json = JsonSerializer.Serialize(friends, AppJsonContext.Default.ListFriendConfig);

        if (!TryDeserializeFriends(json, out _))
        {
            error = "Could not serialize friends list.";
            return false;
        }

#if IOS
        if (!Platforms.iOS.Services.IosFriendsFileStorage.WriteJson(json))
        {
            error = "Could not write friends file.";
            return false;
        }
#endif

        _storage.SetString(FriendsListKey, json);

        var readBack = ReadFriendsListJson();
        if (string.IsNullOrWhiteSpace(readBack) || !TryDeserializeFriends(readBack, out var parsed) || parsed == null)
        {
            error = "Friends list did not persist on this device.";
            return false;
        }

        return true;
    }

    public void SaveFriendsList(List<FriendConfig> friends)
    {
        TrySaveFriendsList(friends, out _);
    }

#if IOS
    private void TryRecoverFriendsListFromStandardDefaults()
    {
        Platforms.iOS.Services.AppGroupAppStorage.TryCopyNonEmptyStringFromStandard(
            FriendsListKey, _storage);
    }
#endif

    public bool TryAddFriend(FriendConfig friend, out string? error)
    {
        if (!TryLoadFriendsList(out var friends, out error) || friends == null)
            friends = new List<FriendConfig>();

        friends.Add(friend);
        return TrySaveFriendsList(friends, out error);
    }

    public void AddFriend(FriendConfig friend)
    {
        TryAddFriend(friend, out _);
    }

    public void RemoveFriend(string friendId)
    {
        var friends = GetFriendsList();
        friends.RemoveAll(f => f.Id == friendId);
        SaveFriendsList(friends);
        ResetCustomMessageForRecipient(friendId);
    }

    public void UpdateFriend(FriendConfig friend)
    {
        var friends = GetFriendsList();
        var index = friends.FindIndex(f => f.Id == friend.Id);
        if (index >= 0)
        {
            friends[index] = friend;
            SaveFriendsList(friends);
        }
    }

    public List<FriendConfig> GetEnabledFriends()
    {
        return GetFriendsList().Where(f => f.IsEnabled).ToList();
    }

    #endregion

    #region Message Configuration

    public string GetMessageText()
    {
        var fromFile = TryReadMessageTextFile();
        if (fromFile != null)
            return fromFile;

        var stored = _storage.GetString(MessageTextKey, string.Empty);
        return string.IsNullOrEmpty(stored) ? DefaultMessage : stored;
    }

    public void SetMessageText(string message)
    {
        var text = message ?? string.Empty;
        _storage.SetString(MessageTextKey, text);
        TryWriteMessageTextFile(text);
    }

    private static string SharedStorageDirectory
    {
        get
        {
#if IOS
            return Platforms.iOS.Services.AppGroupPaths.SharedStorageDirectory;
#else
            return FileSystem.AppDataDirectory;
#endif
        }
    }

    private static string MessageTextFilePath =>
        Path.Combine(SharedStorageDirectory, AppConstants.MessageTextFileName);

    private static string CustomFriendMessagesFilePath =>
        Path.Combine(SharedStorageDirectory, AppConstants.CustomFriendMessagesFileName);

    private static string RandomizeMessagesFlagFilePath =>
        Path.Combine(SharedStorageDirectory, AppConstants.RandomizeMessagesFlagFileName);

    // ── Per-Recipient Custom Message Overrides ──

    private string ReadCustomFriendMessagesJson()
    {
        var fromFile = TryReadTextFile(CustomFriendMessagesFilePath);
        if (!string.IsNullOrWhiteSpace(fromFile))
            return fromFile;

        return _storage.GetString(CustomFriendMessagesKey, string.Empty);
    }

    private static string? TryReadTextFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            return File.ReadAllText(path);
        }
        catch
        {
            return null;
        }
    }

    public Dictionary<string, string> GetCustomFriendMessages()
    {
        var json = ReadCustomFriendMessagesJson();
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>(StringComparer.Ordinal);

        try
        {
            var parsed = JsonSerializer.Deserialize(json, AppJsonContext.Default.DictionaryStringString);
            if (parsed != null)
                return new Dictionary<string, string>(parsed, StringComparer.Ordinal);
        }
        catch
        {
            try
            {
                var fallback = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
                if (fallback != null)
                    return new Dictionary<string, string>(fallback, StringComparer.Ordinal);
            }
            catch
            {
                // Corrupted customization JSON must not crash automation or settings.
            }
        }

        return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private void SaveCustomFriendMessages(Dictionary<string, string> map)
    {
        var sanitized = map
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

        var json = JsonSerializer.Serialize(sanitized, AppJsonContext.Default.DictionaryStringString);
        _storage.SetString(CustomFriendMessagesKey, json);
        TryWriteTextFile(CustomFriendMessagesFilePath, json);
    }

    public string? GetCustomMessageForRecipient(string? friendId)
    {
        if (string.IsNullOrWhiteSpace(friendId))
            return null;

        var map = GetCustomFriendMessages();
        return map.TryGetValue(friendId, out var message) && !string.IsNullOrWhiteSpace(message)
            ? message
            : null;
    }

    public void SetCustomMessageForRecipient(string friendId, string? message)
    {
        if (string.IsNullOrWhiteSpace(friendId))
            return;

        var map = GetCustomFriendMessages();
        if (string.IsNullOrWhiteSpace(message))
        {
            if (!map.Remove(friendId))
                return;
        }
        else
        {
            map[friendId] = message;
        }

        SaveCustomFriendMessages(map);
    }

    public void ResetCustomMessageForRecipient(string friendId) =>
        SetCustomMessageForRecipient(friendId, null);

    public int GetCustomizedRecipientCount(IEnumerable<FriendConfig> recipients)
    {
        var map = GetCustomFriendMessages();
        if (map.Count == 0)
            return 0;

        return recipients
            .Where(r => !string.IsNullOrWhiteSpace(r.Id))
            .Select(r => r.Id)
            .Distinct(StringComparer.Ordinal)
            .Count(id => map.TryGetValue(id, out var msg) && !string.IsNullOrWhiteSpace(msg));
    }

    /// <summary>
    /// Resolves the message to send for a recipient when randomizer is OFF:
    /// 1. Valid custom message for recipient.Id if configured.
    /// 2. Existing default Automation Text otherwise.
    /// </summary>
    public string ResolveMessageForRecipient(FriendConfig? friend)
    {
        var custom = GetCustomMessageForRecipient(friend?.Id);
        if (!string.IsNullOrWhiteSpace(custom))
            return custom;

        return GetMessageText();
    }

    private static string? TryReadMessageTextFile()
    {
        try
        {
            if (!File.Exists(MessageTextFilePath))
                return null;

            return File.ReadAllText(MessageTextFilePath);
        }
        catch
        {
            return null;
        }
    }

    private static void TryWriteMessageTextFile(string text)
    {
        TryWriteTextFile(MessageTextFilePath, text);
    }

    private static void TryWriteTextFile(string path, string text)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + ".tmp";
            File.WriteAllText(temp, text);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }
        catch
        {
            // UserDefaults / Preferences mirror remains the fallback.
        }
    }

    private static bool? TryReadBoolFlagFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            var text = File.ReadAllText(path).Trim();
            if (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1")
                return true;
            if (text.Equals("false", StringComparison.OrdinalIgnoreCase) || text == "0")
                return false;

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static void TryWriteBoolFlagFile(string path, bool value) =>
        TryWriteTextFile(path, value ? "true" : "false");

    // ── Randomized Normal Messages ──

    private const string RandomizeNormalMessagesKey = "randomize_normal_messages";

    /// <summary>
    /// 50 built-in short streak messages for randomized normal-mode sends.
    /// </summary>
    public static readonly List<string> BuiltInStreakMessages = new()
    {
        "Streak",
        "streak",
        "streakk",
        "streaak",
        "streaakkk",
        "streaaak",
        "strk",
        "Strk",
        "s",
        "S",
        "streaks",
        "Streaks",
        "streakss",
        "streak lol",
        "yo streak",
        "yoo streak",
        "yoo streakk",
        "hey streak",
        "hii streak",
        "hi streak",
        "heyy streak",
        "streak hii",
        "streak hi",
        "streak yo",
        "streak yoo",
        "streakkk",
        "strek",
        "streek",
        "streeek",
        "streeeek",
        "yo",
        "yoo",
        "yooo",
        "hey",
        "hii",
        "heyy",
        "heyyy",
        "here",
        "heree",
        "strkeee",
        "streak rn",
        "quick streak",
        "streakk lol",
        "streak lmao",
        "lol streak",
        "streaaakk",
        "streakkk lol",
        "daily streak",
        "streak streak",
        "ayo streak"
    };

    /// <summary>
    /// Get whether randomized built-in messages are enabled for normal mode
    /// </summary>
    public bool GetRandomizeNormalMessages()
    {
        var fromFile = TryReadBoolFlagFile(RandomizeMessagesFlagFilePath);
        if (fromFile.HasValue)
            return fromFile.Value;

        return _storage.GetBool(RandomizeNormalMessagesKey, false);
    }

    /// <summary>
    /// Set whether randomized built-in messages are enabled for normal mode
    /// </summary>
    public void SetRandomizeNormalMessages(bool enabled)
    {
        _storage.SetBool(RandomizeNormalMessagesKey, enabled);
        TryWriteBoolFlagFile(RandomizeMessagesFlagFilePath, enabled);
    }

    #endregion

    #region Scheduling

    /// <summary>
    /// Interval between automatic runs, in hours (clamped to 1..23).
    /// </summary>
    public int GetIntervalHours()
    {
        var v = (int)_storage.GetLong(IntervalHoursKey, DefaultIntervalHours);
        return Math.Clamp(v, 1, 23);
    }

    /// <summary>
    /// Set the interval in hours (clamped to 1..23).
    /// </summary>
    public void SetIntervalHours(int hours)
    {
        _storage.SetLong(IntervalHoursKey, Math.Clamp(hours, 1, 23));
    }

    public DateTime? GetLastRunTime()
    {
        var ticks = _storage.GetLong(LastRunKey, 0L);
        if (ticks <= 0)
            return null;

#if IOS
        return AppTime.ToAppTime(new DateTime(ticks, DateTimeKind.Utc));
#else
        return new DateTime(ticks);
#endif
    }

    public void SetLastRunTime(DateTime time)
    {
        _storage.SetLong(LastRunKey, time.Ticks);
    }

    /// <summary>
    /// Default off (matches fork). Users opt in via the Profile toggle.
    /// </summary>
    public bool IsScheduled()
    {
        return _storage.GetBool(IsScheduledKey, false);
    }

    /// <summary>
    /// Whether automatic streak runs are expected. On iOS this means a Shortcuts automation
    /// (onboarding complete); on Android it follows the in-app schedule toggle.
    /// </summary>
    public bool IsAutomationActive()
    {
#if IOS
        if (IsScheduled())
            return true;

        return _storage.GetBool(AppConstants.IosOnboardingCompleteKey, false);
#else
        return IsScheduled();
#endif
    }

    public void SetScheduled(bool scheduled)
    {
        _storage.SetBool(IsScheduledKey, scheduled);
    }

    /// <summary>
    /// Default ON (kept from original): a missing friend should not abort the whole run.
    /// </summary>
    public bool GetSkipUnreachableUsers()
    {
        return _storage.GetBool(SkipUnreachableUsersKey, true);
    }

    public void SetSkipUnreachableUsers(bool skip)
    {
        _storage.SetBool(SkipUnreachableUsersKey, skip);
    }

    public string GetChatPriority()
    {
        var value = _storage.GetString(ChatPriorityKey, ChatPriorityMixed);
        return value is ChatPriorityFriendsFirst or ChatPriorityGroupsFirst
            ? value
            : ChatPriorityMixed;
    }

    public void SetChatPriority(string value)
    {
        var normalized = value is ChatPriorityFriendsFirst or ChatPriorityGroupsFirst
            ? value
            : ChatPriorityMixed;
        _storage.SetString(ChatPriorityKey, normalized);
    }

    public DateTime GetNextRunTime()
    {
        if (GetUseFixedTime())
        {
            var now = AppTime.Now;
            var today = now.Date.AddHours(GetFixedTimeHour()).AddMinutes(GetFixedTimeMinute());
            return today > now ? today : today.AddDays(1);
        }

        var lastRun = GetLastRunTime();
        var intervalHours = GetIntervalHours();

        if (lastRun.HasValue)
            return lastRun.Value.AddHours(intervalHours);

        return AppTime.Now;
    }

    /// <summary>
    /// When true, scheduling uses a fixed daily clock time (Hour:Minute) instead of
    /// the rolling interval based on last run.
    /// </summary>
    public bool GetUseFixedTime() => _storage.GetBool(UseFixedTimeKey, true);

    public void SetUseFixedTime(bool value) => _storage.SetBool(UseFixedTimeKey, value);

    /// <summary>
    /// Hour of day (0-23) for fixed daily schedule.
    /// </summary>
    public int GetFixedTimeHour()
    {
        var stored = _storage.GetLong(FixedTimeHourKey, -1);
        return stored >= 0 ? (int)stored : AppTime.Now.Hour;
    }

    public void SetFixedTimeHour(int hour) => _storage.SetLong(FixedTimeHourKey, Math.Clamp(hour, 0, 23));

    /// <summary>
    /// Minute (0-59) for fixed daily schedule.
    /// </summary>
    public int GetFixedTimeMinute()
    {
        var stored = _storage.GetLong(FixedTimeMinuteKey, -1);
        return stored >= 0 ? (int)stored : AppTime.Now.Minute;
    }

    public void SetFixedTimeMinute(int minute) => _storage.SetLong(FixedTimeMinuteKey, Math.Clamp(minute, 0, 59));

    #endregion

    #region Resilience: retry counter, failure flag, battery toggle

    /// <summary>
    /// Current count of hourly retries used today. Auto-resets when the stored date
    /// stamp is not today's date (so the counter naturally rolls over at midnight).
    /// </summary>
    public int GetTodayRetryCount()
    {
        var storedDateTicks = _storage.GetLong(RetryCountDateKey, 0L);
        var today = AppTime.Today.Ticks;
        if (storedDateTicks != today)
            return 0;
        return (int)_storage.GetLong(RetryCountTodayKey, 0);
    }

    /// <summary>
    /// Increments today's retry counter (rolling the date stamp if it's a new day)
    /// and returns the new value.
    /// </summary>
    public int IncrementTodayRetryCount()
    {
        var today = AppTime.Today.Ticks;
        var storedDateTicks = _storage.GetLong(RetryCountDateKey, 0L);
        var current = storedDateTicks == today ? (int)_storage.GetLong(RetryCountTodayKey, 0) : 0;
        var next = current + 1;
        _storage.SetLong(RetryCountDateKey, today);
        _storage.SetLong(RetryCountTodayKey, next);
        return next;
    }

    /// <summary>Reset today's retry counter (typically on a fully successful run).</summary>
    public void ResetTodayRetryCount()
    {
        _storage.SetLong(RetryCountDateKey, AppTime.Today.Ticks);
        _storage.SetLong(RetryCountTodayKey, 0);
    }

    /// <summary>
    /// Flag indicating that the last attempted run failed and a recovery (e.g. on
    /// network restore) may be worth attempting.
    /// </summary>
    public bool GetLastRunFailed() => _storage.GetBool(LastRunFailedKey, false);

    /// <summary>Set / clear the last-run-failed flag, optionally tagging the reason.</summary>
    public void SetLastRunFailed(bool failed, string? reason)
    {
        _storage.SetBool(LastRunFailedKey, failed);
        if (failed && !string.IsNullOrEmpty(reason))
            _storage.SetString(LastRunFailureReasonKey, reason);
        else
            _storage.Remove(LastRunFailureReasonKey);
    }

    /// <summary>Reason tag set the last time the run failed, or null when cleared.</summary>
    public string? GetLastRunFailureReason()
    {
        var v = _storage.GetString(LastRunFailureReasonKey, string.Empty);
        return string.IsNullOrEmpty(v) ? null : v;
    }

    /// <summary>
    /// True if the battery-low anticipation already fired today (we only fire it once
    /// per calendar day to avoid loops if the device keeps oscillating around 15%).
    /// </summary>
    public bool WasBatteryAnticipationUsedToday()
    {
        var storedTicks = _storage.GetLong(BatteryAnticipationDateKey, 0L);
        return storedTicks == AppTime.Today.Ticks;
    }

    /// <summary>Mark today as already used for battery-low anticipation.</summary>
    public void MarkBatteryAnticipationUsedToday()
    {
        _storage.SetLong(BatteryAnticipationDateKey, AppTime.Today.Ticks);
    }

    /// <summary>
    /// User toggle: when ON, the app will start the streak run early if Android fires
    /// the system battery-low broadcast and today's streak hasn't been sent yet.
    /// Default ON.
    /// </summary>
    public bool GetSendOnBatteryLow() => _storage.GetBool(SendOnBatteryLowKey, true);

    public void SetSendOnBatteryLow(bool value) => _storage.SetBool(SendOnBatteryLowKey, value);

    public bool GetAutoStartConfigured() => _storage.GetBool(AutoStartConfiguredKey, false);

    public void SetAutoStartConfigured(bool value) => _storage.SetBool(AutoStartConfiguredKey, value);

    #endregion

    #region Run History

    public List<StreakRunResult> GetRunHistory()
    {
#if IOS
        var fromFile = Platforms.iOS.Services.IosRunHistoryFileStorage.ReadHistory();
        if (fromFile.Count > 0)
            return fromFile;
#endif

        var json = _storage.GetString(RunHistoryKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return new List<StreakRunResult>();

        try
        {
            return JsonSerializer.Deserialize(json, AppJsonContext.Default.ListStreakRunResult)
                ?? new List<StreakRunResult>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetRunHistory deserialize failed: {ex.Message}");
#if IOS
            Platforms.iOS.Services.IosRunTrace.Append($"GetRunHistory deserialize failed: {ex.Message}");
#endif
            return new List<StreakRunResult>();
        }
    }

    public void AddRunResult(StreakRunResult result)
    {
        var history = GetRunHistory();
        history.Insert(0, result);

        if (history.Count > 50)
            history = history.Take(50).ToList();

        var json = JsonSerializer.Serialize(history, AppJsonContext.Default.ListStreakRunResult);
        _storage.SetString(RunHistoryKey, json);
#if IOS
        Platforms.iOS.Services.IosRunHistoryFileStorage.WriteHistory(history);
#endif
    }

    #endregion

    #region Clear Data

    public void ClearRunHistory()
    {
        _storage.Remove(RunHistoryKey);
#if IOS
        Platforms.iOS.Services.IosRunHistoryFileStorage.Clear();
#endif
    }

    public void ClearAll()
    {
        _storage.Clear();
        try
        {
            if (File.Exists(CustomFriendMessagesFilePath))
                File.Delete(CustomFriendMessagesFilePath);
        }
        catch { }
    }

    #endregion
}
