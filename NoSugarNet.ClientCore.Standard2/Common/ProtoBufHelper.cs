using Google.Protobuf;
using System;

namespace NoSugarNet.ClientCoreNet.Standard2.Common
{
    public static class ProtoBufHelper
    {
        public static byte[] Serizlize(IMessage msg)
        {
            return msg.ToByteArray();
        }
        public static T DeSerizlize<T>(byte[] bytes)
        {
            var msgType = typeof(T);
            object msg = Activator.CreateInstance(msgType);
            ((IMessage)msg).MergeFrom(bytes);
            return (T)msg;
        }
    }

}
