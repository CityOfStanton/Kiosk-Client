using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace KioskClient.Core.Logging;

/// <summary>
/// Application debug log. Writes asynchronously to one file per day named
/// <c>KioskClient-YYYYMMDD.log</c>, capped at 10 MB per file, keeping the last 30 days.
/// All text is scrubbed of identifying information (URLs, hosts, IP addresses, file paths,
/// e-mail addresses, machine and user names) before it is written.
/// Logging never throws: failures to log are swallowed so they cannot affect the app.
/// </summary>
public static partial class AppLog
{
    public const string FilePrefix = "KioskClient-";
    public const string FileExtension = ".log";
    public const long MaxFileSizeBytes = 10L * 1024 * 1024;
    public const int RetainedDays = 30;

    private const string DateFormat = "yyyyMMdd";
    private const int MaxExceptionDepth = 5;

    private static readonly object InitLock = new();
    private static Logger? _logger;

    /// <summary>The folder the log files are written to, or null if logging is not initialized.</summary>
    public static string? LogFolder { get; private set; }

    /// <summary>
    /// Starts logging to the given folder. Safe to call more than once; only the first call has an effect.
    /// </summary>
    public static void Initialize(string folder)
    {
        lock (InitLock)
        {
            if (_logger is not null) return;

            try
            {
                Directory.CreateDirectory(folder);
                PurgeOldFiles(folder);

                _logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Async(sink => sink.File(
                        path: Path.Combine(folder, FilePrefix + FileExtension),
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}",
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: MaxFileSizeBytes,
                        rollOnFileSizeLimit: false,
                        retainedFileCountLimit: RetainedDays))
                    .CreateLogger();

                LogFolder = folder;
            }
            catch
            {
                _logger = null;
            }
        }
    }

    /// <summary>Flushes pending entries and stops logging. Call on application exit.</summary>
    public static void Shutdown()
    {
        lock (InitLock)
        {
            try { _logger?.Dispose(); }
            catch { /* nothing useful to do while shutting down */ }
            _logger = null;
        }
    }

    public static void Debug(string category, string message) => Write(LogEventLevel.Debug, category, message, null);

    public static void Info(string category, string message) => Write(LogEventLevel.Information, category, message, null);

    public static void Warn(string category, string message, Exception? exception = null) => Write(LogEventLevel.Warning, category, message, exception);

    public static void Error(string category, string message, Exception? exception = null) => Write(LogEventLevel.Error, category, message, exception);

    /// <summary>
    /// Describes a URL or file path without revealing it: kind, file extension and a short
    /// one-way identifier that lets entries for the same target be correlated.
    /// </summary>
    public static string Describe(string? target)
    {
        if (string.IsNullOrWhiteSpace(target)) return "(empty)";

        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(target)), 0, 4);

        if (Uri.TryCreate(target, UriKind.Absolute, out var uri) && !uri.IsFile)
            return $"{uri.Scheme}-url ext={SafeExtension(uri.AbsolutePath)} id={id}";

        return $"file ext={SafeExtension(target)} id={id}";
    }

    /// <summary>Removes identifying information from free text.</summary>
    public static string Scrub(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var result = UrlRegex().Replace(text, m => m.Groups["scheme"].Value + "://[redacted]");
        result = UncPathRegex().Replace(result, "[path]");
        result = DrivePathRegex().Replace(result, "[path]");
        result = EmailRegex().Replace(result, "[email]");
        result = Ipv4Regex().Replace(result, "[ip]");
        result = Ipv6Regex().Replace(result, "[ip]");
        result = HostPortRegex().Replace(result, "[host]");

        result = ReplaceLiteral(result, Environment.UserName, "[user]");
        result = ReplaceLiteral(result, Environment.MachineName, "[machine]");
        return result;
    }

    private static void Write(LogEventLevel level, string category, string message, Exception? exception)
    {
        try
        {
            var logger = _logger;
            if (logger is null || !logger.IsEnabled(level)) return;

            var text = Scrub(message);
            if (exception is not null)
                text += Environment.NewLine + FormatException(exception, 0);

            logger.Write(level, "[{Category}] {Message:l}", category, text);
        }
        catch
        {
            // Logging must never break the application.
        }
    }

    private static string FormatException(Exception exception, int depth)
    {
        var sb = new StringBuilder();
        sb.Append(exception.GetType().FullName)
          .Append(": ").Append(Scrub(exception.Message))
          .Append(" (HResult=0x").Append(exception.HResult.ToString("X8", CultureInfo.InvariantCulture)).Append(')');

        if (!string.IsNullOrEmpty(exception.StackTrace))
            sb.AppendLine().Append(ScrubStackTrace(exception.StackTrace));

        if (depth < MaxExceptionDepth)
        {
            if (exception is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                    sb.AppendLine().Append("  ---> ").Append(FormatException(inner, depth + 1));
            }
            else if (exception.InnerException is { } inner)
            {
                sb.AppendLine().Append("  ---> ").Append(FormatException(inner, depth + 1));
            }
        }

        return sb.ToString();
    }

    /// <summary>Keeps method names and source file names/line numbers but drops directory paths.</summary>
    private static string ScrubStackTrace(string stackTrace)
    {
        var result = StackFrameFileRegex().Replace(stackTrace, m =>
            " in " + m.Groups["path"].Value.Split('\\', '/')[^1] + ":line " + m.Groups["line"].Value);
        return Scrub(result);
    }

    private static string SafeExtension(string path)
    {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext) || ext.Length > 6) return "(none)";
        foreach (var c in ext.AsSpan(1))
            if (!char.IsAsciiLetterOrDigit(c)) return "(none)";
        return ext.ToLowerInvariant();
    }

    private static string ReplaceLiteral(string text, string value, string replacement) =>
        value.Length < 3 ? text : text.Replace(value, replacement, StringComparison.OrdinalIgnoreCase);

    private static void PurgeOldFiles(string folder)
    {
        try
        {
            var oldestToKeep = DateOnly.FromDateTime(DateTime.Now).AddDays(-(RetainedDays - 1));

            foreach (var file in Directory.EnumerateFiles(folder, FilePrefix + "*" + FileExtension))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (name.Length < FilePrefix.Length + DateFormat.Length) continue;

                var datePart = name.Substring(FilePrefix.Length, DateFormat.Length);
                if (DateOnly.TryParseExact(datePart, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                    date < oldestToKeep)
                {
                    try { File.Delete(file); }
                    catch { /* in use or locked; try again next start */ }
                }
            }
        }
        catch
        {
            // Retention is best effort.
        }
    }

    [GeneratedRegex(@"(?<scheme>[a-z][a-z0-9+.\-]*)://[^\s'""<>)\]]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\\\\[^\s\\'""<>|*?]+(?:\\[^\r\n'""<>|*?]*)?", RegexOptions.CultureInvariant)]
    private static partial Regex UncPathRegex();

    [GeneratedRegex(@"[a-z]:[\\/][^\r\n'""<>|*?]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DrivePathRegex();

    [GeneratedRegex(@"[\w.+\-]+@[\w\-]+(?:\.[\w\-]+)+", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.CultureInvariant)]
    private static partial Regex Ipv4Regex();

    [GeneratedRegex(@"\b(?:[0-9a-f]{1,4}:){2,7}[0-9a-f]{1,4}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Ipv6Regex();

    [GeneratedRegex(@"\b(?:[a-z0-9\-]+\.)+[a-z]{2,}:\d{1,5}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HostPortRegex();

    [GeneratedRegex(@" in (?<path>[^\r\n]+?):line (?<line>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex StackFrameFileRegex();
}
