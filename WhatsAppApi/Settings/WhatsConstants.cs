using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WhatsAppApi.Settings
{
    /// <summary>
    /// Holds constant information used to connect to whatsapp server
    /// </summary>
    public class WhatsConstants
    {
        #region ServerConstants

        /// <summary>
        /// The whatsapp host
        /// </summary>
        public const string WhatsAppHost = "c3.whatsapp.net";

        /// <summary>
        /// The whatsapp XMPP realm
        /// </summary>
        public const string WhatsAppRealm = "s.whatsapp.net";

        /// <summary>
        /// The whatsapp server
        /// </summary>
        public const string WhatsAppServer = "s.whatsapp.net";

        /// <summary>
        /// The whatsapp group chat server
        /// </summary>
        public const string WhatsGroupChat = "g.us";

        public const string BuildVersionDefault = "";
        public const string BuildVersionRegister = "JLS36C";

        /// <summary>
        /// The whatsapp version the client complies to
        /// </summary>
        public const string BuildVersion = WhatsAppVerDefault;

        public const string WhatsAppVerDefault = "2.16.12";
        public const string WhatsAppVerRegister = "2.17.279";

        /// <summary>
        /// The whatsapp version the client complies to
        /// </summary>
        public const string WhatsAppVer = WhatsAppVerDefault;
	
        /// <summary>
        /// The port that needs to be connected to
        /// </summary>
        public const int WhatsPort = 443;

        public const string OS_NameDefault = "S40";
        public const string OS_NameRegister = "Android";

        /// <summary>
        /// OS name
        /// </summary>
        public const string OS_Name = OS_NameDefault;

        public const string DeviceDefault = "302";
        public const string DeviceRegister = "armani";

        /// <summary>
        /// Device
        /// </summary>
        public const string Device = DeviceDefault;

        public const string ClassesMd5Default = "14w/wF67XBf2vTc+qALwKQ=="; // 2.16.11
        public const string ClassesMd5Register = "3jYxFPSrhqjabEm5b2sXhA=="; // 2.17.279

        /// <summary>
        /// Classes MD5 hash
        /// </summary>
        public const string ClassesMd5 = ClassesMd5Default;

        public const string ManufacturerDefault = "Nokia";
        public const string ManufacturerRegister = "Xiaomi";

        /// <summary>
        /// manufacturer
        /// </summary>
        public const string Manufacturer = ManufacturerDefault;

        public const string OS_VersionDefault = "14.26";
        public const string OS_VersionRegister = "4.3";
        /// <summary>
        /// OS Version
        /// </summary>
        public const string OS_Version = OS_VersionDefault;

        public const string UserAgentDefault = "WhatsApp/" + WhatsAppVerDefault + " " + OS_NameDefault +
            "/" + OS_VersionDefault + " Device/" + ManufacturerDefault + "-" + DeviceDefault;
        public const string UserAgentRegister = "WhatsApp/" + WhatsAppVerRegister + " " + OS_NameRegister +
            "/" + OS_VersionRegister + " Device/" + ManufacturerRegister + "-" + DeviceRegister;

        /// <summary>
        /// The useragent used for http requests
        /// </summary>
        public const string UserAgent = UserAgentDefault;

        #endregion

        #region ParserConstants
        /// <summary>
        /// The number style used
        /// </summary>
        public static NumberStyles WhatsAppNumberStyle = (NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);

        /// <summary>
        /// Unix epoch DateTime
        /// </summary>
        public static DateTime UnixEpoch = new DateTime(0x7b2, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        #endregion
    }
}
