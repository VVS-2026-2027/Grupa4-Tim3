using Microsoft.AspNetCore.Http;

namespace ParkirajBa.Tests.Helpers;

public sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _values = new();

    public IEnumerable<string> Keys => _values.Keys;

    public string Id { get; } = Guid.NewGuid().ToString();

    public bool IsAvailable => true;

    public int? IdleTimeout { get; set; }

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Clear() => _values.Clear();

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        Clear();
        return Task.CompletedTask;
    }

    public void Remove(string key) => _values.Remove(key);

    public void Set(string key, byte[] value) => _values[key] = value;

    public bool TryGetValue(string key, out byte[]? value) => _values.TryGetValue(key, out value);

    public string? GetString(string key) =>
        TryGetValue(key, out var value) ? System.Text.Encoding.UTF8.GetString(value) : null;
}
