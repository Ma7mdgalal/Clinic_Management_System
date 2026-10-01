using Microsoft.Extensions.Logging;

namespace Clinic.Tests.TestDoubles;

/// <summary>
/// Captures everything a service logs so a test can assert on it.
///
/// Two collections, because structured logging has two surfaces and a leak can hide in
/// either one: <see cref="Messages"/> holds the rendered text, and <see cref="Values"/>
/// holds the named placeholders that a provider such as Serilog stores as queryable
/// fields. Asserting only on the rendered text would miss a value that reaches the log
/// store but never the console.
/// </summary>
public sealed class RecordingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public List<KeyValuePair<string, object?>> Values { get; } = [];

    /// <summary>Everything that was logged, in one string, for "must not contain" assertions.</summary>
    public string AllText =>
        string.Join(
            " | ",
            Messages.Concat(Values.Select(v => $"{v.Key}={v.Value}")));

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));

        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            // The "{OriginalFormat}" entry is the template itself, not data. Keeping it
            // would make every assertion match on placeholder names like "{Email}".
            Values.AddRange(pairs.Where(p => p.Key != "{OriginalFormat}"));
        }
    }
}
