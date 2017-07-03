using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using uhttpsharp;
using uhttpsharp.Headers;
using uhttpsharp.Listeners;
using uhttpsharp.RequestProviders;

namespace BankSeeker
{
    class StatusServer
    {
        private HttpServer server;
        private string status = "INITIAL";

        public int Port { get; }

        public StatusServer()
        {
            server = new HttpServer(new HttpRequestProvider());

            // set as random port
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;

            server.Use(new TcpListenerAdapter(listener));
            server.Use((context, next) => {
                byte[] contents = Encoding.UTF8.GetBytes(this.status);
                bool keepAlive = ((IHttpContext)context).Request.Headers.KeepAliveConnection();
                context.Response = new HttpResponse(HttpResponseCode.Ok, contents, keepAlive);
                return Task.Factory.GetCompleted();
            });
            server.Start();
        }

        public void Update(string status)
        {
            this.status = status;
        }

        ~StatusServer()
        {
            server.Dispose();
        }
    }
}
