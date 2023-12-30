using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Schema;
using Shared.Models;

namespace JumpServer.Busi;

public class JumpServerRepo
{
    private readonly string _path;
    private Dictionary<string, JumpServerValueObject> _servers = new Dictionary<string, JumpServerValueObject>();

    public JumpServerRepo(string path = "servers.json")
    {
        _path = path;
        var servers = Load(path);
        foreach (var server in servers)
        {
            _servers[server.JumpServerId] = server;
        }
    }

    public IEnumerable<JumpServerValueObject> GetAllJumpServers()
    {
        return _servers.Values;
    }
    
    public JumpServerValueObject AddJumpServer(JumpServerValueObject jumpServer)
    {
        //Does the server already exist?
        if (TryGetJumpServer(jumpServer.IpAddress, jumpServer.SshPort, jumpServer.UserName, out var server))
        {
            return server;
        }
        
        var serverId = string.IsNullOrEmpty(jumpServer.JumpServerId) ? Guid.NewGuid().ToString() : jumpServer.JumpServerId;
        var t = jumpServer with { JumpServerId = serverId};

        _servers[serverId] = t;
        Save();

        return t; 
    }

    public bool UpdateJumpServer(JumpServerValueObject jumpServer)
    {
        //Lookup by ID first if we can
        if (!string.IsNullOrEmpty(jumpServer.JumpServerId) && _servers.TryGetValue(jumpServer.JumpServerId, out var j))
        {
            _servers[jumpServer.JumpServerId] = jumpServer;
            Save();
            return true;
        }
        //Look up the server by it's address
        else if (TryGetJumpServer(jumpServer.IpAddress, jumpServer.SshPort, jumpServer.UserName, out var k))
        {
            _servers[k.JumpServerId] = jumpServer  with {JumpServerId = k.JumpServerId};
            Save();
            return true;
        }

        return false;
    }
    
    public bool TryGetJumpServer(string address, uint port, string username, [MaybeNullWhen(false)] out JumpServerValueObject? jumpServer)
    {
        foreach (var server in _servers.Values)
        {
            if (server.IpAddress == address && server.SshPort == port && server.UserName == username)
            {
                jumpServer = server;
                return true;
            }
        }

        jumpServer = null;
        return false;
    }

    private List<JumpServerValueObject> Load(string path)
    {
        if (!File.Exists(path))
        {
            return new List<JumpServerValueObject>();
        }
        
        using (var file = File.Open(path, FileMode.Open))
        {
            return JsonSerializer.Deserialize<List<JumpServerValueObject>>(file);
        }
    }

    private void Save()
    {
        Save(_path, _servers.Values);
    }

    private void Save(string path, IEnumerable<JumpServerValueObject> servers)
    {
        using (var file = File.Open(path, FileMode.OpenOrCreate))
        {
            JsonSerializer.Serialize(file, servers);
        }
    }
}