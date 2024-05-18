using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace MQTT
{
    class AsyncEvent : Dictionary<Action<IAsyncResult>, ManualResetEvent>
    {
        public AsyncEvent()
        {
        }
        new public ManualResetEvent this[Action<IAsyncResult> index]
        {
            get
            {
                ManualResetEvent e;
                if (TryGetValue(index, out e) == false)
                {
                    base.Add(index, e = new ManualResetEvent(false));
                }
                return e;
            }
        }
        public AsyncCallback Get(Action<IAsyncResult> index)
        {
            return new AsyncCallback(index);
        }
    }

    public enum ConnectionState
    {
        Error = -1, Stoped, Busy, Connected
    };
    public class Client
    {
        #region SERVER
        Socket _socket;
        private AsyncEvent _async = new AsyncEvent();
        IPEndPoint _remoteEP;
        public string Host { get; set; }
        public int Port { get; set; } = 1883;

        ConnectionState _state;
        public ConnectionState ConnectionState
        {
            get => _state;
            protected set
            {
                if (_state != value)
                {
                    _state = value;
                    RaiseConnectionChanged();
                }
            }
        }
        public bool IsConnected
        {
            get => _state == ConnectionState.Connected;
        }
        public int ConnectTimeout { get; set; } = 3; // second(s)
        #endregion

        #region CLIENT
        public string ID { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int KeepAlive { get; set; } = 60; // second(s)
        #endregion

        #region CONNECTION
        void on_connected(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);
                _mqtt_connect();

                Task.Run(create_threading);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private void on_disconnect(IAsyncResult ar)
        {
            _socket.EndDisconnect(ar);

            ConnectionState = ConnectionState.Stoped;
        }
        private void _mqtt_connect()
        {
            Send(Packet.Connect(ID, UserName, Password, (int)(KeepAlive)));
        }
        private void _mqtt_disconnect()
        {
            Send(Packet.Disconnect());
        }
        public void Connect()
        {
            if (IsConnected) return;

            if (_remoteEP == null)
            {
                IPAddress ip = null;
                try
                {
                    ip = IPAddress.Parse(Host);
                }
                catch
                {
                    try
                    {
                        ip = Dns
                            .GetHostEntry(Host == null ? Dns.GetHostName() : Host)
                            .AddressList[0];
                    }
                    catch
                    {
                        ConnectionState = ConnectionState.Error;
                        ConnectionError?.Invoke();
                    }
                }

                _remoteEP = new IPEndPoint(ip, Port);
            }
            Reconnect();
        }
        public void Reconnect()
        {
            _busy = true;

            _socket = new Socket(_remoteEP.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            _socket.BeginConnect(_remoteEP,
                _async.Get(on_connected), this);


            int seconds = ConnectTimeout;
            int delay = 100;
            int down = 1000 / delay;

            Task.Run(() => {
                ConnectionState = ConnectionState.Busy;
                while (seconds > 0)
                {
                    if (_socket.Connected) { return; }

                    Thread.Sleep(delay);
                    if (--down == 0)
                    {
                        down = 1000 / delay;
                        seconds--;
                        RaiseConnectionChanged();
                    }
                }

                ConnectionState = ConnectionState.Error;
            });
        }
        public void Disconnect()
        {
            if (_socket.Connected)
            {
                _busy = true;
                _mqtt_disconnect();
                _socket.BeginDisconnect(true, new AsyncCallback(on_disconnect), null);
            }
        }
        #endregion

        #region EVENTS
        public event Action ConnectionError;
        public event Action Connecting;
        public event Action Connected;
        public event Action Disconnected;
        public event Action ConnectionChanged;
        protected virtual void RaiseConnectionChanged()
        {
            ConnectionChanged?.Invoke();

            switch (_state)
            {
                case ConnectionState.Busy: Connecting?.Invoke(); return;

                case ConnectionState.Error: ConnectionError?.Invoke(); return;

                case ConnectionState.Stoped:
                    Disconnected?.Invoke();
                    return;

                case ConnectionState.Connected:
                    Connected?.Invoke();
                    return;
            }
        }

        public event Action<string, byte[]> DataReceived;
        protected virtual void RaiseDataRecieved(string topic, byte[] message)
        {
            DataReceived?.Invoke(topic, message);
        }

        protected virtual void OnResponseReceived(byte code, int length, byte[] buffer)
        {
            if (code == 0x20)
            {
                ConnectionState = ConnectionState.Connected;
                return;
            }

            if (code == 0x30)
            {
                int len = (buffer[0] << 8) | buffer[1];
                string topic = Encoding.UTF8.GetString(buffer, 2, len);

                int i = len + 2;
                byte[] data = new byte[buffer.Length - i];
                for (int k = 0; k < data.Length; k++)
                {
                    data[k] = buffer[i++];
                }
                RaiseDataRecieved(topic, data);
                return;
            }
        }
        #endregion

        #region RECEIVE
        bool _busy;
        byte[] one_byte = new byte[1];
        byte read_byte()
        {
            _socket.Receive(one_byte, 1, 0);
            return one_byte[0];
        }

        async Task create_threading()
        {
            Action disc = () => {
                _socket.Shutdown(SocketShutdown.Both);
                ConnectionState = ConnectionState.Stoped;
            };

            await Task.Run(() => {
                while (_state != ConnectionState.Stoped)
                {
                    if (_busy) continue; // Đang gửi lên server

                    Thread.Sleep(10);

                    if (_socket.Available == 0 && _socket.Poll(1, SelectMode.SelectRead))
                    {
                        disc(); return;
                    }

                    try
                    {
                        byte code = read_byte();

                        switch (code)
                        {
                            case 0x00: continue;
                            case 0x70: disc(); return;
                        }

                        var len = new RemainingLength();
                        while (len.Read(read_byte())) ;

                        int size = len.GetValue();
                        byte[] remainingData = new byte[size];
                        for (int i = 0; i < size; i++)
                        {
                            remainingData[i] = read_byte();
                        }

                        OnResponseReceived(code, size, remainingData);
                    }
                    catch
                    {
                    }
                }
            });
        }
        public string subscribe_topic;
        public byte subscribe_qos;
        public void Subscribe(string topic, byte qos)
        {
            Task.Run(() => Send(Packet.Subscribe(subscribe_topic = topic, subscribe_qos = qos)));
        }
        public void Subscribe(string topic)
        {
            Subscribe(topic, 0);
        }

        public void Unsubscribe(string topic)
        {
            Task.Run(() => Send(Packet.Unsubcribe(topic)));
        }
        #endregion

        #region SEND
        private void _debug(Packet packet, bool endl)
        {
            StringBuilder builder = new StringBuilder();
            foreach (byte[] e in packet)
            {
                foreach (var b in e)
                {
                    builder.AppendFormat("{0:X2} ", b);
                }
            }

            if (endl) builder.Append('\n');
        }
        void Send(Packet packet)
        {
            if (_socket.Connected == false) return;

            _busy = true;
            try
            {
                _socket.Send(packet.ToBytes());
            }
            catch
            {
            }
            _busy = false;
        }
        #endregion

        #region CONSTRUCTORS
        public Client(string id, string host, int port)
        {
            ID = id ?? Guid.NewGuid().ToString();
            Host = host;
            Port = port;
        }
        public Client(string id, string host, int port, string username, string password)
            : this(id, host, port)
        {
            UserName = username;
            Password = password;
        }
        public Client(string host, int port)
            : this(null, host, port)
        {
        }
        public Client(string host) : this(host, 1883)
        {
        }
        #endregion

        public void Ping(int seconds)
        {
            if (seconds == 0)
            {
                Send(Packet.Ping());
                return;
            }

            Task.Run(() => {
                while (IsConnected)
                {
                    Thread.Sleep(seconds * 1000);
                    Send(Packet.Ping());
                }
            });
        }

        #region PUBLISH
        public void Publish(string topic, byte[] message, byte qos, bool retain)
        {
            Send(Packet.Publish(topic, message, qos, retain));
        }
        public void Publish(string topic, string message, byte qos, bool retain)
        {
            Publish(topic, Encoding.UTF8.GetBytes(message), qos, retain);
        }
        public void Publish(string topic, byte[] message, byte qos)
        {
            Publish(topic, message, qos, false);
        }
        public void Publish(string topic, string message, byte qos)
        {
            Publish(topic, message, qos, false);
        }
        public void Publish(string topic, byte[] message)
        {
            Publish(topic, message, 0, false);
        }
        public void Publish(string topic, string message)
        {
            Publish(topic, message, 0, false);
        }
        #endregion
    }
}
