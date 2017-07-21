using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using WhatsAppApi.Response;

namespace WhatsAppApi.Helper
{
    public class BinTreeNodeReader
    {
        public KeyStream Key;
        private List<byte> buffer;
        private bool started = false;

        public BinTreeNodeReader()
        {
            
        }

        protected void reset()
        {
            this.started = false;
        }

        public void SetKey(byte[] key, byte[] mac)
        {
            this.Key = new KeyStream(key, mac);
        }

        protected void streamStart()
        {
            started = true;
            int tag = (int)this.buffer[0];
            this.buffer.RemoveAt(0);

            int size = this.readListSize(tag);
            this.buffer.RemoveAt(0);

            if (tag != -1)
            {
                if (tag == 236)
                {
                    tag = (int)this.buffer[0] + 237;
                    this.buffer.RemoveAt(0);
                }

                string token = this.getToken(tag);
                throw new CorruptStreamException("expecting STREAM_START in streamStart, instead got token: " + token);
            }

            int attribCount = (size - 2 + size % 2) / 2;
            this.readAttributes(attribCount);
        }

        protected int peekInt8(int offset = 0)
        {
            int ret = 0;

            if (this.buffer.Count >= offset + 1)
                ret = this.buffer[offset];

            return ret;
        }

        protected int peekInt16(int offset = 0)
        {
            int ret = 0;
            if (this.buffer.Count >= offset + 2)
            {
                ret = (int)this.buffer[0 + offset] << 8;
                ret |= (int)this.buffer[1 + offset] << 0;
            }
            return ret;
        }

        protected int peekInt24(int offset = 0)
        {
            int ret = 0;
            if (this.buffer.Count >= 3 + offset)
            {
                ret = (this.buffer[0 + offset] << 16) + (this.buffer[1 + offset] << 8) + this.buffer[2 + offset];
            }
            return ret;
        }

        public ProtocolTreeNode nextTree(byte[] pInput = null, bool useDecrypt = true)
        {
            if (pInput != null && pInput.Length > 0)
            {
                this.buffer = new List<byte>();
                this.buffer.AddRange(pInput);
            }

            int firstByte = this.peekInt8();
            int stanzaFlag = (firstByte & 0xF0) >> 4;
            int stanzaSize = this.peekInt16(1) | ((firstByte & 0x0F) << 16);

            int flags = stanzaFlag;
            int size = stanzaSize;

            this.readInt24();

            bool isEncrypted = (stanzaFlag & 8) != 0;

            if (isEncrypted)
            {
                if (this.Key != null)
                {
                    var realStanzaSize = stanzaSize - 4;
                    var macOffset = stanzaSize - 4;
                    var treeData = this.buffer.ToArray();
                    try
                    {
                        this.Key.DecodeMessage(treeData, macOffset, 0, realStanzaSize);
                    }
                    catch (Exception e)
                    {
                        Helper.DebugAdapter.Instance.fireOnPrintDebug(e);
                    }
                    this.buffer.Clear();
                    this.buffer.AddRange(treeData.Take(realStanzaSize).ToArray());
                }
                else
                {
                    throw new Exception("Received encrypted message, encryption key not set");
                }
            }

            if (stanzaSize > 0)
            {
                /*
                if (!this.started)
                {
                    this.streamStart();
                }
                */

                ProtocolTreeNode node = this.nextTreeInternal();
                if (node != null)
                    this.DebugPrint(node.NodeString("rx "));
                return node;
            }

            return null;
        }

        protected string getToken(int token)
        {
            string tokenString = null;
            new TokenDictionary().GetToken(token, ref tokenString);
            if (tokenString == null)
            {
                token = readInt8();
                new TokenDictionary().GetToken(token, ref tokenString);
            }
            return tokenString;
        }

        protected string getTokenDouble(int n, int n2)
        {
            string tokenString = null;
            int pos = n2 + n * 256;
            new TokenDictionary().GetToken(pos, ref tokenString);
            return tokenString;
        }

