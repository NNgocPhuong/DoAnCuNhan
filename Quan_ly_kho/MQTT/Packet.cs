using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT
{
    class RemainingLength : List<byte>
    {
        public RemainingLength() { }
        public RemainingLength(int value)
        {
            SetValue(value);
        }
        public int GetValue()
        {
            int v = 0;
            foreach (byte e in this)
            {
                v = (v << 7) | e;
            }
            return v;
        }
        public void SetValue(int x)
        {
            while (true)
            {
                int e = x & 127;
                x >>= 7;

                if (x == 0)
                {
                    Add((byte)e);
                    break;
                }

                Add((byte)(e | 128));
            }
        }
        public bool Read(byte e)
        {
            this.Insert(0, (byte)(e & 0x7F));
            return (e & 0x80) != 0;
        }
    }
    public class Packet : List<byte[]>
    {
        public Packet(int code)
        {
            Push((byte)code);
            Push((byte)0);
        }
        public Packet(int type, int reserved) : this((type << 4) | reserved) { }
        public void Push(int value)
        {
            base.Add(new byte[] { (byte)(value >> 8), (byte)value });
        }
        public void Push(byte b)
        {
            base.Add(new byte[] { b });
        }
        public void Push(string src)
        {
            this.Push(Encoding.UTF8.GetBytes(src));
        }
        public void Push(byte[] data)
        {
            this.Push(data, true);
        }
        public void Push(byte[] data, bool length)
        {
            if (length) { this.Push(data.Length); }
            base.Add(data);
        }
        public byte[] ToBytes()
        {
            var lst = new List<byte>();
            foreach (var e in this)
            {
                lst.AddRange(e);
            }
            return lst.ToArray();
        }
        Packet CalcRemaining()
        {
            int v = 0;
            foreach (var s in this)
            {
                v += s.Length;
            }
            this[1] = new RemainingLength(v - 2).ToArray();
            return this;
        }
        static public Packet Connect(string id, string un, string pw, int keep)
        {
            var p = new Packet(1, 0);
            p.Push("MQTT");
            p.Push(0x0402);
            p.Push(keep);
            p.Push(id);
            if (un != null)
            {
                p.Push(un);
                if (pw != null) p.Push(pw);
            }
            return p.CalcRemaining();
        }
        static public Packet Ping()
        {
            return new Packet(12, 0);
        }
        static public Packet PingResponse()
        {
            return new Packet(13, 0);
        }
        static public Packet Disconnect()
        {
            return new Packet(14, 0);
        }
        static public Packet Subscribe(string topic, byte qos)
        {
            Packet p = new Packet(8, 2);
            p.Push(0);
            p.Push(topic);
            p.Push(qos);
            return p.CalcRemaining();
        }
        static public Packet Publish(string topic, byte[] message, byte qos, bool retain)
        {
            Packet p = new Packet(3, (qos << 4) | (retain ? 1 : 0));
            p.Push(topic);
            p.Add(message);
            return p.CalcRemaining();
        }
        static public Packet Connack()
        {
            var p = new Packet(2, 0);
            p.Push(0);
            return p.CalcRemaining();
        }
        static public Packet Suback(byte code)
        {
            var p = new Packet(9, 0);
            p.Push(0);
            p.Push(code);
            return p.CalcRemaining();
        }
        static public Packet Puback()
        {
            var p = new Packet(4, 0);
            p.Push(0);
            return p.CalcRemaining();
        }
        static public Packet Pubrec()
        {
            var p = new Packet(5, 0);
            p.Push(0);
            return p.CalcRemaining();
        }
        static public Packet Pubrel()
        {
            var p = new Packet(6, 1);
            p.Push(0);
            return p.CalcRemaining();
        }
        static public Packet Pubcomp()
        {
            var p = new Packet(7, 0);
            p.Push(0);
            return p.CalcRemaining();
        }
        static public Packet Unsubcribe(string topic)
        {
            Packet p = new Packet(10, 2);
            p.Push(0);
            p.Push(topic);
            return p.CalcRemaining();
        }
        static public Packet Unsuback()
        {
            Packet p = new Packet(11, 0);
            p.Push(0);

            return p.CalcRemaining();
        }
    }
}
