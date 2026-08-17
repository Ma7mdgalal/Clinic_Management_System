using Serilog;

namespace Clinic.Application.Logging;

public static class AppLogger
{
    public static void Information(string messageTemplate, params object?[] propertyValues) =>
        Log.Information(messageTemplate, propertyValues);

    public static void Warning(string messageTemplate, params object?[] propertyValues) =>
        Log.Warning(messageTemplate, propertyValues);

    public static void Error(Exception exception, string messageTemplate, params object?[] propertyValues) =>
        Log.Error(exception, messageTemplate, propertyValues);

    public static void Debug(string messageTemplate, params object?[] propertyValues) =>
        Log.Debug(messageTemplate, propertyValues);

    public static void Fatal(Exception exception, string messageTemplate, params object?[] propertyValues) =>
        Log.Fatal(exception, messageTemplate, propertyValues);
}
