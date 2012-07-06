using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Xml;
using log4net;
using log4net.Core;

namespace Icebot
{
    public class Icebot
    {
        public Icebot()
        {
            __CONSTRUCT(_defaultConfigurationFile);
        }

        public IcebotConfiguration config = new IcebotConfiguration();

        internal List<IcebotServer> servers = new List<IcebotServer>();

        internal static Assembly _asm
        {
            get { return Assembly.GetExecutingAssembly(); }
        }

        private string _defaultConfigurationFile
        {
            get { return _asm.FullName + ".xml"; }
        }

        private void __CONSTRUCT(string configuration)
        {
        }

        public void LoadConfig(string filename)
        {
            LoadLogEngine(filename);
            XmlReaderSettings setup = new XmlReaderSettings();
            setup.ConformanceLevel = ConformanceLevel.Document;
            setup.IgnoreComments = true;
            setup.DtdProcessing = DtdProcessing.Ignore;
            setup.CloseInput = true;
            XmlReader r = XmlReader.Create(filename, setup);
            while (r.Read() && r.Name != "icebot" && !r.EOF) { }
            if (r.EOF)
            {
                _log.Error("Didn't find a valid icebot configuration in the config file.");
                throw new Exception();
            }
            else
            {
                config = IcebotConfiguration.Deserialize(r);
                r.Close();
                _log.Info("Loaded icebot configuration.");
            }
        }

        private void LoadLogEngine(string filename)
        {
            // Loading log engine
            log4net.Config.XmlConfigurator.Configure(new FileInfo(filename));
            _log.Info("Log engine loaded.");
        }

        protected ILog _log
        {
            get { return LogManager.GetLogger(this.GetType().Name); }
        }

        public void Connect()
        {
            if (servers.Count != 0)
                return;
            foreach (IcebotServerConfiguration s in config.Servers)
            {
                IcebotServer serv = new IcebotServer(this, s);
                servers.Add(serv);
                serv.Connect();
            }
        }

        public void Disconnect()
        {
            foreach (IcebotServer s in servers)
            {
                s.Disconnect();
                servers.Remove(s);
            }
        }
    }
}
