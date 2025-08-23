using System.Net.Http;
using System.Net.Http.Json;

namespace TEKLauncher.Data;

/// <summary>Manages general downloads within the app.</summary>
static class Downloader
{
    /// <summary>HTTP client that performs downloads.</summary>
    static readonly HttpClient s_client = new()
    {
        DefaultRequestVersion = new(2, 0), //Use HTTP/2 by default
        Timeout = TimeSpan.FromSeconds(10),
    };
    /// <summary>Initializes HTTP client.</summary>
    static Downloader() => s_client.DefaultRequestHeaders.UserAgent.ParseAdd($"TEKLauncher {App.Version}"); //Valid User-Agent must be specified to access GitHub API
    /// <summary>Asynchronously attempts to download a file.</summary>
    /// <param name="filePath">The path of the file to write downloaded content to.</param>
    /// <param name="eventHandlers">Handlers for PrepareProgress and UpdateProgress events.</param>
    /// <param name="urls">List of URLs of the file to download. Each consecutive URL will be used as fallback if the download fails for the previous one.</param>
    /// <returns>The task that represents the asynchronous operation and wraps the value that indicates whether the download succeeded.</returns>
    public static async Task<bool> DownloadFileAsync(string filePath, EventHandlers eventHandlers, params string[] urls)
    {
        foreach (string url in urls)
            try
            {
                using var response = (await s_client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false)).EnsureSuccessStatusCode();
                using var content = response.Content;
                eventHandlers.PrepareProgress?.Invoke(true, content.Headers.ContentLength ?? -1);
                using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
                using var writer = File.Create(filePath);
                byte[] buffer = new byte[81920];
                int bytesRead;
                long progressAccumulator = 0;
                long lastRecordedTime = 0;
                do
                {
                    bytesRead = await stream.ReadAsync(buffer).ConfigureAwait(false);
                    writer.Write(buffer, 0, bytesRead);
                    progressAccumulator += bytesRead;
                    long timeDifference = Environment.TickCount64 - lastRecordedTime;
                    if (timeDifference >= 200)
                    {
                        lastRecordedTime += timeDifference;
                        eventHandlers.UpdateProgress?.Invoke(progressAccumulator);
                        progressAccumulator = 0;
                    }
                }
                while (bytesRead > 0);
                eventHandlers.UpdateProgress?.Invoke(progressAccumulator);
                return true;
            }
            catch { }
        return false;
    }
	/// <summary>Asynchronously attempts to download a byte array.</summary>
	/// <param name="urls">List of URLs of the string to download. Each consecutive URL will be used as fallback if the download fails for the previous one.</param>
	public static async Task<byte[]?> DownloadBytesAsync(params string[] urls)
    {
		foreach (string address in urls)
			try { return await s_client.GetByteArrayAsync(address).ConfigureAwait(false); }
			catch { }
		return null;
	}
    /// <summary>Asynchronously attempts to download a string.</summary>
    /// <param name="urls">List of URLs of the string to download. Each consecutive URL will be used as fallback if the download fails for the previous one.</param>
    public static async Task<string?> DownloadStringAsync(params string[] urls)
    {
        foreach (string address in urls)
            try { return await s_client.GetStringAsync(address).ConfigureAwait(false); }
            catch { }
        return null;
    }
    /// <summary>Asynchronously attempts to download a JSON object.</summary>
    /// <param name="url">URL of the object to download.</param>
    public static async Task<T?> DownloadJsonAsync<T>(string url)
    {
        try { return await s_client.GetFromJsonAsync<T>(url).ConfigureAwait(false); }
        catch { return default; }
    }
}