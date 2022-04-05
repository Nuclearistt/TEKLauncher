using System.Net.Http;
using System.Threading;
using TEKLauncher.Steam.CM.Messages.Bodies;
using TEKLauncher.Steam.Manifest;

namespace TEKLauncher.Steam;

/// <summary>Manages the list of Steam CDN servers and downloads of manifests and patches from them.</summary>
static class CDNClient
{
    /// <summary>HTTP client that downloads manifests and patches from the CDN.</summary>
    static readonly HttpClient s_client = new() { DefaultRequestVersion = new(1, 1), Timeout = TimeSpan.FromSeconds(10) };
    /// <summary>Gets CDN server list.</summary>
    public static Uri[] Servers { get; private set; } = Array.Empty<Uri>();
    /// <summary>Updates CDN server list if necessary.</summary>
    public static void CheckServerList()
    {
        if (Servers.Length >= Client.NumberOfDownloadThreads)
            return;
        var servers = new List<CDNServersResponse.Types.Server>(Client.NumberOfDownloadThreads);
        while (servers.Count < Client.NumberOfDownloadThreads)
            servers.AddRange(Array.FindAll(CM.Client.GetCDNServers(), s => (s.Type == "SteamCache" || s.Type == "CDN") && (s.HttpsSupport == "mandatory" || s.HttpsSupport == "optional")));
        servers.Sort((left, right) =>
        {
            int result = (right.PreferredServer ? 1 : 0) - (left.PreferredServer ? 1 : 0);
            if (result == 0)
            {
                result = left.Load.CompareTo(right.Load);
                if (result == 0)
                    result = (right.HttpsSupport == "mandatory" ? 1 : 0) - (left.HttpsSupport == "mandatory" ? 1 : 0);
            }
            return result;
        });
        Servers = new Uri[Client.NumberOfDownloadThreads];
        for (int i = 0; i < Client.NumberOfDownloadThreads; i++)
            Servers[i] = new(string.Concat("https://", servers[i].Host));
    }
    /// <summary>Reads manifest file or downloads it if it's missing.</summary>
    /// <param name="item">Identifier of the item to get manifest for.</param>
    /// <param name="manifestId">ID of the manifest to get.</param>
    /// <param name="eventHandlers">Handlers for client events.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A depot manifest object.</returns>
    public static DepotManifest GetManifest(ItemIdentifier item, ulong manifestId, EventHandlers eventHandlers, CancellationToken cancellationToken)
    {
        string manifestPath = $@"{Client.ManifestsFolder}\{item}-{manifestId}.manifest";
        if (!File.Exists(manifestPath))
        {
            string encodedManifestPath = Path.ChangeExtension(manifestPath, ".manifest-enc");
            if (!File.Exists(encodedManifestPath))
            {
                CheckServerList();
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.DownloadingManifest), 0);
                bool success = false;
                Span<byte> buffer = stackalloc byte[81920];
                for (int i = 0; !success && i < Servers.Length; i++)
                    try
                    {
                        using var response = s_client.GetAsync($"{Servers[i]}depot/{item.DepotId}/manifest/{manifestId}/5", HttpCompletionOption.ResponseHeadersRead, cancellationToken).Result.EnsureSuccessStatusCode();
                        using var content = response.Content;
                        eventHandlers.PrepareProgress?.Invoke(true, content.Headers.ContentLength ?? -1);
                        using var stream = content.ReadAsStream(cancellationToken);
                        using var writer = File.Create(encodedManifestPath);
                        int bytesRead;
                        long progressAccumulator = 0;
                        long lastRecordedTime = 0;
                        do
                        {
                            bytesRead = stream.Read(buffer);
                            writer.Write(buffer[..bytesRead]);
                            progressAccumulator += bytesRead;
                            long timeDifference = Environment.TickCount64 - lastRecordedTime;
                            if (timeDifference >= 200)
                            {
                                lastRecordedTime += timeDifference;
                                eventHandlers.UpdateProgress?.Invoke(progressAccumulator);
                                progressAccumulator = 0;
                            }
                            if (cancellationToken.IsCancellationRequested)
                                throw new OperationCanceledException();
                        }
                        while (bytesRead > 0);
                        eventHandlers.UpdateProgress?.Invoke(progressAccumulator);
                        success = true;
                    }
                    catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { throw new OperationCanceledException(); }
                    catch (OperationCanceledException) { throw; }
                    catch { }
                if (!success)
                {
                    if (File.Exists(encodedManifestPath))
                        File.Delete(encodedManifestPath);
                    throw new SteamException(LocManager.GetString(LocCode.FailedToDownloadManifest));
                }
            }
            manifestPath = encodedManifestPath;
        }
        eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.LoadingManifest), 0);
        try { return new(manifestPath, item.DepotId); }
        catch
        {
            if (File.Exists(manifestPath))
                File.Delete(manifestPath);
            throw;
        }
    }
    /// <summary>Reads patch file or downloads it if it's missing.</summary>
    /// <param name="item">Identifier of the item to get patch for.</param>
    /// <param name="sourceManifestId">ID of the manifest to patch from.</param>
    /// <param name="targetManifestId">ID of the manifest to patch to.</param>
    /// <param name="eventHandlers">Handlers for client events.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A depot patch object.</returns>
    public static DepotPatch? GetPatch(ItemIdentifier item, ulong sourceManifestId, ulong targetManifestId, EventHandlers eventHandlers, CancellationToken cancellationToken)
    {
        string patchPath = $@"{Client.ManifestsFolder}\{item}-{sourceManifestId}-{targetManifestId}.patch";
        if (!File.Exists(patchPath))
        {
            string encodedPatchPath = Path.ChangeExtension(patchPath, ".patch-enc");
            if (!File.Exists(encodedPatchPath))
            {
                CheckServerList();
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.DownloadingPatch), 0);
                bool success = false;
                Span<byte> buffer = stackalloc byte[81920];
                for (int i = 0; !success && i < Servers.Length; i++)
                    try
                    {
                        using var response = s_client.GetAsync($"{Servers[i]}depot/{item.DepotId}/patch/{sourceManifestId}/{targetManifestId}", HttpCompletionOption.ResponseHeadersRead, cancellationToken).Result.EnsureSuccessStatusCode();
                        using var content = response.Content;
                        eventHandlers.PrepareProgress?.Invoke(true, content.Headers.ContentLength ?? -1);
                        using var stream = content.ReadAsStream(cancellationToken);
                        using var fileStream = File.Create(encodedPatchPath);
                        int bytesRead;
                        long progressAccumulator = 0;
                        long lastRecordedTime = 0;
                        do
                        {
                            bytesRead = stream.Read(buffer);
                            fileStream.Write(buffer[..bytesRead]);
                            progressAccumulator += bytesRead;
                            long timeDifference = Environment.TickCount64 - lastRecordedTime;
                            if (timeDifference >= 200)
                            {
                                lastRecordedTime += timeDifference;
                                eventHandlers.UpdateProgress?.Invoke(progressAccumulator);
                                progressAccumulator = 0;
                            }
                            if (cancellationToken.IsCancellationRequested)
                                throw new OperationCanceledException();
                        }
                        while (bytesRead > 0);
                        eventHandlers.UpdateProgress?.Invoke(progressAccumulator);
                        fileStream.Position = 0;
                        if (fileStream.Read(buffer[..16]) < 16 || BitConverter.ToUInt64(buffer) == 0 && BitConverter.ToUInt64(buffer[8..]) == 0) //Some patches have empty content (all bytes with value of zero) for some weird reason
                            return null;
                        success = true;
                    }
                    catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound) { return null; }
                    catch (HttpRequestException e) when (e.Message.Contains("invalid header")) { return null; } //Some patches have an invalid HTTP header for reasons only Steam knows
                    catch (AggregateException ae) when (ae.InnerException is HttpRequestException e && (e.StatusCode == HttpStatusCode.NotFound || e.Message.Contains("invalid header"))) { return null; }
                    catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { throw new OperationCanceledException(); }
                    catch (OperationCanceledException) { throw; }
                    catch { }
                if (!success)
                {
                    if (File.Exists(encodedPatchPath))
                        File.Delete(encodedPatchPath);
                    throw new SteamException(LocManager.GetString(LocCode.FailedToDownloadPatch));
                }
            }
            patchPath = encodedPatchPath;
        }
        eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.LoadingPatch), 0);
        try { return new(patchPath, Client.DepotKeys[item.DepotId]); }
        catch
        {
            if (File.Exists(patchPath))
                File.Delete(patchPath);
            throw;
        }
    }
}