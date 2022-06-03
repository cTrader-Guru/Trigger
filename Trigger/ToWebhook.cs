using System;
using System.Net;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace cAlgo
{

    public class Webhook
    {


        /* --> USAGE:
              
        [Parameter("EndPoint", Group = "Webhook", DefaultValue = "https://api.telegram.org/bot[ YOUR TOKEN ]/sendMessage")]
        public string EndPoint { get; set; }
        public Webhook MyWebook;

        [Parameter("POST", Group = "Webhook", DefaultValue = "chat_id=[ @CHATID ]&text={0}")]
        public string PostParams { get; set; }

        public Webhook MyWebook;

        protected override void OnStart()
        {

            Print("{0} : {1}", NAME, VERSION);

            EndPoint = EndPoint.Trim();
            if (EndPoint.Length < 1)
            {

                MessageBox.Show("Wrong 'EndPoint', es. 'https://api.telegram.org/bot[ YOUR TOKEN ]/sendMessage'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Stop();

            }

            PostParams = PostParams.Trim();
            if (PostParams.IndexOf("{0}") < 0)
            {

                MessageBox.Show("Wrong 'POST params', es. 'chat_id=[ @CHATID ]&text={0}'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Stop();

            }

            MyWebook = new Webhook(EndPoint);

        }

        private void ToWebhook(Position position, string message)
        {

            if (message.Trim().Length == 0)
                return;

            if (OnlyThis && position.SymbolName != SymbolName)
                return;

            bool nolabel = position.Label == null || position.Label.Length == 0;

            if (ListLabels.Count > 0 && (nolabel || ListLabels.IndexOf(position.Label) < 0))
                return;

            message = message.Trim();

            string message_to_send = FormatMessage(position, message);

            Task<Webhook.WebhookResponse> webhook_result = Task.Run(async () => await MyWebook.SendAsync(string.Format(PostParams, message_to_send)));

            // --> We don't know which webhook the client is using, probably a json response
            // --> var Telegram = JObject.Parse(webhook_result.Result.Response);
            // --> Print(Telegram["ok"]);

        }
         
        */



        public class WebhookResponse
        {

            public int Error { get; set; }
            public string Response { get; set; }

        }

        private readonly string EndPoint = "";

        public Webhook(string NewEndPoint)
        {

            if (NewEndPoint.Trim().Length < 1) throw new ArgumentException("Parameter cannot be null", "NewEndPoint");

            EndPoint = NewEndPoint.Trim();

        }

        public async Task<WebhookResponse> SendAsync(string post_params)
        {

            WebhookResponse response = new WebhookResponse();

            try
            {

                Uri myuri = new Uri(EndPoint);

                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                using (WebClient wc = new WebClient())
                {

                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string json_result = await Task.Run(() => wc.UploadString(myuri, post_params));

                    response.Response = json_result;
                    response.Error = 0;
                    return response;
                }

            }
            catch (Exception exc)
            {

                response.Response = exc.Message;
                response.Error = 1;
                return response;

            }

        }

    }

}
