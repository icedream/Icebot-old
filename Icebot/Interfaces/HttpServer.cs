using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Cache;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using log4net;

namespace Icebot.Interfaces
{
    public class HttpServer
    {
        HttpListener listener = new HttpListener();
        Thread lthr;
        ILog log;

        public HttpServer()
        {
            listener.Prefixes.Add("http://*:29001/Icebot/");

            lthr = new Thread(new ThreadStart(_lthr));
            lthr.IsBackground = true;

            log = LogManager.GetLogger("HTTP Server");
        }

        public void Start()
        {
            log.Info("Starting http server");
            listener.Start();
            lthr.Start();
        }

        public void Stop()
        {
            log.Info("Stopping http server");
            lthr.Abort();
            listener.Stop();
        }

        private void _lthr()
        {
            log.Debug("Starting http listener thread");
            try
            {
                while (true)
                {
                    var req = listener.GetContext();
                    Thread thr = new Thread(new ParameterizedThreadStart(_http));
                    thr.IsBackground = true;
                    thr.Start(req);
                }
            }
            catch (ThreadAbortException)
            {
                log.Debug("Shutting down http listener thread (Thread aborted)");
            }
            log.Debug("Listener thread shut down");
        }

        private void _http(object o) { _http(o as HttpListenerContext); }
        private void _http(HttpListenerContext req)
        {
            log.Debug("HTTP request: " + req.Request.RawUrl);
            try
            {
                string cmd = req.Request.Url.AbsoluteUri.Split('/').Last().ToLower();
                switch (cmd)
                {
                        /*
                    case "mc":
                        log.Debug("Sending back SSMinecraftCheck json serialization");
                        req.Response.Close(
                            Encoding.UTF8.GetBytes(
                                JsonConvert.SerializeObject(Program.mc, Formatting.Indented, new IsoDateTimeConverter())
                            ),
                            true
                        );
                        break;
                         */
                    default:
                        req.Response.Close(Encoding.UTF8.GetBytes("Invalid request"), true);
                        break;
                }
            }
            catch
            {
                try
                {
                    req.Response.Close();
                }
                catch
                {
                    { }
                }
            }
        }
    }
}
