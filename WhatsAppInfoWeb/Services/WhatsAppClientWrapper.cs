using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WhatsAppApi;
using WhatsAppApi.Account;
using WhatsAppApi.Helper;
using static WhatsAppApi.WhatsEventBase;

namespace WhatsAppInfoWeb.Services
{
    public class WhatsAppClientWrapper : IWhatsAppClientWrapper
    {
        const string PHONE = "70000000000";

        IHostingEnvironment appEnvironment;
        string datFilename, photosDir;
        WhatsApp waClient;
        Thread threadReceiver;

        bool loggedIn = false;

        public WhatsAppClientWrapper(IHostingEnvironment appEnv)
        {
            appEnvironment = appEnv;
            datFilename = Path.Combine(appEnvironment.ContentRootPath, string.Format("{0}.next.dat", PHONE));
            photosDir = Path.Combine(appEnvironment.ContentRootPath, "photos");
            Directory.CreateDirectory(photosDir);

            Start();
        }

        public void Start()
        {
            // registration if necessary
            string password = WhatsAppApi.Register.WhatsRegisterV2.RequestExist(PHONE);

            if (String.IsNullOrEmpty(password))
            {
                // register process
                string response = string.Empty;
                if (WhatsAppApi.Register.WhatsRegisterV2.RequestCode(PHONE, out password, out response, "sms"))
                {
                    Console.WriteLine("Response: [ " + response + " ]");
                    if (string.IsNullOrEmpty(password))
                    {
                        Console.Write("SMS sent. Enter code: ");
                        string code = Console.ReadLine();
                        password = WhatsAppApi.Register.WhatsRegisterV2.RegisterCode(PHONE, code, out response);
                        Console.WriteLine("Response: [ " + response + " ]");
                    }
                }
                else
                {
                    Console.WriteLine("Response: [ " + response + " ]");

                    Console.WriteLine("Trying to register by voice.");
                    if (WhatsAppApi.Register.WhatsRegisterV2.RequestCode(PHONE, out password, out response, "voice"))
                    {
                        Console.WriteLine("Response: [ " + response + " ]");
                        if (string.IsNullOrEmpty(password))
                        {
                            Console.Write("WhatsApp making a call. Enter code: ");
                            string code = Console.ReadLine();
                            password = WhatsAppApi.Register.WhatsRegisterV2.RegisterCode(PHONE, code, out response);
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
                Console.WriteLine("Cannot complete register process (invalid code / banned)");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            waClient = new WhatsApp(PHONE, password, "nickname", false);

            bool registered = File.Exists(datFilename);

            //WhatsApp.DEBUG = true;
            //WhatsApp.DEBUGOutBound = true;
            DebugAdapter.Instance.OnPrintDebug += (data) =>
            {
                Console.WriteLine(data);
            };

            waClient.OnError += (id, from, code, text) =>
            {
                Console.WriteLine("ERROR: id={0}, from={1}, code={2}, text={3}", id, from, code, text);
            };

            waClient.OnConnectSuccess += () =>
            {
                Console.WriteLine("Connected success");
            };

            waClient.OnConnectFailed += (ex) =>
            {
                Console.WriteLine("Connected failed: " + ex.Message);
            };

            waClient.OnLoginSuccess += (pn, data) =>
            {
                loggedIn = true;

                string sdata = Convert.ToBase64String(data);
                Console.WriteLine("Login success. Next token: {0}", sdata);
                try
                {
                    File.WriteAllText(datFilename, sdata);
                }
                catch (Exception) { }

                waClient.SendGetPrivacyList();
                waClient.SendGetClientConfig();

                if (waClient.LoadPreKeys() == null)
                    waClient.sendSetPreKeys(true);

                threadReceiver = new Thread(t =>
                {
                    while (waClient != null)
                    {
                        waClient.PollMessages();
                        Thread.Sleep(100);
                    }
                })
                {
                    IsBackground = true
                };

                threadReceiver.Start();

            };

            waClient.OnLoginFailed += (data) => {
                Console.WriteLine("Login failed. Reason: {0}", data);
            };

            waClient.Connect();

            byte[] nextChallenge = null;

            if (registered)
            {
                try
                {
                    string foo = File.ReadAllText(datFilename);
                    nextChallenge = Convert.FromBase64String(foo);
                }
                catch (Exception) { };
            }

            waClient.Login(nextChallenge);
        }

        public void Stop()
        {
            if (waClient != null && waClient.ConnectionStatus != ApiBase.CONNECTION_STATUS.DISCONNECTED) {
                waClient.Disconnect();
            }
        }

        public ContactInfo GetInfoByPhone(string phone)
        {
            if (!loggedIn)
            {
                throw new InvalidOperationException("Not logged in.");
            }

            ContactInfo result = new ContactInfo();

            lock (this)
            {
                WhatsUserManager usrMan = new WhatsUserManager();
                var tmpUser = usrMan.CreateUser(phone, "User");
                result.FullJid = tmpUser.GetFullJid();
                result.Phone = "+" + phone;

                // get last seen time
                OnGetLastSeenDelegate onGetLastSeenHandler = (from, lastSeen) =>
                {
                    Console.WriteLine("{0} last seen on {1}", from, lastSeen.ToString());
                    result.LastSeen = lastSeen;
                };
                waClient.OnGetLastSeen += onGetLastSeenHandler;
                Console.WriteLine("[] Request last seen {0}", tmpUser);
                waClient.SendQueryLastOnline(tmpUser.GetFullJid());
                Thread.Sleep(1000);

                // load profile photo
                OnGetPictureDelegate onGetPhotoHandler = (from, id, data) =>
                {
                    Console.WriteLine("PHOTO: from={0}, id={1}", from, id);
                    File.WriteAllBytes(Path.Combine(photosDir, phone + ".png"), data);
                    result.PhotoFilename = Path.Combine(photosDir, phone + ".png");
                };
                waClient.OnGetPhoto += onGetPhotoHandler;
                Console.WriteLine("[] Request profile photo for {0}", tmpUser);
                waClient.SendGetPhoto(tmpUser.GetFullJid(), null, true);
                Thread.Sleep(1000);

                // get status
                OnGetStatusDelegate onGetStatusHandler = (from, type, name, status) =>
                {
                    Console.WriteLine("Status: from={0}, type={1}, name={2}, status={3}", from, type, name, status);
                    result.Status = status;
                };
                waClient.OnGetStatus += onGetStatusHandler;
                Console.WriteLine("[] Request profile status for {0}", tmpUser);
                waClient.SendGetStatuses(new string[] { tmpUser.GetFullJid() });
                Thread.Sleep(1000);

                // unsubscribe
                waClient.OnGetLastSeen -= onGetLastSeenHandler;
                waClient.OnGetPhoto -= onGetPhotoHandler;
                waClient.OnGetStatus -= onGetStatusHandler;

                result.IsRegistered = (result.LastSeen != null) || (result.PhotoFilename != null) || (result.Status != null);
            }

            return result;
        }

    }
}
