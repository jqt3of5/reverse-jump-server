using System.Collections.Generic;
using System.Linq;
using JumpServer.Models;

namespace JumpServer.Busi
{
    public class ClientModel
    {
        private List<ClientValueObject> _clients = new();
        public IEnumerable<ClientValueObject> Clients => _clients;

        public void AddClient(ClientValueObject client)
        {
           _clients.Add(client); 
        }

        public void ClientConnected(string connectionId, ClientValueObject? client = null)
        {
              
        }

        public void ClientDisconnected(string connectionId)
        {
            
        }

        public bool AttachTunnel(string clientId, TunnelValueObject tunnel)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return false;
            }
            
            if (_clients.FirstOrDefault(c => c.ClientId == clientId) is { } client)
            {
                _clients.Remove(client);
                _clients.Add(client with {Tunnels = client.Tunnels.Append(tunnel).ToArray()});
                return true;
            }

            return false;
        }

        public bool DetachTunnel(string clientId, int serverPort)
        {
            if (serverPort <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return false;
            }
            
            if (_clients.FirstOrDefault(c => c.ClientId == clientId) is { } client)
            {
                if (client.Tunnels.FirstOrDefault(t => t.ServerPort == serverPort) is {} tunnel)
                {
                    _clients.Remove(client);
                    _clients.Add(client with {Tunnels = client.Tunnels.Except(new []{tunnel}).ToArray()});
                    return true;
                }
            }
            return false;
        }
    }
}