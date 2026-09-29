using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Data;

/// <summary>
/// Decorator for another <see cref="IGameDataSource"/> that first downloads the data file from
/// <see cref="PlannerConstants.DataUrl"/> when it does not exist (the download half of Python <c>load_data</c>).
/// </summary>
public sealed class DownloadingGameDataSource : IGameDataSource
{
    private readonly IGameDataSource _inner;
    private readonly HttpClient _http;
    private readonly string _path;
    private readonly TextWriter _notices;

    /// <summary>Creates the decorator.</summary>
    /// <param name="inner">The source that reads the file once it exists.</param>
    /// <param name="http">The client used for the download (not disposed by this class).</param>
    /// <param name="path">The data file path, as given by the user.</param>
    /// <param name="notices">Receives the "not found; downloading" notice.</param>
    public DownloadingGameDataSource(IGameDataSource inner, HttpClient http, string path, TextWriter notices)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(http);
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(notices);
        _inner = inner;
        _http = http;
        _path = path;
        _notices = notices;
    }

    /// <inheritdoc />
    /// <exception cref="PlannerException">The download failed.</exception>
    public async Task<GameData> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            await _notices.WriteLineAsync($"{_path} not found; downloading {PlannerConstants.DataUrl}")
                .ConfigureAwait(false);
            await DownloadAsync(cancellationToken).ConfigureAwait(false);
        }

        return await _inner.LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DownloadAsync(CancellationToken cancellationToken)
    {
        string? folder = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string partial = _path + ".download";
        try
        {
            using HttpResponseMessage response = await _http
                .GetAsync(new Uri(PlannerConstants.DataUrl), HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            FileStream file = new(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await using (file.ConfigureAwait(false))
            {
                await response.Content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            }

            File.Move(partial, _path, overwrite: true);
        }
        catch (HttpRequestException e)
        {
            TryDelete(partial);
            throw new PlannerException($"Could not download {PlannerConstants.DataUrl}: {e.Message}", e);
        }
        catch
        {
            TryDelete(partial);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best effort: a leftover partial download does not affect the next run.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort, as above.
        }
    }
}
