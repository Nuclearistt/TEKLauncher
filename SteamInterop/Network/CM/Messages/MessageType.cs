namespace TEKLauncher.SteamInterop.Network.CM.Messages
{
	internal enum MessageType
    {
        Invalid,
        Multi,
        Heartbeat = 703,
        LogOff = 706,
        LogOnResponse = 751,
        ChannelEncrypt = 1303,
        ChannelEncryptResponse,
        ChannelEncryptResult,
        LogOn = 5514,
        ServiceMethod = 5594,
        ServiceMethodResponse,
        ProductInfo = 8903,
        ProductInfoResponse
    }
}