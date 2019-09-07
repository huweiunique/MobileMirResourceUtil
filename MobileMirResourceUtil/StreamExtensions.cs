using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MobileMirResourceUtil
{
    public static class StreamExtensions
    {
        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        public static async Task WriteAsync(this Stream stream, string value)
        {
            var dataBytes = Encoding.UTF8.GetBytes(value);
            await stream.WriteAsync(dataBytes, 0, dataBytes.Length, CancellationToken.None).ConfigureAwait(false);
        }
        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        public static async Task WriteAsync(this Stream stream, byte value)
        {
            await stream.WriteAsync(new[] { value }, 0, 1, CancellationToken.None).ConfigureAwait(false);
        }
        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        public static async Task WriteAsync(this Stream stream, byte[] value)
        {
            await stream.WriteAsync(value, 0, value.Length, CancellationToken.None).ConfigureAwait(false);
        }
        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        public static async Task WriteAsync(this Stream stream, short value)
        {
            var dataBytes = BitConverter.GetBytes(value);
            await stream.WriteAsync(dataBytes, 0, dataBytes.Length, CancellationToken.None).ConfigureAwait(false);
        }
        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        public static async Task WriteAsync(this Stream stream, int value)
        {
            var dataBytes = BitConverter.GetBytes(value);
            await stream.WriteAsync(dataBytes, 0, dataBytes.Length, CancellationToken.None).ConfigureAwait(false);
        }

        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        public static async Task<byte[]> ReadBytesAsync(this Stream stream, int length)
        {
            var dataBytes = new byte[length];
            await stream.ReadAsync(dataBytes, 0, dataBytes.Length, CancellationToken.None).ConfigureAwait(false);
            return dataBytes;
        }
    }
}
