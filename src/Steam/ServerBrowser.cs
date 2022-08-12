using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using TEKLauncher.Servers;

namespace TEKLauncher.Steam;

/// <summary>Manages Steam client API interop for using subset of ISteamMatchmaking and ISteamMatchmakingServers interfaces.</summary>
static class ServerBrowser
{
    /// <summary>Steam pipe handle.</summary>
    static int s_pipe;
    /// <summary>Steam user handle.</summary>
    static int s_user;
    /// <summary>ISteamClient interface handle.</summary>
    static IntPtr s_steamClient;
    /// <summary>ISteamMatchmaking interface handle.</summary>
    static IntPtr s_steamMatchmaking;
    /// <summary>ISteamMatchmakingServers interface handle.</summary>
    static IntPtr s_steamMatchmakingServers;
    /// <summary>ISteamUtils interface handle.</summary>
    static IntPtr s_steamUtils;
    /// <summary>ISteamClient::BReleaseSteamPipe function pointer.</summary>
    static BReleaseSteamPipe s_bReleaseSteamPipe = null!;
    /// <summary>ISteamClient::ReleaseUser function pointer.</summary>
    static ReleaseUser s_releaseUser = null!;
    /// <summary>ISteamClient::BShutdownIfAllPipesClosed function pointer.</summary>
    static BShutdownIfAllPipesClosed s_bShutdownIfAllPipesClosed = null!;
    /// <summary>ISteamMatchmaking::AddFavoriteGame function pointer.</summary>
    static AddFavoriteGame s_addFavoriteGame = null!;
    /// <summary>ISteamMatchmaking::RemoveFavoriteGame function pointer.</summary>
    static RemoveFavoriteGame s_removeFavoriteGame = null!;
    /// <summary>ISteamMatchmakingServers::RequestInternetServerList function pointer.</summary>
    static RequestInternetServerList s_requestInternetServerList = null!;
    /// <summary>ISteamMatchmakingServers::RequestLANServerList function pointer.</summary>
    static RequestLANServerList s_requestLANServerList = null!;
    /// <summary>ISteamMatchmakingServers::RequestFavoritesServerList function pointer.</summary>
    static RequestFavoritesServerList s_requestFavoritesServerList = null!;
    /// <summary>ISteamMatchmakingServers::ReleaseRequest function pointer.</summary>
    static ReleaseRequest s_releaseRequest = null!;
    /// <summary>ISteamMatchmakingServers::GetServerDetails function pointer.</summary>
    static GetServerDetails s_getServerDetails = null!;
    /// <summary>ISteamMatchmakingServers::CancelQuery function pointer.</summary>
    static CancelQuery s_cancelQuery = null!;
    /// <summary>ISteamMatchmakingServers::GetServerCount function pointer.</summary>
    static GetServerCount s_getServerCount = null!;
    /// <summary>ISteamUtils::RunFrame function pointer.</summary>
    static RunFrame s_runFrame = null!;
    //Steam API function delegate definitions
    delegate int CreateSteamPipe(IntPtr pThis);
    delegate bool BReleaseSteamPipe(IntPtr pThis, int hSteamPipe);
    delegate int ConnectToGlobalUser(IntPtr pThis, int hSteamPipe);
    delegate void ReleaseUser(IntPtr pThis, int hSteamPipe, int hUser);
    delegate IntPtr GetISteamGenericInterface(IntPtr pThis, int hSteamUser, int hSteamPipe, [MarshalAs(UnmanagedType.LPStr)]string pchVersion);
    delegate bool BShutdownIfAllPipesClosed(IntPtr pThis);
    delegate int AddFavoriteGame(IntPtr pThis, uint nAppID, uint nIP, ushort nConnPort, ushort nQueryPort, uint unFlags, uint rTime32LastPlayedOnServer);
    delegate bool RemoveFavoriteGame(IntPtr pThis, uint nAppID, uint nIP, ushort nConnPort, ushort nQueryPort, uint unFlags);
    delegate IntPtr RequestInternetServerList(IntPtr pThis, uint iApp, in MatchMakingKeyValuePair_t[] ppchFilters, uint nFilters, IntPtr pRequestServersResponse);
    delegate IntPtr RequestLANServerList(IntPtr pThis, uint iApp, IntPtr pRequestServersResponse);
    delegate IntPtr RequestFavoritesServerList(IntPtr pThis, uint iApp, in MatchMakingKeyValuePair_t[] ppchFilters, uint nFilters, IntPtr pRequestServersResponse);
    delegate void ReleaseRequest(IntPtr pThis, IntPtr hServerListRequest);
    delegate IntPtr GetServerDetails(IntPtr pThis, IntPtr hRequest, int iServer);
    delegate void CancelQuery(IntPtr pThis, IntPtr hRequest);
    delegate int GetServerCount(IntPtr pThis, IntPtr hRequest);
    delegate void RunFrame(IntPtr pThis);
    /// <summary>Attempts to create Steam client API interfaces and get function pointers.</summary>
    static void Initialize()
    {
        if (!App.IsRunning)
            return;
        string? dllPath = (string?)Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam\ActiveProcess")?.GetValue("SteamClientDll64");
        if (dllPath is null)
            return;
        //Load steamclient64.dll module into process
        if (WinAPI.LoadLibraryExW(dllPath, IntPtr.Zero, 0x8) == IntPtr.Zero)
            return;
        s_steamClient = CreateInterface("SteamClient020", 0); //Create ISteamClient interface
        var vfptr = Marshal.ReadIntPtr(s_steamClient); //, get its virtual function poiter table
        //and compute its function addresses
        s_pipe = Marshal.GetDelegateForFunctionPointer<CreateSteamPipe>(Marshal.ReadIntPtr(vfptr))(s_steamClient);
        s_bReleaseSteamPipe = Marshal.GetDelegateForFunctionPointer<BReleaseSteamPipe>(Marshal.ReadIntPtr(vfptr, 0x8));
        s_user = Marshal.GetDelegateForFunctionPointer<ConnectToGlobalUser>(Marshal.ReadIntPtr(vfptr, 0x10))(s_steamClient, s_pipe);
        s_releaseUser = Marshal.GetDelegateForFunctionPointer<ReleaseUser>(Marshal.ReadIntPtr(vfptr, 0x20));
        var getISteamGenericInterface = Marshal.GetDelegateForFunctionPointer<GetISteamGenericInterface>(Marshal.ReadIntPtr(vfptr, 0x60));
        s_bShutdownIfAllPipesClosed = Marshal.GetDelegateForFunctionPointer<BShutdownIfAllPipesClosed>(Marshal.ReadIntPtr(vfptr, 0xB8));
        //Create the rest of interfaces and get their function pointers
        s_steamMatchmaking = getISteamGenericInterface(s_steamClient, s_user, s_pipe, "SteamMatchMaking009");
        s_steamMatchmakingServers = getISteamGenericInterface(s_steamClient, s_user, s_pipe, "SteamMatchMakingServers002");
        s_steamUtils = getISteamGenericInterface(s_steamClient, s_user, s_pipe, "SteamUtils010");
        vfptr = Marshal.ReadIntPtr(s_steamMatchmaking);
        s_addFavoriteGame = Marshal.GetDelegateForFunctionPointer<AddFavoriteGame>(Marshal.ReadIntPtr(vfptr, 0x10));
        s_removeFavoriteGame = Marshal.GetDelegateForFunctionPointer<RemoveFavoriteGame>(Marshal.ReadIntPtr(vfptr, 0x18));
        vfptr = Marshal.ReadIntPtr(s_steamMatchmakingServers);
        s_requestInternetServerList = Marshal.GetDelegateForFunctionPointer<RequestInternetServerList>(Marshal.ReadIntPtr(vfptr));
        s_requestLANServerList = Marshal.GetDelegateForFunctionPointer<RequestLANServerList>(Marshal.ReadIntPtr(vfptr, 0x08));
        s_requestFavoritesServerList = Marshal.GetDelegateForFunctionPointer<RequestFavoritesServerList>(Marshal.ReadIntPtr(vfptr, 0x18));
        s_releaseRequest = Marshal.GetDelegateForFunctionPointer<ReleaseRequest>(Marshal.ReadIntPtr(vfptr, 0x30));
        s_getServerDetails = Marshal.GetDelegateForFunctionPointer<GetServerDetails>(Marshal.ReadIntPtr(vfptr, 0x38));
        s_cancelQuery = Marshal.GetDelegateForFunctionPointer<CancelQuery>(Marshal.ReadIntPtr(vfptr, 0x40));
        s_getServerCount = Marshal.GetDelegateForFunctionPointer<GetServerCount>(Marshal.ReadIntPtr(vfptr, 0x58));
        s_runFrame = Marshal.GetDelegateForFunctionPointer<RunFrame>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(s_steamUtils), 0x70));
    }
    /// <summary>Adds specified server to favorites.</summary>
    /// <param name="endpoint">Query endpoint of the server.</param>
    public static void AddFavorite(IPEndPoint endpoint)
    {
        Span<byte> buffer = stackalloc byte[4];
        endpoint.Address.TryWriteBytes(buffer, out _);
        s_addFavoriteGame(s_steamMatchmaking, 346110, (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer)), (ushort)endpoint.Port, (ushort)endpoint.Port, 0x1, (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
    /// <summary>Removes specified server from favorites.</summary>
    /// <param name="endpoint">Query endpoint of the server.</param>
    public static void RemoveFavorite(IPEndPoint endpoint)
    {
        Span<byte> buffer = stackalloc byte[4];
        endpoint.Address.TryWriteBytes(buffer, out _);
        s_removeFavoriteGame(s_steamMatchmaking, 346110, (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer)), (ushort)endpoint.Port, (ushort)endpoint.Port, 0x1);
    }
    /// <summary>Shuts down Steam client API interfaces and releases their resources.</summary>
    public static void Shutdown()
    {
        if (s_steamClient == IntPtr.Zero)
            return;
        s_releaseUser(s_steamClient, s_pipe, s_user);
        s_bReleaseSteamPipe(s_steamClient, s_pipe);
        s_bShutdownIfAllPipesClosed(s_steamClient);
    }
    /// <summary>Gets specified server list via Steam client API.</summary>
    /// <param name="type">Type of the server list to get.</param>
    /// <param name="clusterId">ID of the cluster to get servers for.</param>
    /// <returns>An array of server objects, or <see langword="null"/> if the request fails.</returns>
    public static Server[]? GetServers(ServerListType type, string? clusterId = null)
    {
        if (s_steamClient == IntPtr.Zero)
        {
            Initialize();
            if (s_steamClient == IntPtr.Zero)
                return null;
        }
        var filters = type == ServerListType.LAN ? null : new MatchMakingKeyValuePair_t[]
        {
            new() { m_szKey = "gamedir", m_szValue = "ark_survival_evolved" },
            new() { m_szKey = "gamedataand", m_szValue = clusterId is null ? "SERVERUSESBATTLEYE_b:false,TEKWrapper:1" : $"SERVERUSESBATTLEYE_b:false,TEKWrapper:1,CLUSTERID_s:{clusterId}" }
        };
        var request = type switch
        {
            ServerListType.LAN => s_requestLANServerList(s_steamMatchmakingServers, 346110, IntPtr.Zero),
            ServerListType.Favorites => s_requestFavoritesServerList(s_steamMatchmakingServers, 346110, in filters!, 2, IntPtr.Zero),
            _ => s_requestInternetServerList(s_steamMatchmakingServers, 346110, in filters!, 2, IntPtr.Zero)
        };
        for (int i = type == ServerListType.Online ? 250 : 20; i > 0 && s_getServerCount(s_steamMatchmakingServers, request) == 0; i--)
        {
            s_runFrame(s_steamUtils);
            Thread.Sleep(20);
        }
        s_cancelQuery(s_steamMatchmakingServers, request);
        int numServers = s_getServerCount(s_steamMatchmakingServers, request);
        if (numServers > 300)
            numServers = 300;
        var result = new Server[numServers];
        for (int i = 0; i < numServers; i++)
        {
            var address = Marshal.PtrToStructure<Servernetadr_t>(s_getServerDetails(s_steamMatchmakingServers, request, i));
            result[i] = new(new(unchecked((uint)IPAddress.NetworkToHostOrder(address.m_unIP)), address.m_usQueryPort));
        }
        s_releaseRequest(s_steamMatchmakingServers, request);
        return result;
    }
    [DllImport("steamclient64.dll", CharSet = CharSet.Ansi)]
    static extern IntPtr CreateInterface(string pchVersion, int cbSize);
    /// <summary>Defines types of server lists that may be retrieved by <see cref="GetServers(ServerListType)"/>.</summary>
    public enum ServerListType
    {
        LAN,
        Favorites,
        Online
    }
    //Steam client API type definitions
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    struct MatchMakingKeyValuePair_t
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string m_szKey;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string m_szValue;
    }
    #pragma warning disable CS0649
    struct Servernetadr_t
    {
        public ushort m_usConnectionPort;
        public ushort m_usQueryPort;
        public int m_unIP;
    };
    #pragma warning restore CS0649
}