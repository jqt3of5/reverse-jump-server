using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JumpServer.Models;

namespace JumpServer.Busi
{
    public class ClientModel
    {
        private List<ClientValueObject> _clients = new();
        public IEnumerable<ClientValueObject> Clients => _clients;

        public bool GetClient(string clientId, [NotNullWhen(true)] out ClientValueObject? client)
        {
            if (_clients.FirstOrDefault(c => c.ClientId == clientId) is { } c)
            {
                client = c;
                return true;
            }

            client = null;
            return false;
        }
        
        public void UpdateClient(ClientValueObject client)
        {
            if (_clients.FirstOrDefault(c => c.ClientId == client.ClientId) is { } c)
            {
               _clients.Remove(c);
            }
            _clients.Add(client); 
        }

        public void ClientConnected(string connectionId)
        {
            if (_clients.FirstOrDefault(c => c.ConnectionId == connectionId) is { } client)
            {
                UpdateClient(client with {Connected = true});
            }
        }

        public void ClientDisconnected(string connectionId)
        {
            if (_clients.FirstOrDefault(c => c.ConnectionId == connectionId) is { } client)
            {
                UpdateClient(client with {Connected = false});
            } 
        }

        public bool AttachTunnel(string clientId, TunnelValueObject tunnel)
        {
            //TODO: Thread Safety
            
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return false;
            }
            
            if (_clients.FirstOrDefault(c => c.ClientId == clientId) is { } client)
            {
                UpdateClient(client with {Tunnels = client.Tunnels.Append(tunnel).ToArray()});
                return true;
            }

            return false;
        }

        public bool TunnelConnected(string clientId, string tunnelId)
        {
            if (string.IsNullOrWhiteSpace(tunnelId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return false;
            }

            if (_clients.FirstOrDefault(c => c.ClientId == clientId) is { } client)
            {
                if (client.Tunnels.FirstOrDefault(t => t.TunnelId == tunnelId) is { } tunnel)
                {
                    var tunnels = client.Tunnels.Except(new[] { tunnel });
                   
                    UpdateClient(client with {Tunnels = tunnels.Append(tunnel with {Established = true}).ToArray()});
                    return true;
                }
            }

            return false;
        }

        public bool TunnelDisconnected(string clientId, string tunnelId)
        {
            if (string.IsNullOrWhiteSpace(tunnelId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return false;
            } 
            
            if (_clients.FirstOrDefault(c => c.ClientId == clientId) is { } client)
            {
                if (client.Tunnels.FirstOrDefault(t => t.TunnelId == tunnelId) is { } tunnel)
                {
                    var tunnels = client.Tunnels.Except(new[] { tunnel });
                   
                    UpdateClient(client with {Tunnels = tunnels.Append(tunnel with {Established = false}).ToArray()});
                    return true;
                }
            }

            return false;
        }

        public bool DetachTunnel(string clientId, string tunnelId)
        {
            if (string.IsNullOrWhiteSpace(tunnelId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return false;
            }
            
            if (_clients.FirstOrDefault(c => c.ClientId == clientId) is { } client)
            {
                if (client.Tunnels.FirstOrDefault(t => t.TunnelId == tunnelId) is {} tunnel)
                {
                    UpdateClient(client with {Tunnels = client.Tunnels.Except(new []{tunnel}).ToArray()});
                    return true;
                }
            }
            return false;
        }
    }
}