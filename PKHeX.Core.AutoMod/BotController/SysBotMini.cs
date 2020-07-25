﻿using System.Net.Sockets;
using System.Threading;

namespace PKHeX.Core.AutoMod
{
    public class SysBotMini : ICommunicator
    {
        public string IP = "192.168.1.65";
        public int Port = 6000;

        public Socket Connection = new Socket(SocketType.Stream, ProtocolType.Tcp);

        public bool Connected;

        private readonly object _sync = new object();

        bool ICommunicator.Connected { get => Connected; set => Connected = value; }
        int ICommunicator.Port { get => Port; set => Port = value; }
        string ICommunicator.IP { get => IP; set => IP = value; }


        public void Connect()
        {
            lock (_sync)
            {
                Connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
                Connection.Connect(IP, Port);
                Connected = true;
            }
        }

        public void Disconnect()
        {
            lock (_sync)
            {
                Connection.Disconnect(false);
                Connected = false;
            }
        }

        private int ReadInternal(byte[] buffer)
        {
            int br = Connection.Receive(buffer, 0, 1, SocketFlags.None);
            while (buffer[br - 1] != (byte)'\n')
                br += Connection.Receive(buffer, br, 1, SocketFlags.None);
            return br;
        }

        private int SendInternal(byte[] buffer) => Connection.Send(buffer);

        public int Read(byte[] buffer)
        {
            lock (_sync)
                return ReadInternal(buffer);
        }

        public byte[] ReadBytes(uint offset, int length)
        {
            lock (_sync)
            {
                var cmd = SwitchCommand.Peek(offset, length);
                SendInternal(cmd);

                // give it time to push data back
                Thread.Sleep((length / 256) + 100);
                var buffer = new byte[(length * 2) + 1];
                var _ = ReadInternal(buffer);
                return Decoder.ConvertHexByteStringToBytes(buffer);
            }
        }

        public void WriteBytes(byte[] data, uint offset)
        {
            lock (_sync)
            {
                SendInternal(SwitchCommand.Poke(offset, data));

                // give it time to push data back
                Thread.Sleep((data.Length / 256) + 100);
            }
        }
    }
}