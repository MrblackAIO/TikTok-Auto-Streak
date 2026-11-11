import AppIntents
import StreakEngine

/// Widget extension entry point. Queues a run for the main app (WKWebView cannot run in the extension).
public struct MaintainStreakIntent: AppIntent {
    public static var title: LocalizedStringResource = "Maintain TikTok Streaks"
    public static var description = IntentDescription(
        "Opens Streak Saver and sends streak messages to your enabled friends. Sign in inside the app first so your TikTok session is available.")
    /// Required: WKWebView cannot run inside the WidgetKit extension sandbox.
    public static var openAppWhenRun: Bool = true

    public init() {}

    public func perform() async throws -> some IntentResult {
        PendingStreakRun.request()
        return .result()
    }
}