        protected string readString(int token)
        {
            if (token == -1)
            {
                throw new ArgumentException("-1 token in readString");
            }

            if (token > 2 && token < 236)
            {
                return this.getToken(token);
            }

            if (token == 0)
            {
                return null;
            }

            if (token >= 236 && token <= 239)
            {
                return this.getTokenDouble(token - 236, this.readInt8());
            }

            if (token == 250)
            {
                int tokenUser = this.buffer[0];
                this.buffer.RemoveAt(0);
                string user = this.readString(tokenUser);

                int tokenServer = this.buffer[0];
                this.buffer.RemoveAt(0);
                string server = this.readString(tokenServer);

                if (user != null && server != null)
                {
                    return user + "@" + server;
                }

                if (server != null)
                {
                    return server;
                }

                throw new ArgumentException("readString couldn't reconstruct jid");
            }

            if (token == 251 || token == 255)
            {
                return this.readPacked8(token);
            }

            if (token == 252)
            {
                int size8 = this.readInt8();
                byte[] buff = this.readArray(size8);
                return WhatsApp.SYSEncoding.GetString(buff);
            }

            if (token == 253)
            {
                int size20 = this.readInt20();
                byte[] buff = this.readArray(size20);
                return WhatsApp.SYSEncoding.GetString(buff);
            }

            if (token == 254)
            {
                int size31 = this.readInt31();
                byte[] buff = this.readArray(size31);
                return WhatsApp.SYSEncoding.GetString(buff);
            }

            throw new ArgumentException("readString couldn't match token " + token);
        }
        
        protected string readPacked8(int n)
        {
            int size = readInt8();
            int remove = 0;

            if ((size & 0x80) != 0 && n == 251)
            {
                remove = 1;
            }

            size = size & 0x7f;
            byte[] text = readArray(size);
            string hexData = BitConverter.ToString(text).Replace("-", string.Empty).ToUpper();
            int dataSize = hexData.Length;
            var outResult = new List<char>();
            if (remove == 0)
            {
                for (int i = 0; i < dataSize; i++)
                {
                    char ch = hexData[i];
                    int val = (char) Convert.ToInt32("0" + ch, 16);
                    if ((i == dataSize-1) && val > 11 && n != 251)
                    {
                        continue;
                    }
                    outResult.Add(Convert.ToChar(this.unpackByte(n, val)));
                }
            } else
            {
                outResult.AddRange(hexData.Substring(0, dataSize - 1));
            }
            return new string(outResult.ToArray());
        }

        protected int unpackByte(int n, int n2)
        {
            if (n == 251)
            {
                return this.unpackHex(n2);
            }
            if (n == 255)
            {
                return this.unpackNibble(n2);
            }
            throw new ArgumentException("bad packed type: " + n);
        }

        protected int unpackNibble(int n)
        {
            if (n >= 0 && n < 10)
            {
                return n + 48;
            }
            if (n == 10 || n == 11)
            {
                return 45 + (n - 10);
            }
            throw new ArgumentException("bad nibble: " + n);
        }

        protected int unpackHex(int n)
        {
            if (n >= 0 && n < 10)
            {
                return n + 48;
            }
            if (n >= 10 && n < 16)
            {
                return 65 + (n - 10);
            }
            throw new ArgumentException("bad hex: " + n);
        }

        protected int readHeader(int offset = 0)
        {
            int ret = 0;
            if (this.buffer.Count >= (3 + offset))
            {
                int b0 = (int)this.buffer[offset];
                int b1 = (int)this.buffer[offset + 1];
                int b2 = (int)this.buffer[offset + 2];
                ret = b0 + (b1 << 16) + (b2 << 8);
            }

            return ret;
        }

        protected string readNibble()
        {
            var nextByte = readInt8();

            var ignoreLastNibble = (nextByte & 0x80) != 0;
            var size = (nextByte & 0x7f);
            var nrOfNibbles = size * 2 - (ignoreLastNibble ? 1 : 0);

            var data = readArray(size);
            var chars = new List<char>();

            for (int i = 0; i < nrOfNibbles; i++)
            {
                nextByte = data[(int)Math.Floor(i / 2.0)];

                var shift = 4 * (1 - i % 2);
                int dec = (nextByte & (15 << shift)) >> shift;

                switch (dec)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                        chars.Add(dec.ToString()[0]);
                        break;
                    case 10:
                    case 11:
                        chars.Add((char)(dec - 10 + 45));
                        break;
                    default:
                        throw new Exception("Bad nibble: " + dec);
                }
            }
            return new string(chars.ToArray());
        }

        protected IEnumerable<KeyValue> readAttributes(int size)
        {
            var attributes = new List<KeyValue>();
            for (int i = 0; i < size; i++)
            {
                string key = this.readString(this.readInt8());
                string value = this.readString(this.readInt8());
                if (key != null && value != null)
                {
                    attributes.Add(new KeyValue(key, value));
                }
            }
            return attributes;
        }

