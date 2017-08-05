using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhatsAppInfoWeb.Services
{
    public interface IWhatsAppClientWrapper
    {
        ContactInfo GetInfoByPhone(String phone);
        void Start();
        void Stop();
    }
}
