using Serilog;

namespace SKRYBEK.Services.Logging;

public static class SkrybekLog
{
    public static void Initialize(string logPath)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    public static void Info(string msg) => Log.Information(msg);
    public static void Warning(string msg) => Log.Warning(msg);
    public static void Error(string msg, Exception? ex = null) => Log.Error(ex, msg);

    public static void Close() => Log.CloseAndFlush();
}
