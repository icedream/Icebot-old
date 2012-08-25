using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.IO.Compression;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using System.Threading;

namespace Icebot.Databases
{
    [Serializable()]
    public class IDatabase
    {
        private Stream _stream = null;
        private static BinaryFormatter _serializer = new BinaryFormatter();

        public IDatabase()
        {
            // Setup serializer
            _serializer.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            _serializer.FilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
            _serializer.TypeFormat = System.Runtime.Serialization.Formatters.FormatterTypeStyle.TypesWhenNeeded;
        }

        protected void SetDatabaseStream(Stream stream)
        {
            if(this._stream != null)
                this._stream.Close();
            this._stream = new GZipStream(stream, CompressionMode.Compress, false);
            Sync();
        }

        protected static IDatabase FromDBFile(string file)
        {
            // Open database file
            Stream _bstream = File.Open(file, FileMode.OpenOrCreate);
            // Make database decompressable
            Stream _stream = new DeflateStream(_bstream, CompressionMode.Decompress, true);
            // Extract data!
            object dbObject = _serializer.Deserialize(_stream);
            // Close stream
            _stream.Close();
            // Return database
            ((IDatabase)dbObject)._stream = new GZipStream(_bstream, CompressionMode.Compress, false);
            return dbObject as IDatabase;
        }

        protected void Sync()
        {
            if (_stream != null)
            {
                _stream.Seek(0, SeekOrigin.Begin);
                _serializer.Serialize(_stream, this);
            }
        }
    }
}
