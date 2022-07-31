using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Toolkit.HighPerformance;
using TEKLauncher.Servers;
using TEKLauncher.Steam.CM.Messages;
using TEKLauncher.Steam.CM.Messages.Bodies;

namespace TEKLauncher.Steam.CM;

/// <summary>Steam CM client.</summary>
static class Client
{
    /// <summary>Steam cell ID used in certain requests.</summary>
    public static uint CellId { get; set; }
    /// <summary>Disconnects client from the CM server.</summary>
    public static void Disconnect() => WebSocketConnection.Disconnect();
    /// <summary>Refreshes Steam CM server list via Web API.</summary>
    public static void RefreshServerList()
    {
        var serverList = Downloader.DownloadJsonAsync<CMListResponse>($"https://api.steampowered.com/ISteamDirectory/GetCMList/v1?cellid={CellId}").Result;
        if (serverList.Response?.Servers is null)
            return;
        var urls = new Uri[serverList.Response.Servers.Length];
        for (int i = 0; i < urls.Length; i++)
            urls[i] = new($"wss://{serverList.Response.Servers[i]}/cmsocket/");
        WebSocketConnection.ServerList = new(urls);
    }
    /// <summary>Retrieves latest game and DLC manifest IDs for <see cref="Steam.Client.DepotManifestIds"/>.</summary>
    public static void UpdateDepotManifestIds()
    {
        if (!WebSocketConnection.IsLoggedOn)
            WebSocketConnection.Connect();
        ulong jobId = GlobalId.NextJobId();
        var message = new Message<ProductInfo>(MessageType.ProductInfo);
        message.Body.Apps.Add(new ProductInfo.Types.AppInfo { AppId = 346110, AccessToken = 0 });
        message.Body.MetadataOnly = false;
        message.Header.SourceJobId = jobId;
        var response = WebSocketConnection.GetMessage<ProductInfoResponse>(message, MessageType.ProductInfoResponse, jobId);
        if (response is null || response.Body.Apps.Count == 0)
            throw new SteamException(LocManager.GetString(LocCode.FailedToGetManifestID));
        using var reader = new StreamReader(response.Body.Apps[0].Buffer.Memory.AsStream());
        var vdf = new VDFNode(reader)["depots"];
        foreach (uint depotId in Steam.Client.DepotManifestIds.Keys)
            Steam.Client.DepotManifestIds[depotId] = ulong.TryParse(vdf?[depotId.ToString()]?["manifests"]?["public"]?.Value, out ulong id) ? id : 0;
        Steam.Client.ManifestIdsLastUpdated = Environment.TickCount64;
    }
    /// <summary>Gets request code for specified manifest.</summary>
    /// <param name="depotId">ID of the depot that the manifest belongs to.</param>
    /// <param name="manifestId">ID of the manifest to get request code for.</param>
    /// <returns>The manifest request code.</returns>
    public static ulong GetManifestRequestCode(uint depotId, ulong manifestId)
    {
        if (depotId > 346111) //A DLC depot
        {
            Span<byte> requestData = stackalloc byte[13];
            requestData[0] = 1;
            BitConverter.TryWriteBytes(requestData.Slice(1, 4), depotId);
            BitConverter.TryWriteBytes(requestData.Slice(5, 8), manifestId);
            byte[]? responseData = UdpClient.Transact(UdpClient.ArkoudaWatcherEndpoint, requestData);
            if (responseData is null || responseData.Length != 8)
                throw new SteamException(string.Format(LocManager.GetString(LocCode.FailedToGetManifestRequestCode), $"{depotId}-{manifestId}"));
            return BitConverter.ToUInt64(responseData);
        }
        if (!WebSocketConnection.IsLoggedOn)
            WebSocketConnection.Connect();
        ulong jobId = GlobalId.NextJobId();
        var message = new Message<ManifestRequestCode>(MessageType.ServiceMethod);
        message.Body.AppId = 346110;
        message.Body.DepotId = depotId;
        message.Body.ManifestId = manifestId;
        message.Body.AppBranch = "public";
        message.Header.SourceJobId = jobId;
        message.Header.TargetJobName = "ContentServerDirectory.GetManifestRequestCode#1";
        var response = WebSocketConnection.GetMessage<ManifestRequestCodeResponse>(message, MessageType.ServiceMethodResponse, jobId);
        if (response is null)
            throw new SteamException(string.Format(LocManager.GetString(LocCode.FailedToGetManifestRequestCode), $"{depotId}-{manifestId}"));
        return response.Body.ManifestRequestCode;
    }
    /// <summary>Retrieves latest manifest ID for specified mod.</summary>
    /// <param name="modId">ID of the mod to retrieve manifest ID for.</param>
    /// <returns>The manifest ID.</returns>
    public static ulong GetModManifestId(ulong modId)
    {
        if (!WebSocketConnection.IsLoggedOn)
            WebSocketConnection.Connect();
        ulong jobId = GlobalId.NextJobId();
        var message = new Message<ModInfo>(MessageType.ServiceMethod);
        message.Body.AppId = 346110;
        message.Body.Items.Add(new ModInfo.Types.Item { Id = modId });
        message.Header.SourceJobId = jobId;
        message.Header.TargetJobName = "PublishedFile.GetItemInfo#1";
        var response = WebSocketConnection.GetMessage<ModInfoResponse>(message, MessageType.ServiceMethodResponse, jobId);
        if (response is null || response.Body.Items.Count == 0)
            throw new SteamException(LocManager.GetString(LocCode.FailedToGetManifestID));
        return response.Body.Items[0].ManifestId;
    }
    /// <summary>Retrieves Steam CDN server list.</summary>
    /// <returns>An array of Steam CDN server entries.</returns>
    public static CDNServersResponse.Types.Server[] GetCDNServers()
    {
        if (!WebSocketConnection.IsLoggedOn)
            WebSocketConnection.Connect();
        ulong jobId = GlobalId.NextJobId();
        var message = new Message<CDNServers>(MessageType.ServiceMethod);
        message.Body.CellId = CellId;
        message.Header.SourceJobId = jobId;
        message.Header.TargetJobName = "ContentServerDirectory.GetServersForSteamPipe#1";
        var response = WebSocketConnection.GetMessage<CDNServersResponse>(message, MessageType.ServiceMethodResponse, jobId);
        if (response is null)
            throw new SteamException(LocManager.GetString(LocCode.FailedToGetCDNServerList));
        var result = new CDNServersResponse.Types.Server[response.Body.Servers.Count];
        response.Body.Servers.CopyTo(result, 0);
        return result;
    }
    /// <summary>Retrieves details for specified mods.</summary>
    /// <param name="ids">IDs of the mods to retrieve details for.</param>
    /// <returns>An array of mod details; the array is empty if request fails.</returns>
    public static Mod.ModDetails[] GetModDetails(params ulong[] ids)
    {
        if (!WebSocketConnection.IsLoggedOn)
            try { WebSocketConnection.Connect(); }
            catch { return Array.Empty<Mod.ModDetails>(); }
        ulong jobId = GlobalId.NextJobId();
        var message = new Message<ModDetails>(MessageType.ServiceMethod);
        message.Body.Ids.AddRange(ids);
        message.Body.IncludeMetadata = true;
        message.Header.SourceJobId = jobId;
        message.Header.TargetJobName = "PublishedFile.GetDetails#1";
        var response = WebSocketConnection.GetMessage<ModDetailsResponse>(message, MessageType.ServiceMethodResponse, jobId);
        if (response is null)
            return Array.Empty<Mod.ModDetails>();
        var result = new Mod.ModDetails[response.Body.Details.Count];
        for (int i = 0; i < response.Body.Details.Count; i++)
        {
            var item = response.Body.Details[i];
            result[i] = new(item.AppId, item.Result == 1 ? 1 : item.Result == 9 ? 2 : 0, DateTimeOffset.FromUnixTimeSeconds(item.LastUpdated).Ticks, item.Id, item.HcontentFile, item.Name, item.PreviewUrl);
        }
        return result;
    }
    /// <summary>Queries mods available in the workshop.</summary>
    /// <param name="page">Current page number.</param>
    /// <param name="search">Search query.</param>
    /// <param name="total">When this method returns, contains the total number of available pages.</param>
    /// <returns>An array of mod details; the array is empty if request fails.</returns>
    public static Mod.ModDetails[] QueryMods(uint page, string? search, out uint total)
    {
        total = 0;
        if (!WebSocketConnection.IsLoggedOn)
            try { WebSocketConnection.Connect(); }
            catch { return Array.Empty<Mod.ModDetails>(); }
        ulong jobId = GlobalId.NextJobId();
        var message = new Message<QueryMods>(MessageType.ServiceMethod);
        message.Body.Page = page;
        message.Body.ModsPerPage = 20;
        message.Body.AppId = 346110;
        if (!string.IsNullOrEmpty(search))
            message.Body.SearchText = search;
        message.Body.ReturnMetadata = true;
        message.Header.SourceJobId = jobId;
        message.Header.TargetJobName = "PublishedFile.QueryFiles#1";
        var response = WebSocketConnection.GetMessage<QueryModsResponse>(message, MessageType.ServiceMethodResponse, jobId);
        if (response is null)
            return Array.Empty<Mod.ModDetails>();
        total = response.Body.Total;
        var result = new Mod.ModDetails[response.Body.Items.Count];
        for (int i = 0; i < response.Body.Items.Count; i++)
        {
            var item = response.Body.Items[i];
            result[i] = new(346110, item.Result == 1 ? 1 : item.Result == 9 ? 2 : 0, DateTimeOffset.FromUnixTimeSeconds(item.LastUpdated).Ticks, item.Id, item.HcontentFile, item.Name, item.PreviewUrl);
        }
        return result;
    }
    /// <summary>Represents Steam Web API CM server list response JSON object.</summary>
    readonly record struct CMListResponse
    {
        [JsonPropertyName("response")]
        public ServerList? Response { get; init; }
        public record ServerList
        {
            [JsonPropertyName("serverlist_websockets")]
            public string[]? Servers { get; init; }
        }
    }
    /// <summary>Generates global IDs.</summary>
    static class GlobalId
    {
        /// <summary>Increments with every new generated job ID.</summary>
        static ulong s_counter;
        /// <summary>Mask for <see cref="s_counter"/> that also includes fields that need to be initialized only once.</summary>
        static readonly ulong s_mask;
        /// <summary>Initializes <see cref="s_mask"/>.</summary>
        static GlobalId()
        {
            using var currentProcess = Process.GetCurrentProcess();
            s_mask = 0x3FF0000000000 | (((((ulong)currentProcess.StartTime.Ticks - 0x8C6BDABF8998000) / 10000000) & 0xFFFFF) << 20);
        }
        /// <summary>Generates next unique job ID.</summary>
        /// <returns>A job ID.</returns>
        public static ulong NextJobId() => s_mask | ++s_counter;
    }
}