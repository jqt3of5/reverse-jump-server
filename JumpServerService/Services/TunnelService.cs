using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    private IConfiguration _tunnelConfiguration;
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

    private IConfiguration LoadTunnelConfiguration()
    {
        var builder = new ConfigurationBuilder();
        
        _logger.Log(LogLevel.Information, "Checking appsettings.json for config path....");
        if (_configuration["Config"] is { } configPath && File.Exists(configPath))
        {
            _logger.Log(LogLevel.Information, $"appsettings.json had config path {configPath}....");
            builder.AddJsonFile(configPath, optional: false);
            return builder.Build();
        }

        var basePath = Path.Combine(AppContext.BaseDirectory, "jump.json");
        _logger.Log(LogLevel.Information, $"Checking {basePath} for config file....");
        if (File.Exists(basePath))
        {
            _logger.Log(LogLevel.Information, $"found config file at {basePath}....");
            builder.AddJsonFile(basePath, optional: false);
            return builder.Build();
        }

        if (OperatingSystem.IsLinux())
        {
            var etcPath = "/etc/jump/jump.json";
            _logger.Log(LogLevel.Information, $"Checking {etcPath} for config file....");
            if (File.Exists(etcPath))
            {
                _logger.Log(LogLevel.Information, $"Found config file at {etcPath}....");
                builder.AddJsonFile(etcPath, optional: false);
                return builder.Build();
            }
        }
        
        var cwdPath = Path.Combine(Directory.GetCurrentDirectory(), "jump.json");
        _logger.Log(LogLevel.Information, $"Checking {cwdPath} for config file....");
        if (File.Exists(cwdPath))
        {
            _logger.Log(LogLevel.Information, $"found config file at {cwdPath}....");
            builder.AddJsonFile(cwdPath, optional: false);
            return builder.Build();
        }

        throw new FileNotFoundException("Could not find config file in any of the search paths");
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        //TODO: Can I redo this when the config changes?
        _tunnelConfiguration = LoadTunnelConfiguration();
        
        var tunnels = _tunnelConfiguration.GetSection("Tunnels").Get<List<ConfiguredTunnel>>();
        if (tunnels == null)
        {
            _logger.Log(LogLevel.Warning, "No tunnels to start");
            return Task.CompletedTask;
        }

        foreach (var tunnel in tunnels)
        {
            try
            {
                var client = ConnectToServer(tunnel.JumpServerAddress, tunnel.JumpServerPort, tunnel.JumpServerUser, tunnel.KeyFile);
                var t = StartTunnel(client, tunnel.RemotePort, tunnel.LocalPort, tunnel.Host, tunnel.TunnelName);
                
                _logger.Log(LogLevel.Information, $"Tunnel started with Id: {t.TunnelId}: {t.RemotePort}:{t.Host}:{t.LocalPort} through jump server: {tunnel.JumpServerAddress}:{tunnel.JumpServerPort}");
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e, $"Exception while starting tunnel, {tunnel.RemotePort}:{tunnel.Host}:{tunnel.LocalPort} through jump server: {tunnel.JumpServerAddress}:{tunnel.JumpServerPort}");
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
        if (_sshClients.TryGetValue($"{jumpServerAddress}:{jumpServerPort}", out var c))
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

            _logger.Log(LogLevel.Information, $"Connected to ssh server {server.IpAddress}:{server.SshPort} with host key: {args.FingerPrintSHA256}");
            
            if (args.FingerPrintSHA256 == server.HostKey)
            {
                args.CanTrust = true;
            }
            else
            {
                _logger.Log(LogLevel.Warning, $"Warning host key: {args.FingerPrintSHA256} differs from what was expected: {server.HostKey}");
            }
        };
        
        client.ErrorOccurred += (sender, args) =>
        {
            //TODO: Associate these logs and update the connection state somehow
            _logger.Log(LogLevel.Warning, args.Exception, $"Exception occurred while connecting to the ssh server {server.IpAddress}:{server.SshPort}");
        };

        client.Connect();
        
        if (!client.IsConnected)
        {
            client.Dispose();
            throw new ApplicationException($"ssh tunnel failed to connect to {server.IpAddress}:{server.SshPort}");
        }
        
        _sshClients[$"{jumpServerAddress}:{jumpServerPort}"] = client;
        
        return client;
    }
    
    public TunnelValueObject StartTunnel(SshClient client, uint remotePort, uint localPort,
        string host, string tunnelName)
    {
        if (!client.IsConnected)
        {
            throw new ArgumentException("Client was not connected");
        }

        if (client.ConnectionInfo.Username != "root" && remotePort < 1024)
        {
            throw new ArgumentException("Only root users can forward remote ports < 1024");
        }

        //TODO: Is a tunnel for this port/host already open?
        var port = new ForwardedPortRemote(client.ConnectionInfo.Host, remotePort, host, localPort);
        
        var tunnel = new TunnelValueObject("", client.ConnectionInfo.Host, (uint)client.ConnectionInfo.Port, remotePort,
            localPort, host, false, tunnelName); 
        
        port.Exception += (sender, args) =>
        {
            //TODO: Associate these logs and update the connection state somehow
            _logger.Log(LogLevel.Error, args.Exception, $"Error while forwarding port {remotePort}:{host}:{localPort} through jump server: {client.ConnectionInfo.Host}:{client.ConnectionInfo.Port}");
        };
        port.RequestReceived += (sender, args) =>
        {
            _logger.Log(LogLevel.Debug, $"Connection from client: {args.OriginatorHost}:{args.OriginatorPort} for tunnel: {remotePort}:{host}:{localPort} through jump server: {client.ConnectionInfo.Host}:{client.ConnectionInfo.Port}");
        };
        
        // proc.StartInfo.Arguments = $" -nT -o ServerAliveInterval=60 -o ServerAliveCountMax=3 -o StrictHostKeyChecking=accept-new -i {keyfile} -R {remotePort}:{host}:{localPort} {jumpServerUser}@{jumpServerAddress} -p {jumpServerPort}";
        
        tunnel = _repo.AddUpdateTunnel(tunnel);
        
        client.AddForwardedPort(port);
        
        port.Start();

        if (!port.IsStarted)
        {
            throw new ApplicationException($"Port failed to forward {remotePort}:{host}:{localPort} through jump server: {client.ConnectionInfo.Host}:{client.ConnectionInfo.Port}"); 
        }

        _repo.AddUpdateTunnel(tunnel with { Established = true });

        return tunnel;
    }
}