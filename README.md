# Reverse-Jump Server Manager
I wanted to host a home assistant instance from a raspberry pi running in my house. I got it installed, and it worked
great until I decided that I wanted it to be accessible outside of my home LAN. Well, much to my dismay I learned that
my ISP was double NATing me. This basically means there is no hope of me simply hosting a server accessible to the outside world. So, 
I turned to my next best option: the Reverse Jump Sever. 

A jump server is typically an ssh server that is used as an entry point to other devices on the internal network. In the case of a reverse jump server, 
the ssh server sits outside of the network (in an EC2 instance in AWS) and then some machine running this service will establish an 
ssh connection with the external server, setting up remote port forwarding. Allowing traffic through the ssh tunnel into the internal
network. 

## Config File

The tunnels are configured through the use of a config file. The service will search for this config file in the following order:

* A path specified in appsettings.json:"Config"
* \<BaseDirectory>/jump.json
* /etc/jump/jump.json
* \<CWD>/jump.json

A typical format would look like:

```json
{
  "Tunnels" : [
    {
      "JumpServerAddress": "jump.myhost.com",
      "JumpServerPort": 22,
      "JumpServerUser": "user",
      "RemotePort": 1234,
      "LocalPort": 443,
      "Host": "google.com",
      "KeyFile": "/keys/key.pem",
      "TunnelName": "TestTunnel"
    }
  ]
}
```

## Configuring the ssh server

There are two major things that need to be configured on the ssh server side. 

### 1. Set "GatewayPorts yes"

In the sshd config file (/etc/ssh/sshd_config) you need to ensure "GatewayPorts" is set to yes. This allows ports that have been remotely forwaded to be
accessible outside of the machine running the sshd server. 

### 2. Allow ssh key access for the root user

I know this is terrible practice.... But OpenSSH hard blocks port forwarding for < 1024 unless the user is root. So
if you want to create tunnels for these ports, you'll need to allow root to ssh in. At somepoint I might develop a solution with sudo/socat to forward these ports
without direct root access. 

### 3. Create a separate sshd instance (If using AWS)

When using spot instances, and your EC2 instance restarts, the host keys will change every time. I couldn't figure out a nice way
to fix this, so as an alternative I created a duplicate sshd instance (duplicating the systemd files, and the sshd_config file) pointing to a separate
set of host keys that did not change. 


## Release Notes:

### 1.0.0

* Tunnels can be configured through the jump.json file.
* Servers are stored in servers.json with their host key for validation