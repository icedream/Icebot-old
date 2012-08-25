using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Threading;
using log4net;

namespace Icebot.Irc
{
    public class IrcLayer
    {
        // Constructor
        public IrcLayer()
        {
            _log = LogManager.GetLogger(this.GetType().Name);

            ShouldAutoPing = true;
        }

        // Private variables
        private ILog _log;
        private TcpClient _tcp = new TcpClient();
        private Stream _stream = null;
        private StreamReader _reader = null;
        private StreamWriter _writer = null;
        private Thread _readloopthread;

        // Public editable properties
        public bool ShouldAutoPing { get; set; }

        // Public read-only properties
        public bool IsConnected { get { return _tcp.Connected && _stream != null; } }

        // Events
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<ErrorEventArgs> ConnectionError;
        public event EventHandler<IrcRawSendEventArgs> RawSent;
        public event EventHandler<IrcRawReceiveEventArgs> RawReceived;
        public event EventHandler<IrcNumericReplyEventArgs> NumericReceived;

        // Event wrappers
        protected virtual void OnConnected()
        {
            if (Connected != null)
                Connected.Invoke(this, new EventArgs());
        }
        protected virtual void OnDisconnected()
        {
            if (Disconnected != null)
                Disconnected.Invoke(this, new EventArgs());
        }
        protected virtual void OnConnectionError(Exception e)
        {
            if (ConnectionError != null)
                ConnectionError.Invoke(this, new ErrorEventArgs(e));
        }
        protected virtual void OnRawReceived(IrcRawReceiveEventArgs e)
        {
            if (RawReceived != null)
                RawReceived.Invoke(this, e);

            // Check if reply is numeric
            if (IrcNumericReplyEventArgs.IsValid(e))
                OnNumericReceived(new IrcNumericReplyEventArgs(e));
        }
        protected virtual void OnRawSent(IrcRawSendEventArgs e)
        {
            if (RawSent != null)
                RawSent.Invoke(this, e);
        }
        protected virtual void OnNumericReceived(IrcNumericReplyEventArgs e)
        {
            if (NumericReceived != null)
                NumericReceived.Invoke(this, e);
        }

        // Public functions
        public void Connect(string host)
        {
            Connect(host, false);
        }
        public void Connect(string host, bool ssl)
        {
            Connect(host, (ushort)(ssl ? 6667 : 6697), ssl);
        }
        public void Connect(string host, ushort port)
        {
            Connect(host, 6667);
        }
        public void Connect(string host, ushort port, bool ssl)
        {
            if (IsConnected)
                throw new Exception("Already connected, disconnect first before reconnecting to this or another server.");

            _log.Info("Connecting to " + host + ":" + (ssl ? "+" : "") + port.ToString() + "...");
            _tcp.Connect(host, port);
            _stream = _tcp.GetStream();

            if (ssl)
            {
                _log.Info("Initializing SSL layer...");
                _stream = new SslStream(_stream, false);
                ((SslStream)_stream).AuthenticateAsClient(host);
            }

            // TODO: Auto-detect connection encoding
            _reader = new StreamReader(_stream, Encoding.UTF8);
            _writer = new StreamWriter(_stream, Encoding.UTF8);

            // Start the reading loop
            _readloopthread = new Thread(new ThreadStart(_readLoop));
            _readloopthread.IsBackground = true;
            _readloopthread.Start();

            _log.Info("Connection established.");

            OnConnected();
        }
        public void Disconnect()
        {
            try
            {
                _log.Info("Disconnecting...");
                _stream.Close();
                if (_stream != null)
                    _stream.Dispose();
                _log.Info("Disconnected.");
            }
            catch(Exception ex)
            {
                _log.Warn("Erroneous connection closed. Error: " + ex.Message);
            }
            try
            {
                if (_readloopthread != null)
                    _readloopthread.Abort();
            }
            catch (Exception ex)
            {
                _log.Warn("Could not abort reading loop thread. Error: " + ex.Message);
            }
            _readloopthread = null;
            _stream = null;
            _writer = null;
            _reader = null;
        }
        public void WriteLine(string line)
        {
            _log.Debug("SEND: " + line);
            OnRawSent(new IrcRawSendEventArgs(line));
            _writer.WriteLine(line);
            _writer.Flush();
        }
        public void SendCommand(string command)
        {
            SendCommand(command, new string[] { });
        }
        public void SendCommand(string command, params string[] parameters)
        {
            command = command.ToUpper();

            // Combine command with parameters if any given
            if (parameters != null && parameters.Length > 0)
            {
                if (parameters.Last().Contains(' ')) // last argument must have : prefixed if containing <space> char
                    parameters[parameters.Length - 1] = ":" + parameters.Last();
                command += " " + string.Join(" ", parameters);
            }

            // Write command
            WriteLine(command);
        }

        // Private functions
        private void _readLoop()
        {
            try
            {
                _log.Debug("Thread for looped reading started.");
                while (true)
                {
                    string line = _readLine();
                    if (ShouldAutoPing && line.StartsWith("PING", StringComparison.OrdinalIgnoreCase))
                        WriteLine(string.Format("PONG {0}", line.Substring(5)));
                    else
                        OnRawReceived(new IrcRawReceiveEventArgs(line));
                }
            }
            catch (ThreadAbortException)
            {
                _log.Debug("Catched thread abort request. Thread is shutting down...");
            }
            catch (Exception readError)
            {
                _log.Fatal("Error while reading from stream in reading loop: " + readError);
                _log.Error("Forcing disconnection...");
                Disconnect();
            }
        }
        private string _readLine()
        {
            try
            {
                string line = _reader.ReadLine();
                _log.Debug("RECV: " + line);
                return line;
            }
            catch (Exception e)
            {
                _log.Error("Could not read from stream: " + e);
                return "";
            }
        }
    }
}
