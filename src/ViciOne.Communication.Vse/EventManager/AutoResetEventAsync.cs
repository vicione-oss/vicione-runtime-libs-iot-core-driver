namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// An async-friendly auto-reset event. Unlike <see cref="AutoResetEvent"/>, multiple calls to
/// <see cref="Set"/> that arrive before anyone waits still only wake a single waiter once - the
/// <c>_signal</c> flag coalesces them instead of queuing up releases.
/// </summary>
internal sealed class AutoResetEventAsync : IDisposable
{
    private readonly SemaphoreSlim _mutex = new(0, 1);
    private int _signal;

    public void Dispose() => _mutex.Dispose();

    public void Set()
    {
        if (Interlocked.CompareExchange(ref _signal, 1, 0) == 0)
            _mutex.Release();
    }

    public async Task<bool> WaitAsync(int milliSecondsTimeout)
    {
        try
        {
            return await _mutex.WaitAsync(milliSecondsTimeout).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _signal, 0);
        }
    }
}
