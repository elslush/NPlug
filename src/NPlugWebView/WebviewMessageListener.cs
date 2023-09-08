using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using static NPlug.Interop.LibVst;

namespace NPlugWebView;

public class WebviewMessageListener
{
    private Webview webview;

    public WebviewMessageListener(Webview webview)
    {
        this.webview = webview;
    }

    public enum Type
    {
        INT,
        FLOAT,
        STRING,
        BINARY
    }

    public class MessageAttribute
    {
        public string Name { get; set; }
        public Type Type { get; set; }
    }

    public void Subscribe(string receiver, string messageId, List<MessageAttribute> attributes)
    {
        subscriptions[messageId] = new MessageSubscription
        {
            Descriptor = new MessageDescriptor { MessageId = messageId, Attributes = attributes },
            NotifyFunction = receiver
        };
    }

    unsafe int Notify(IMessage message)
    {
        var messageIdPtr = message.getMessageID();
        var messageId = Marshal.PtrToStringUni((IntPtr)messageIdPtr);

        if (messageId is null || !subscriptions.TryGetValue(messageId, out var subscription))
            return ComResult.False; // replace TReturn with the actual type

        var json = SerializeMessage(message, subscription.Descriptor);
        var sendMessageJs = $"{subscription.NotifyFunction}({json});";
        webview.EvalJS(sendMessageJs, response => { });
        return ComResult.Ok; // replace TReturn with the actual type
    }

    private unsafe string SerializeMessage(IMessage message, MessageDescriptor descriptor)
    {
        var attributes = *message.getAttributes();

        using MemoryBufferWriter memoryBufferWriter = MemoryBufferWriter.Get();
        using Utf8JsonWriter jsonDoc = new((IBufferWriter<byte>)memoryBufferWriter);

        jsonDoc.WriteStartObject();
        var messageIdPtr = message.getMessageID();
        var messageId = Marshal.PtrToStringUni((IntPtr)messageIdPtr); // Assumes ASCII. If it's UTF-8, you might need a different conversion.
        jsonDoc.WriteString("messageId", messageId);

        foreach (var attr in descriptor.Attributes)
        {
            var maxByteCount = Encoding.UTF8.GetMaxByteCount(attr.Name.Length);
            IntPtr bytesPtr = Marshal.AllocHGlobal(maxByteCount + 1);
            fixed (char* charsPtr = attr.Name)
            {
                int actualByteCount = Encoding.UTF8.GetBytes(charsPtr, attr.Name.Length, (byte*)bytesPtr, maxByteCount);
                *((byte*)bytesPtr + actualByteCount) = 0; // null terminator
            }

            var attrID = new AttrID { Value = (byte*)bytesPtr };

            switch (attr.Type)
            {
                case Type.INT:
                    long i;
                    if (attributes.getInt(attrID, &i) == ComResult.Ok)
                        jsonDoc.WriteNumber(attr.Name.AsSpan(), i);
                    break;

                case Type.FLOAT:
                    double floatValue;
                    if (attributes.getFloat(attrID, &floatValue) == ComResult.Ok)
                        jsonDoc.WriteNumber(attr.Name.AsSpan(), floatValue);
                    break;

                case Type.STRING:
                    IntPtr bufferPtr = Marshal.AllocHGlobal(128 * sizeof(char));
                    try
                    {
                        if (attributes.getString(attrID, (char*)bufferPtr, 128 * sizeof(char)) == ComResult.Ok)
                        {
                            var strValue = Marshal.PtrToStringUni(bufferPtr); 
                            jsonDoc.WriteString(attr.Name.AsSpan(), strValue);
                        }
                    }
                    finally
                    {
                        // Always free the allocated unmanaged memory
                        Marshal.FreeHGlobal(bufferPtr);
                    }
                    break;

                case Type.BINARY:
                    uint binarySize = 0;
                    IntPtr binarySizePtr = new(&binarySize);
                    void* actualBinaryDataPtr = null;

                    var binaryResult = attributes.getBinary(attrID, &actualBinaryDataPtr, (uint*)&binarySizePtr);

                    // Adjust the method signature for GetBinary if it's different
                    if (binaryResult == ComResult.Ok && binarySize > 0)
                        jsonDoc.WriteBase64String(attr.Name.AsSpan(), new ReadOnlySpan<byte>(actualBinaryDataPtr, (int)binarySize));
                    break;
            }

            Marshal.FreeHGlobal((IntPtr)attrID.Value);
        }

        jsonDoc.WriteEndObject();
        jsonDoc.Flush();
        return Encoding.UTF8.GetString(memoryBufferWriter.GetSpan());
    }

    private class MessageDescriptor
    {
        public string MessageId { get; set; }
        public List<MessageAttribute> Attributes { get; set; }
    }

    private class MessageSubscription
    {
        public MessageDescriptor Descriptor { get; set; }
        public string NotifyFunction { get; set; }
    }

    private Dictionary<string, MessageSubscription> subscriptions = new Dictionary<string, MessageSubscription>();
}
