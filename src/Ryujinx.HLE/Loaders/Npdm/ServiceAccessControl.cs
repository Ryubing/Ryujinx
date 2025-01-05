using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Ryujinx.HLE.Loaders.Npdm
{
    public class ServiceAccessControl
    {
        public IReadOnlyDictionary<string, bool> Services { get; private set; }

        /// <exception cref="System.ArgumentException">The stream does not support reading, is <see langword="null"/>, or is already closed.</exception>
        /// <exception cref="System.ArgumentException">An error occured while reading bytes from the stream.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        public ServiceAccessControl(Stream stream, int offset, int size)
        {
            stream.Seek(offset, SeekOrigin.Begin);

            BinaryReader reader = new(stream);

            int bytesRead = 0;

            Dictionary<string, bool> services = [];

            while (bytesRead != size)
            {
                byte controlByte = reader.ReadByte();

                if (controlByte == 0)
                {
                    break;
                }

                int length = (controlByte & 0x07) + 1;
                bool registerAllowed = (controlByte & 0x80) != 0;

                services[Encoding.ASCII.GetString(reader.ReadBytes(length))] = registerAllowed;

                bytesRead += length + 1;
            }

            Services = new ReadOnlyDictionary<string, bool>(services);
        }
    }
}
