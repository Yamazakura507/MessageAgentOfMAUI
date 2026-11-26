using LottieAgent.Interfaces;

namespace MessageAgent.Services;

internal sealed class RawLottieJsonLoader(string json) : ILottieJsonLoader
{
    public ValueTask<string> LoadAsync(string assetPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(json);
    }
}