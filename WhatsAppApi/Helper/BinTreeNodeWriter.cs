using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Helper
{
    public class BinTreeNodeWriter
    {
        private List<byte> buffer;
        public KeyStream Key;

        public BinTreeNodeWriter()
        {
            buffer = new List<byte>();
        }

        public byte[] StartStream(string domain, string resource)
        {
            var attributes = new List<KeyValue>();
            this.buffer = new List<byte>();

            attributes.Add(new KeyValue("to", domain));
            attributes.Add(new KeyValue("resource", resource));
            this.writeListStart(attributes.Count * 2 + 1);

            this.buffer.Add(1);
            this.writeAttributes(attributes.ToArray());

            byte[] ret = this.flushBuffer();
            this.buffer.Add((byte)'W');
            this.buffer.Add((byte)'A');
            this.buffer.Add(0x1);
            this.buffer.Add(0x6);
            this.buffer.AddRange(ret);
            ret = buffer.ToArray();
            this.buffer = new List<byte>();
            return ret;
        }

        public byte[] Write(ProtocolTreeNode node, bool encrypt = true)
        {
            if (node == null)
            {
                this.buffer.Add(0);
            }
            else
            {
                if (WhatsApp.DEBUG && WhatsApp.DEBUGOutBound)
                    this.DebugPrint(node.NodeString("tx "));
                this.writeInternal(node);
            }
            return this.flushBuffer(encrypt);
        }

        protected byte[] flushBuffer(bool encrypt = true)
        {
            byte[] data = this.buffer.ToArray();
            byte[] data2 = new byte[data.Length + 4];
            Buffer.BlockCopy(data, 0, data2, 0, data.Length);

            byte[] size = this.GetInt24(data.Length);
            if (encrypt && this.Key != null)
            {
                byte[] paddedData = new byte[data.Length + 4];
                Array.Copy(data, paddedData, data.Length);
                this.Key.EncodeMessage(paddedData, paddedData.Length - 4, 0, paddedData.Length - 4);
                data = paddedData;

                //add encryption signature
                uint encryptedBit = 0u;
                encryptedBit |= 8u;
                long dataLength = data.Length;
                size[0] = (byte)((ulong)((ulong)encryptedBit << 4) | (ulong)((dataLength & 16711680L) >> 16));
                size[1] = (byte)((dataLength & 65280L) >> 8);
                size[2] = (byte)(dataLength & 255L);
            }
            byte[] ret = new byte[data.Length + 3];
            Buffer.BlockCopy(size, 0, ret, 0, 3);
            Buffer.BlockCopy(data, 0, ret, 3, data.Length);
            this.buffer = new List<byte>();
            return ret;
            
        }

        protected void writeAttributes(IEnumerable<KeyValue> attributes)
        {
            if (attributes != null)
            {
                foreach (var item in attributes)
                {
                    this.writeString(item.Key);
                    this.writeString(item.Value, false);
                }
            }
        }

        private byte[] GetInt16(int len)
        {
            byte[] ret = new byte[2];
            ret[0] = (byte)((len & 0xff00) >> 8);
            ret[1] = (byte)(len & 0x00ff);
            return ret;
        }

        private byte[] GetInt24(int len)
        {
            byte[] ret = new byte[3];
            ret[0] = (byte)((len & 0xf0000) >> 16);
            ret[1] = (byte)((len & 0xff00) >> 8);
            ret[2] = (byte)(len & 0xff);
            return ret;
        }

        protected void writeBytes(string bytes, bool packed)
        {
            writeBytes(WhatsApp.SYSEncoding.GetBytes(bytes), packed);
        }
        protected void writeBytes(byte[] bytes, bool packed = false)
        {
            byte[] toWrite = bytes;

            int len = bytes.Length;
            if (len >= 0x100000)
            {
                this.buffer.Add(254);
                this.writeInt31(len);
            }
            else if (len >= 0x100)
            {
                this.buffer.Add(253);
                this.writeInt20(len);
            }

            byte[] r = null;
            if (packed)
            {
                if (len < 128)
                {
                    r = this.tryPackAndWriteHeader(255, bytes);
                    if (r == null)
                    {
                        r = this.tryPackAndWriteHeader(251, bytes);
                    }
                }
            }

            if (r == null)
            {
                this.buffer.Add(252);
                this.writeInt8(len);
            } else
            {
                toWrite = r;
            }

            this.buffer.AddRange(toWrite);
        }

        protected void writeInt16(int v)
        {
            this.buffer.Add((byte)((v & 0xff00) >> 8));
            this.buffer.Add((byte)(v & 0x00ff));
        }

        protected void writeInt24(int v)
        {
            this.buffer.Add((byte)((v & 0xff0000) >> 16));
            this.buffer.Add((byte)((v & 0x00ff00) >> 8));
            this.buffer.Add((byte)(v & 0x0000ff));
        }

        protected void writeInt20(int v)
        {
            this.buffer.Add((byte)((v & 0x0f0000) >> 16));
            this.buffer.Add((byte)((v & 0x00ff00) >> 8));
            this.buffer.Add((byte)(v & 0x0000ff));
        }

        protected void writeInt31(int v)
        {
            this.buffer.Add((byte)((v & 0x7f000000) >> 24));
            this.buffer.Add((byte)((v & 0xff0000) >> 16));
            this.buffer.Add((byte)((v & 0x00ff00) >> 8));
            this.buffer.Add((byte)(v & 0x0000ff));
        }

        protected void writeInt8(int v)
        {
            this.buffer.Add((byte)(v & 0xff));
        }

        protected void writeInternal(ProtocolTreeNode node)
        {
            int len = 1;
            if (node.attributeHash != null)
            {
                len += node.attributeHash.Count() * 2;
            }
            if (node.children.Any())
            {
                len += 1;
            }
            if (node.data.Length > 0)
            {
                len += 1;
            }
            this.writeListStart(len);
            this.writeString(node.tag);
            this.writeAttributes(node.attributeHash);
            if (node.data.Length > 0)
            {
                this.writeBytes(node.data);
            }
            if (node.children != null && node.children.Any())
            {
                this.writeListStart(node.children.Count());
                foreach (var item in node.children)
                {
                    this.writeInternal(item);
                }
            }
        }
        protected void writeJid(string user, string server)
        {
            this.buffer.Add(250);
            if (user.Length > 0)
            {
                this.writeString(user, true);
            }
            else
            {
                this.writeToken(0);
            }
            this.writeString(server);
        }

        protected void writeListStart(int len)
        {
            if (len == 0)
            {
                this.buffer.Add(0x00);
            }
            else if (len < 256)
            {
                this.buffer.Add(0xf8);
                this.writeInt8(len);
            }
            else
            {
                this.buffer.Add(0xf9);
                this.writeInt16(len);
            }
        }

        protected void writeString(string tag, bool packed = false)
        {
            int intValue = -1;
            bool secondary = false;
            if (new TokenDictionary().TryGetToken(tag, ref secondary, ref intValue))
            {
                if (secondary)
                {
                    this.writeToken(236);
                }
                this.writeToken(intValue);
                return;
            }
            int num2 = tag.IndexOf('@');
            if (num2 < 1)
            {
                this.writeBytes(tag, packed);
                return;
            }
            string server = tag.Substring(num2 + 1);
            string user = tag.Substring(0, num2);
            this.writeJid(user, server);
        }

        protected void writeToken(int token)
        {
            if (token <= 255 && token >= 0)
            {
                this.buffer.Add((byte)token);
            }
            else 
            {
                throw new ArgumentException("Invalid token");
            }
        }

        protected void DebugPrint(string debugMsg)
        {
            if (WhatsApp.DEBUG && debugMsg.Length > 0)
            {
                Helper.DebugAdapter.Instance.fireOnPrintDebug(debugMsg);
            }
        }

        protected byte[] tryPackAndWriteHeader(byte v, byte[] data)
        {
            int len = data.Length;
            if (len >= 128)
            {
                return null;
            }

            byte[] arr = new byte[(int)((len + 1) / 2)];
            for (int i=0; i < len; i++)
            {
                int packByte = this.packByte(v, data[i]);
                if (packByte == -1) {
                    Array.Clear(arr, 0, arr.Length);
                    break;
                }
                int n2 = (int)(i / 2);
                arr[n2] |= (byte)(packByte << 4 * (1 - i % 2));
            }

            if (arr.Length > 0)
            {
                if (len % 2 == 1)
                {
                    arr[arr.Length - 1] |= 15;
                }
                this.buffer.Add(v);
                this.writeInt8(len % 2 << 7 | arr.Length);
                return arr;
            }

            return null;
        }

        protected int packByte(int v, byte n2)
        {
            if (v == 251)
            {
                return this.packHex(n2);
            }
            if (v == 255)
            {
                return this.packNibble(n2);
            }
            return -1;
        }

        protected int packHex(int n)
        {
            if (n >= 48 && n < 58)
            {
                return n - 48;
            }
            if (n >= 65 && n < 71)
            {
                return 10 + (n - 65);
            }
            return -1;
        }

        protected int packNibble(int n)
        {
            if (n == 45 || n == 46)
            {
                return 10 + (n - 45);
            }
            if (n >= 48 && n < 58)
            {
                return n - 48;
            }
            return -1;
        }
    }
}
