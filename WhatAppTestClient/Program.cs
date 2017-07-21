using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WhatsAppApi;
using WhatsAppApi.Account;
using WhatsAppApi.Helper;

namespace ConsoleApp1
{
    class Program
    {
        static string getDatFileName(string pn)
        {
            string filename = string.Format("{0}.next.dat", pn);
            return Path.Combine(Directory.GetCurrentDirectory(), filename);
        }

        static void Main(string[] args)
        {
            string phone = "1XXXXXXXXXX";
            string password = "";
            string nickname = "nickname";

            string foreignPhone = "1XXXXXXXXXX";
            
            if (String.IsNullOrEmpty(password))
            {
                password = WhatsAppApi.Register.WhatsRegisterV2.RequestExist(phone);
            }
            
            if (String.IsNullOrEmpty(password))
            {
                // register process
                string response = string.Empty;
                if (WhatsAppApi.Register.WhatsRegisterV2.RequestCode(phone, out password, out response, "sms"))
                {
                    Console.WriteLine("Response: [ " + response + " ]");
                    if (string.IsNullOrEmpty(password))
                    {
                        Console.Write("SMS sent. Enter code: ");
                        string code = Console.ReadLine();
                        password = WhatsAppApi.Register.WhatsRegisterV2.RegisterCode(phone, code, out response);
                        Console.WriteLine("Response: [ " + response + " ]");
                    }
                } else
                {
                    Console.WriteLine("Response: [ " + response + " ]");

                    Console.WriteLine("Trying to register by voice.");
                    if (WhatsAppApi.Register.WhatsRegisterV2.RequestCode(phone, out password, out response, "voice"))
                    {
                        Console.WriteLine("Response: [ " + response + " ]");
                        if (string.IsNullOrEmpty(password))
                        {
                            Console.Write("WhatsApp making a call. Enter code: ");
                            string code = Console.ReadLine();
                            password = WhatsAppApi.Register.WhatsRegisterV2.RegisterCode(phone, code, out response);
                            Console.WriteLine("Response: [ " + response + " ]");
                        }
                        else
                        {
                            Console.WriteLine("Cannot initiate register process (banned?)");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Response: [ " + response + " ]");
                    }
                }
            }

            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Cannot complete register process (invalide code / banned)");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            WhatsApp wa = new WhatsApp(phone, password, nickname, false);

            string datFile = getDatFileName(phone);
            bool registered = File.Exists(datFile);

            //WhatsApp.DEBUG = true;
            //WhatsApp.DEBUGOutBound = true;
            DebugAdapter.Instance.OnPrintDebug += (data) =>
            {
                Console.WriteLine(data);
            };

            wa.OnConnectSuccess += () =>
            {
                Console.WriteLine("Connected success");
            };

            wa.OnConnectFailed += (ex) =>
            {
                Console.WriteLine("Connected failed: " + ex.Message);
            };

            wa.OnGetLastSeen += (from, lastSeen) =>
            {
                Console.WriteLine("{0} last seen on {1}", from, lastSeen.ToString());
            };

            wa.OnGetContactName += (from, contactName) =>
            {
                Console.WriteLine("{0} - {1}", from, contactName);
            };

            wa.OnGetSyncResult += (index, sid, existingUsers, failedNumbers) =>
            {
                Console.WriteLine("Sync result for {0}:", sid);
                foreach (KeyValuePair<string, string> item in existingUsers)
                {
                    Console.WriteLine("Existing: {0} (username {1})", item.Key, item.Value);
                }
                foreach (string item in failedNumbers)
                {
                    Console.WriteLine("Non-Existing: {0}", item);
                }
            };

            wa.OnGetStatus += (from, type, name, status) =>
            {
                Console.WriteLine("Status: from={0}, type={1}, name={2}, status={3}", from, type, name, status);
            };

            wa.OnError += (id, from, code, text) =>
            {
                Console.WriteLine("ERROR: id={0}, from={1}, code={2}, text={3}", id, from, code, text);
            };

            wa.OnGetPhoto += (from, id, data) =>
            {
                Console.WriteLine("PHOTO: from={0}, id={1}", from, id);
                File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "photo.png"), data);
            };

            wa.OnGetMessage += (messageNode, from, id, name, message, receipt_sent) =>
            {
                Console.WriteLine("MESSAGE: from={0}, id={1}, name={2}, message={3}, receipt_sent=", from, id, name, message, receipt_sent);
            };
            
            wa.OnLoginSuccess += (pn, data) =>
            {
                string sdata = Convert.ToBase64String(data);
                Console.WriteLine("Login success. Next password: {0}", sdata);
                try
                {
                    File.WriteAllText(getDatFileName(phone), sdata);
                }
                catch (Exception) { }

                if (registered)
                {
                    wa.SendGetPrivacyList();
                    wa.SendGetClientConfig();

                    if (wa.LoadPreKeys() == null)
                        wa.sendSetPreKeys(true);

                    Thread thRecv = new Thread(t =>
                    {
                        try
                        {
                            while (wa != null)
                            {
                                wa.PollMessages();
                                Thread.Sleep(100);
                                continue;
                            }

                        }
                        catch (ThreadAbortException)
                        {
                        }
                    })
                    { IsBackground = true };
                    thRecv.Start();

                    WhatsUserManager usrMan = new WhatsUserManager();
                    var tmpUser = usrMan.CreateUser(foreignPhone, "User");

                    //Thread.Sleep(1000);
                    //Console.WriteLine("[] Request sync {0}", foreignPhone);
                    //wa.SendSync(new string[] { foreignPhone });

                    Thread.Sleep(1000);
                    Console.WriteLine("[] Request last seen {0}", tmpUser);
                    wa.SendQueryLastOnline(tmpUser.GetFullJid());

                    //Thread.Sleep(1000);
                    //Console.WriteLine("[] Request profile photo for {0}", tmpUser);
                    //wa.SendGetPhoto(tmpUser.GetFullJid(), null, true);

                    //Thread.Sleep(1000);
                    //Console.WriteLine("[] Request profile status for {0}", tmpUser);
                    //wa.SendGetStatuses(new string[] { tmpUser.GetFullJid() });

                    //Thread.Sleep(1000);
                    //Console.WriteLine("[] Request contact {0}", foreignPhone);
                    //wa.SendSync(new string[] { foreignPhone });

                    Thread.Sleep(int.MaxValue);

                    thRecv.Abort();
                }

                wa.Disconnect();
            };

            wa.OnLoginFailed += (data) => {
                Console.WriteLine("Login failed. Reason: {0}", data);
            };

            wa.Connect();

            byte[] nextChallenge = null;

            if (registered)
            {
                try
                {
                    string foo = File.ReadAllText(datFile);
                    nextChallenge = Convert.FromBase64String(foo);
                }
                catch (Exception) { };
            }

            wa.Login(nextChallenge);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
