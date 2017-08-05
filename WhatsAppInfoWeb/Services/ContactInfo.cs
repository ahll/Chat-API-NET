using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhatsAppInfoWeb.Services
{
    public class ContactInfo
    {
        public String FullJid { get; set; }
        public String Phone { get; set; }
        public Boolean IsRegistered { get; set; }
        public String PhotoFilename { get; set; }
        public String Status { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
