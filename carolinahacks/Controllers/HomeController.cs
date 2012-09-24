using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using NLog;

namespace carolinahacks.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(string email)
        {
            string mcApiKey = "7b3c78aaebf96c451591f41eadcf93a3-us5";
            string mcListId = "d9a06e880d";
            string mcServer = "https://us5.api.mailchimp.com/1.3/?method=listSubscribe";

            dynamic listParams = new ExpandoObject();
            listParams.apikey = mcApiKey;
            listParams.id = mcListId;
            listParams.email_address = email;
            listParams.double_optin = false;
            listParams.send_welcome = true;
            listParams.update_existing = true;

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(listParams);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(mcServer);
            request.Method = "POST";
            request.ContentType = "application/json";

            using (Stream requestStream = request.GetRequestStream())
            {
                byte[] bytes = System.Text.Encoding.Default.GetBytes(json);
                requestStream.Write(bytes, 0, bytes.Length);
            }

            using (Stream responseStream = ((HttpWebResponse)request.GetResponse()).GetResponseStream())
            {
                StreamReader reader = new StreamReader(responseStream);

                //MailChimp doesn't make it entirely clear what can cause a failure
                // I'm just being overly cautious with the code below.  I've set up NLog
                // to send me an email if bad or weird things happen

                Logger logger = LogManager.GetLogger("MailChimpSubscribeResult");

                try
                {
                    dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(reader.ReadToEnd());

                    if (result is Boolean)
                    {
                        if (result)
                        {
                            return View("Thanks");
                        }
                        else
                        {
                            logger.Warn("Result: Type is Boolean.  Value is False");
                            LogManager.Flush();
                            return View("Retry");
                        }
                    }
                    else
                    {
                        logger.Warn("Result: Type is NOT Boolean.");
                        LogManager.Flush();
                        return View("Retry");
                    }
                }
                catch (Exception exc)
                {
                    logger.Warn("Result: Exception parsing JSON.", new object[] { exc.Message });
                    LogManager.Flush();
                    return View("Retry");
                }                               
            }

        }

    }
}
