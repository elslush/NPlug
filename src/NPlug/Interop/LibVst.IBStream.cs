// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.IO;

namespace NPlug.Interop;

internal static unsafe partial class LibVst
{
    /// <summary>
    /// Proxy implementation to connect a <see cref="Stream"/> to an <see cref="IBStream"/>.
    /// </summary>
    public sealed class IBStreamClient : Stream
    {
        [ThreadStatic]
        private static IBStreamClient? _stream;
        internal static IBStreamClient GetStream(IBStream* state)
        {
            var stream = _stream;
            if (stream is null)
            {
                stream = new IBStreamClient();
                _stream = stream;
            }
            stream.NativeStream = state;
            return stream;
        }

        public IBStream* NativeStream { get; set; }
        
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(new Span<byte>(buffer, offset, count));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long position = 0;
            NativeStream->seek(offset, (int)origin, &position);
            return position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Cannot set the length on the underlying VST IBStream");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            int read = 0;
            fixed (byte* ptr = buffer)
            {
                NativeStream->read(ptr, buffer.Length, &read);
            }
            return read;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            fixed (byte* ptr = buffer)
            {
                int offset = 0;
                while (offset < buffer.Length)
                {
                    int bytesWritten = 0;
                    if (NativeStream->write(ptr + offset, buffer.Length - offset, &bytesWritten).IsSuccess)
                    {
                        offset += bytesWritten;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public override long Length
        {
            get
            {
                var currentPosition = Position;
                Seek(0, SeekOrigin.End);
                var length = Position;
                Position = currentPosition;
                return length;
            }
        }

        public override long Position
        {
            get
            {
                long length = 0;
                NativeStream->tell(&length);
                return length;
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
    }

    /// <summary>
    /// Managed equivalent is <see cref="Stream"/>.
    /// </summary>
    public partial struct IBStream
    {
        private static Stream Get(IBStream* stream) => (Stream)((ComObjectHandle*)stream)->Handle.Target!;

        private static partial ComResult read_ToManaged(IBStream* self, void* buffer, int numBytes, int* numBytesRead)
        {
            var stream = Get(self);
            *numBytesRead = stream.Read(new Span<byte>(buffer, numBytes));
            return true;
        }

        private static partial ComResult write_ToManaged(IBStream* self, void* buffer, int numBytes, int* numBytesWritten)
        {
            var stream = Get(self);
            stream.Write(new Span<byte>(buffer, numBytes));
            *numBytesWritten = numBytes;
            return true;
        }

        private static partial ComResult seek_ToManaged(IBStream* self, long pos, int mode, long* result)
        {
            var stream = Get(self);
            *result = stream.Seek(pos, mode == 0 ? SeekOrigin.Begin : mode == 1 ? SeekOrigin.Current : SeekOrigin.End);
            return true;
        }

        private static partial ComResult tell_ToManaged(IBStream* self, long* pos)
        {
            var stream = Get(self);
            *pos = stream.Position;
            return true;
        }
    }
}