#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace WakeOnLan
{
    /// <summary>
    /// Manages remote servers, enabled WOL, and detecting their presence
    /// </summary>




    public class RemoteServer
    {
        private IPAddress ipAddress;
        private MacAddress macAddress;
        private string description;

        #region Constructor
        public RemoteServer(string desc, IPAddress ipAddr, MacAddress macAddr)
        {
            Setup(desc, ipAddr, macAddr);
        }
        #endregion

        public static PingReply Ping(string host)
        {
            return Ping(host, 2500);
        }

        public static PingReply Ping(string host, int timeOut)
        {
            return Ping(host, timeOut, 128);
        }

        public static PingReply Ping(string host, int timeOut, int ttl)
        {
            Ping synchronousPing = new Ping();
            PingOptions options = new PingOptions(ttl, true);

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            return synchronousPing.Send(host, timeOut, buffer, options);
        }

        public void Setup(string desc, IPAddress ipAddr, MacAddress macAddr)
        {
            ipAddress = ipAddr;
            macAddress = macAddr;
            description = desc;
        }

        public IPAddress IpAddress
        {
            get
            {
                return ipAddress;
            }
        }

        public MacAddress MacAddress
        {
            get
            {
                return macAddress;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public bool IsAwake()
        {
            //ping the server, if we get a return, the server is awake.
            PingReply reply = Ping(ipAddress.ToString(), 200);
            return (reply.Status == IPStatus.Success);
        }

        public void WakeUp()
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,ProtocolType.Udp);
            IPEndPoint iep1 = new IPEndPoint(IPAddress.Broadcast, 9050);
            IPEndPoint iep2 = new IPEndPoint(IPAddress.Parse("192.168.1.255"), 9050);

            byte[] data = CreateMagicPacket(macAddress);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            sock.SendTo(data, iep1);
            sock.SendTo(data, iep2);
            sock.Close();
        }

        private byte[] CreateMagicPacket(MacAddress macAddress)
        { 
            //Magic packet  contains a 6-bytes trailer and 16 times a 6-bytes sequence containing the MAC address
            byte[] magicPacket = new byte[17 * 6];

            //setup trailer
            for (int i = 0; i < 6; i++)
            {
                magicPacket[i] = 0xFF;
            }

            //now the body
            for (int i = 1; i <= 16; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    magicPacket[i * 6 + j] = macAddress[j];
                }
            }
            return magicPacket;
        }
    }
}