        protected ProtocolTreeNode nextTreeInternal()
        {
            int size = this.readListSize(this.readInt8());
            int token = this.readInt8();
            if (token == 1)
            {
                // token = this.readInt8();
                var attributes = this.readAttributes(size);
                return new ProtocolTreeNode("start", attributes);
            }

            if (token == 2)
            {
                return null;
            }

            string tag = this.readString(token);

            if (size == 0 || tag == null) {
                throw new CorruptStreamException("nextTree sees 0 list or null tag");
            }

            int attribCount = (size - 2 + size % 2) / 2;
            var attribs = this.readAttributes(attribCount);

            if (size % 2 == 1)
            {
                return new ProtocolTreeNode(tag, attribs);
            }

            int read2 = this.readInt8();

            byte[] nodeData = null;
            List<ProtocolTreeNode> nodeChildren = null;

            if (this.isListTag(read2))
            {
                nodeChildren = this.readList(read2);
            } else if (read2 == 252)
            {
                size = this.readInt8();
                nodeData = this.readArray(size);
            } else if (read2 == 253)
            {
                size = this.readInt20();
                nodeData = this.readArray(size);
            } else if (read2 == 254)
            {
                size = this.readInt31();
                nodeData = this.readArray(size);
            } else if (read2 == 251 || read2 == 255)
            {
                nodeData = WhatsApp.SYSEncoding.GetBytes(this.readPacked8(read2));
            } else
            {
                nodeData = WhatsApp.SYSEncoding.GetBytes(this.readString(read2));
            }

            return new ProtocolTreeNode(tag, attribs, nodeChildren, nodeData);
        }

        protected bool isListTag(int token)
        {
            return ((token == 248) || (token == 0) || (token == 249));
        }

        protected List<ProtocolTreeNode> readList(int token)
        {
            int size = this.readListSize(token);
            var ret = new List<ProtocolTreeNode>();
            for (int i = 0; i < size; i++)
            {
                ret.Add(this.nextTreeInternal());
            }
            return ret;
        }

        protected int readListSize(int token)
        {
            int size = 0;
            if (token == 0)
            {
                size = 0;
            }
            else if (token == 248)
            {
                size = this.readInt8();
            }
            else if (token == 249)
            {
                size = this.readInt16();
            }
            else
            {
                throw new Exception("BinTreeNodeReader->readListSize: Invalid token " + token);
            }
            return size;
        }
        
        protected int readInt31()
        {
            int ret = 0;
            if (this.buffer.Count >= 3)
            {
                int b0 = (int)this.buffer[0];
                int b1 = (int)this.buffer[1];
                int b2 = (int)this.buffer[2];
                this.buffer.RemoveRange(0, 3);
                ret = ((b0 & 0xF) << 16) | (b1 << 8) | b2;
            }

            return ret;
        }

        protected int readInt24()
        {
            int ret = 0;
            if (this.buffer.Count >= 3)
            {
                ret = (int)(this.buffer[0] << 16);
                ret |= (int)(this.buffer[1] << 8);
                ret |= (int)(this.buffer[2] << 0);
                this.buffer.RemoveRange(0, 3);
            }

            return ret;
        }

        protected int readInt20()
        {
            int ret = 0;
            if (this.buffer.Count >= 3)
            {
                int b0 = (int)this.buffer[0];
                int b1 = (int)this.buffer[1];
                int b2 = (int)this.buffer[2];
                this.buffer.RemoveRange(0, 3);
                ret = ((b0 & 0xF) << 16) | (b1 << 8) | b2;
            }

            return ret;
        }

        protected int readInt16()
        {
            int ret = 0;
            if (this.buffer.Count >= 2)
            {
                ret = (int)(this.buffer[0] << 8);
                ret |= (int)(this.buffer[1] << 0);
                this.buffer.RemoveRange(0, 2);
            }

            return ret;
        }

        protected int readInt8()
        {
            int ret = 0;
            if (this.buffer.Count >= 1)
            {
                ret = (int)this.buffer[0];
                this.buffer.RemoveAt(0);
            }

            return ret;
        }

        protected byte[] readArray(int len)
        {
            byte[] ret = new byte[len];
            if (this.buffer.Count >= len)
            {
                Buffer.BlockCopy(this.buffer.ToArray(), 0, ret, 0, len);
                this.buffer.RemoveRange(0, len);
            }
            else
            {
                throw new Exception();
            }
            return ret;
        }

        protected void DebugPrint(string debugMsg)
        {
            if (WhatsApp.DEBUG && debugMsg.Length > 0)
            {
                Helper.DebugAdapter.Instance.fireOnPrintDebug(debugMsg);
            }
        }
    }
}
