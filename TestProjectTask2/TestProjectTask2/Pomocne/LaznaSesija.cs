using Microsoft.AspNetCore.Http;

namespace TestProjectTask2.Pomocne;

/// <summary>
/// Fake ISession: sesija u memoriji (Dictionary) umjesto prave ASP.NET Core sesije.
/// Koristi se kao: httpContext.Session = new LaznaSesija();
/// </summary>
public class LaznaSesija : ISession
{
    private readonly Dictionary<string, byte[]> _podaci = new();

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _podaci.Keys;

    public void Clear() => _podaci.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _podaci.Remove(key);
    public void Set(string key, byte[] value) => _podaci[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _podaci.TryGetValue(key, out value!);
}
