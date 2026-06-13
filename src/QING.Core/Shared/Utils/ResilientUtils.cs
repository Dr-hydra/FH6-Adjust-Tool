namespace QING.Core;
public static class ResilientUtils {

    /// <summary>
    /// 在抛出任意异常时，延迟并自动重试。
    /// </summary>
    public static void Retry(Action action, int maxAttempts = 2, int delayMs = 200, [CallerMemberName] string caller = "")
        => RetryOn<Exception>(action, maxAttempts, delayMs, caller);
    /// <summary>
    /// 在抛出特定异常时，延迟并自动重试。
    /// </summary>
    public static void RetryOn<TException>(Action action, int maxAttempts = 2, int delayMs = 200, [CallerMemberName] string caller = "") where TException : Exception 
        => RetryOn<TException, object?>(() => { action(); return null; }, maxAttempts, delayMs, caller);
    /// <summary>
    /// 在抛出特定异常时，延迟并自动重试。
    /// </summary>
    public static void RetryOn<TException1, TException2>(Action action, int maxAttempts = 2, int delayMs = 200, [CallerMemberName] string caller = "") where TException1 : Exception where TException2 : Exception
        => RetryOn<TException1, TException2, object?>(() => { action(); return null; }, maxAttempts, delayMs, caller);

    /// <summary>
    /// 在抛出任意异常时，延迟并自动重试。
    /// </summary>
    public static void Retry<TOut>(Func<TOut> func, int maxAttempts = 2, int delayMs = 200, [CallerMemberName] string caller = "")
        => RetryOn<Exception, TOut>(func, maxAttempts, delayMs, caller);
    /// <summary>
    /// 在抛出特定异常时，延迟并自动重试。
    /// </summary>
    public static TOut RetryOn<TException, TOut>(Func<TOut> func, int maxAttempts = 2, int delayMs = 200, [CallerMemberName] string caller = "") where TException : Exception {
        int attempt = 0;
        while (true) {
            try {
                return func(); // 成功则退出
            } catch (TException ex) {
                attempt++;
                if (attempt < maxAttempts) {
                    Logger.Warn(ex, $"第 {attempt} 次 {caller} 尝试失败，将在 {delayMs}ms 后重试");
                    Thread.Sleep(delayMs);
                } else { // 超过最大尝试次数
                    Logger.Warn(ex, $"第 {attempt} 次 {caller} 尝试失败，已到达最大尝试次数");
                    throw;
                }
            }
        }
    }
    /// <summary>
    /// 在抛出特定异常时，延迟并自动重试。
    /// </summary>
    public static TOut RetryOn<TException1, TException2, TOut>(Func<TOut> func, int maxAttempts = 2, int delayMs = 200, [CallerMemberName] string caller = "") where TException1 : Exception where TException2 : Exception {
        int attempt = 0;
        while (true) {
            try {
                return func(); // 成功则退出
            } catch (Exception ex) when (ex is TException1 or TException2) {
                attempt++;
                if (attempt < maxAttempts) {
                    Logger.Warn(ex, $"第 {attempt} 次 {caller} 尝试失败，将在 {delayMs}ms 后重试");
                    Thread.Sleep(delayMs);
                } else { // 超过最大尝试次数
                    Logger.Warn(ex, $"第 {attempt} 次 {caller} 尝试失败，已到达最大尝试次数");
                    throw;
                }
            }
        }
    }

}
