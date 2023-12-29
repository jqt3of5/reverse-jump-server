using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using JumpServer.Busi;
using JumpServer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Shared.Models;

namespace JumpServer.Services;

public class TunnelService : IHostedService
{
    private readonly ILogger<TunnelService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TunnelRepo _repo;
    private readonly JumpServerRepo _jumpServerRepo;
    private Dictionary<string, SshClient> _sshClients = new Dictionary<string, SshClient>();

    //TODO: Need a cron that will check the status of the tunnels (And attempt to reconnect?)
    public TunnelService(ILogger<TunnelService> logger, IConfiguration configuration, TunnelRepo repo, JumpServerRepo jumpServerRepo)
    {
        _logger = logger;
        _configuration = configuration;
        _repo = repo;
        _jumpServerRepo = jumpServerRepo;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var tunnels = _configuration.GetSection("Tunnels").Get<List<ConfiguredTunnel>>();
        if (tunnels == null)
        {
            _logger.Log(LogLevel.Warning, "No tunnels to start");
            return Task.CompletedTask;
        }

        //TODO: Can I redo this when the config changes?
        foreach (var tunnel in tunnels)
        {
            try
            {
                var client = ConnectToServer(tunnel.JumpServerAddress, tunnel.JumpServerPort, tunnel.JumpServerUser, tunnel.KeyFile);
                var t = StartTunnel(client, tunnel.RemotePort, tunnel.LocalPort, tunnel.Host, tunnel.TunnelName);
                
                _logger.Log(LogLevel.Information, $"Tunnel started with Id: {t.TunnelId}: {t.RemotePort}:{t.Host}:{t.LocalPort}");
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e, $"Exception while starting tunnel, {tunnel.RemotePort}:{tunnel.Host}:{tunnel.LocalPort}");
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var tunnel in _repo.GetAllTunnels())
        {
            
            if (tunnel.TunnelId != null && _sshClients.TryGetValue(tunnel.TunnelId, out var client))
            {
                client.Dispose();
                _sshClients.Remove(tunnel.TunnelId);
            }

            _repo.DeleteTunnel(tunnel.TunnelId);
        }
        return Task.CompletedTask;
    }

    private SshClient ConnectToServer(string jumpServerAddress, uint jumpServerPort, string jumpServerUser,
        string keyfile)
    {
        
        //Are we already connected?
        if (_sshClients.TryGetValue($"{jumpServerUser}@{jumpServerAddress}:{jumpServerPort}", out var c))
        {
            if (!c.IsConnected)
            {
                c.Connect();
            }
            
            return c;
        }
        
        if (!File.Exists(keyfile))
        {
            throw new ArgumentException($"key file {keyfile} does not exist. Tunnel was not started");
        }

        //Make sure the repo is uptodate
        if (!_jumpServerRepo.TryGetJumpServer(jumpServerAddress, jumpServerPort, jumpServerUser, out var server))
        {
            server = new JumpServerValueObject("", jumpServerAddress, jumpServerPort, keyfile,
                jumpServerUser, "");
            
            _jumpServerRepo.AddJumpServer(server);
        } 
        
        var connection = new ConnectionInfo(jumpServerAddress,(int)jumpServerPort, jumpServerUser,
            //TODO: Support more than private keys
            new PrivateKeyAuthenticationMethod(jumpServerUser, new PrivateKeyFile(keyfile)));
       
        //TODO: Somehow need to validate that /etc/sshd_config has GatewayPorts yes
        var client = new SshClient(connection);
        
        client.HostKeyReceived += (sender, args) =>
        {
            //The host key hasn't been set, just  set it
            if (string.IsNullOrEmpty(server.HostKey))
            {
                _jumpServerRepo.UpdateJumpServer(server with { HostKey = args.FingerPrintSHA256 });
            }

            _logger.Log(LogLevel.Information, $"Connected to ssh server with host key: {args.HostKey}");
            
            if (_jumpServerRepo.ValidateJumpServer(args.FingerPrintSHA256, server.JumpServerId))
            {
                args.CanTrust = true;
            }
            else
            {
                _logger.Log(LogLevel.Warning, $"Connected to ssh server with host key: {args.HostKey}");
            }
        };
        
        client.ErrorOccurred += (sender, args) =>
        {
            //TODO: Associate these logs and update the connection state somehow
            _logger.Log(LogLevel.Warning, args.Exception, "Exception occurred while connecting to the ssh server");
        };

        client.Connect();
        
        if (!client.IsConnected)
        {
            client.Dispose();
            throw new ApplicationException($"ssh tunnel failed to connect");
        }
        
        _sshClients[$"{jumpServerUser}@{jumpServerAddress}:{jumpServerPort}"] = client;
        
        return client;
    }
    
    public TunnelValueObject StartTunnel(SshClient client, uint remotePort, uint localPort,
        string host, string tunnelName)
    {
        if (!client.IsConnected)
        {
            throw new ArgumentException("Client was not connected");
        }

        //TODO: Is a tunnel for this port/host already open?
        var port = new ForwardedPortRemote(client.ConnectionInfo.Host, remotePort, host, localPort);
        
        var tunnel = new TunnelValueObject("", client.ConnectionInfo.Host, (uint)client.ConnectionInfo.Port, remotePort,
            localPort, host, false, tunnelName); 
        
        port.Exception += (sender, args) =>
        {
            //TODO: Associate these logs and update the connection state somehow
            _logger.Log(LogLevel.Error, args.Exception, "Error while forwarding port");
        };
        port.RequestReceived += (sender, args) =>
        {
            _logger.Log(LogLevel.Information, $"Connection from client: {args.OriginatorHost}:{args.OriginatorPort}");
        };
        
        // proc.StartInfo.Arguments = $" -nT -o ServerAliveInterval=60 -o ServerAliveCountMax=3 -o StrictHostKeyChecking=accept-new -i {keyfile} -R {remotePort}:{host}:{localPort} {jumpServerUser}@{jumpServerAddress} -p {jumpServerPort}";
        
        tunnel = _repo.AddUpdateTunnel(tunnel);
        
        client.AddForwardedPort(port);
        
        port.Start();

        if (!port.IsStarted)
        {
            throw new ApplicationException($"Port failed to forward"); 
        }

        _repo.AddUpdateTunnel(tunnel with { Established = true });

        return tunnel;
    }
}