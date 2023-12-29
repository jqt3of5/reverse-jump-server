using System;
using System.Collections.Generic;
using JumpServer.Models;

namespace JumpServer.Busi
{
    public class TunnelRepo
    {
        private Dictionary<string, TunnelValueObject> _tunnels = new Dictionary<string, TunnelValueObject>();
        public TunnelValueObject AddUpdateTunnel(TunnelValueObject tunnel)
        {
            var tunnelId = string.IsNullOrEmpty(tunnel.TunnelId) ? Guid.NewGuid().ToString() : tunnel.TunnelId;
            var t = tunnel with { TunnelId = tunnelId };  
            _tunnels[tunnelId] = t;

            return t;
        }

        public TunnelValueObject? GetTunnel(string tunnelId)
        {
            if (_tunnels.TryGetValue(tunnelId, out var value))
            {
                return value;
            }

            return null;
        }

        public bool DeleteTunnel(string tunnelId)
        {
            if (_tunnels.ContainsKey(tunnelId))
            {
                _tunnels.Remove(tunnelId);
                return true;
            }

            return false;
        }

        public IEnumerable<TunnelValueObject> GetAllTunnels()
        {
            return _tunnels.Values;
        }
    }
}