using Google.Protobuf;

namespace TEKLauncher.Steam.CM.Messages;

/// <summary>Represents a Steam CM protobuf-serialized message.</summary>
class Message
{
    /// <summary>Type of the message.</summary>
    readonly MessageType _type;
    /// <summary>Initializes a new message object with specified message type.</summary>
    /// <param name="type">Type of the message.</param>
    protected Message(MessageType type) => _type = type;
    /// <summary>Gets body of the message.</summary>
    protected IMessage? BodyObject { get; init; }
    /// <summary>Gets header of the message.</summary>
    public MessageHeader Header { get; } = new();
    /// <summary>Serializes the message into a byte array.</summary>
    public byte[] Serialize()
    {
        int headerSize = Header.CalculateSize(), bodySize = BodyObject!.CalculateSize();
        byte[] buffer = new byte[headerSize + bodySize + 8];
        using var stream = new CodedOutputStream(buffer);
        stream.WriteFixed32((uint)_type | 0x80000000);
        stream.WriteFixed32((uint)headerSize);
        stream.WriteRawMessage(Header);
        stream.WriteRawMessage(BodyObject);
        return buffer;
    }
}
/// <summary>Represents a Steam CM protobuf-serialized message with <typeparamref name="TBody"/> as its body.</summary>
class Message<TBody> : Message where TBody : IMessage, new()
{
    /// <summary>Initializes a new message object with specified message type and default body.</summary>
    /// <param name="type">Type of the message.</param>
    public Message(MessageType type) : base(type) => BodyObject = new TBody();
    /// <summary>Initializes a new message object by deserializing a byte array.</summary>
    /// <param name="data">Buffer that contains serialized message data.</param>
    /// <param name="size">Size of message data in <paramref name="data"/>.</param>
    public Message(byte[] data, int size) : base((MessageType)(BitConverter.ToUInt32(data) & 0x7FFFFFFF))
    {
        BodyObject = new TBody();
        int headerSize = BitConverter.ToInt32(data, 4);
        using (var protoDecoder = new CodedInputStream(data, 8, headerSize))
            protoDecoder.ReadRawMessage(Header);
        using (var protoDecoder = new CodedInputStream(data, headerSize + 8, size - headerSize - 8))
            protoDecoder.ReadRawMessage(BodyObject);
    }
    /// <summary>Gets body of the message.</summary>
    public TBody Body => (TBody)BodyObject!;
}