using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JumpServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;

namespace JumpClient.Controllers
{
    public class ClientService : IHostedService
    {
        private HubConnection _connection;

        private ClientValueObject _client = new ClientValueObject("1234", null, "name", true, new TunnelValueObject[] { });
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/clientHub",  (opts) =>
                {
                    opts.HttpMessageHandlerFactory = (message) =>
                    {
                        if (message is HttpClientHandler clientHandler)
                            // always verify the SSL certificate
                            clientHandler.ServerCertificateCustomValidationCallback +=
                                (sender, certificate, chain, sslPolicyErrors) => { return true; };
                        return message;
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            var impl = new ClientImpl(_connection);
            
            _connection.Reconnected += async connectionId =>
            {
                _client = _client with { ConnectionId = connectionId };
                await impl.ClientHello(_client);
            };
            
            try
            {
                await _connection.StartAsync(cancellationToken);

                await impl.ClientHello(_client);

                _connection.On("StartTunnel", new Func<TunnelValueObject, Task<bool>>(async tunnel =>
                {
                    //TODO: Handle 
                    _client = _client with { Tunnels = new []{tunnel} };
                    await impl.TunnelConnected(_client);

                    return true;
                }));
                
                _connection.On("StopTunnel", new Func<TunnelValueObject, Task<bool>>(async tunnel =>
                {
                    //TODO: Handle 
                    _client = _client with { Tunnels = new []{tunnel} };
                    await impl.TunnelDisconnected(_client);

                    return true;
                }));
            }
            catch (Exception e)
            {
                
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _connection.StopAsync(cancellationToken);
            }
            catch (Exception e)
            {
                
            }
        }
    }
}