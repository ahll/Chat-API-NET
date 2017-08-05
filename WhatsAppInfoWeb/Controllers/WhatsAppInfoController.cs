using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WhatsAppInfoWeb.Services;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace WhatsAppInfoWeb.Controllers
{
    [Route("api/[controller]")]
    public class WhatsAppInfoController : Controller
    {
        IHostingEnvironment appEnvironment;
        IWhatsAppClientWrapper clientWrapper;

        public WhatsAppInfoController(IWhatsAppClientWrapper wrapper, IHostingEnvironment appEnv)
        {
            clientWrapper = wrapper;
            appEnvironment = appEnv;
        }

        // GET api/whatsappinfo/70000000000
        [Produces("application/json")]
        [HttpGet("{phone}")]
        public ContactInfo Get(string phone)
        {
            if (phone.Substring(0, 1).Equals("+"))
            {
                phone = phone.Substring(1);
            }

            if (!IsDigitsOnly(phone))
            {
                throw new ArgumentException("phone is incorrect");
            }

            ContactInfo result = clientWrapper.GetInfoByPhone(phone);

            if (!String.IsNullOrEmpty(result.PhotoFilename))
            {
                result.PhotoFilename = "/photos/" + phone;
            }

            return result;
        }

        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            return true;
        }

        [HttpGet("photos/{phone}")]
        public FileResult GetPhotos(string phone)
        {
            if (!IsDigitsOnly(phone))
            {
                throw new ArgumentException("phone is incorrect");
            }

            string path = Path.Combine(Path.Combine(appEnvironment.ContentRootPath, "photos"), phone + ".png");
            string type = "image/png";

            return new FileContentResult(System.IO.File.ReadAllBytes(path), type);
        }

    }
}
