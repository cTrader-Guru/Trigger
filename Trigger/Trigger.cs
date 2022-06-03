using cAlgo.API;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace cAlgo
{

    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class Trigger : Indicator
    {


        #region Enums

        public enum Monitoring
        {

            Bar,
            Tick

        }

        public enum Logical
        {

            Major,
            Minor,
            Equal,
            MajorOrEqual,
            MinorOrEqual

        }

        #endregion

        #region Identity

        public const string NAME = "Trigger";

        public const string VERSION = "1.0.0";

        #endregion

        #region Params

        [Parameter(NAME + " " + VERSION, Group = "Identity", DefaultValue = "https://www.google.com/search?q=ctrader+guru+trigger")]
        public string ProductInfo { get; set; }

        [Parameter("Trigger when this value", Group = "Logic")]
        public DataSeries SourceA { get; set; }

        [Parameter("Is", Group = "Logic", DefaultValue = Logical.Major)]
        public Logical What { get; set; }

        [Parameter("Than this other value", Group = "Logic")]
        public DataSeries SourceB { get; set; }

        [Parameter("With this message (empty = standard message)", Group = "Logic", DefaultValue = "")]
        public string Message { get; set; }

        [Parameter("When to check the logic?", Group = "Monitoring", DefaultValue = Monitoring.Tick)]
        public Monitoring MonitorAt { get; set; }

        [Parameter("Reset after these candles", Group = "Monitoring", DefaultValue = 5, MinValue = 3)]
        public int MonitorReset { get; set; }

        [Parameter("Enabled?", Group = "PopUp", DefaultValue = true)]
        public bool PopUpEnabled { get; set; }

        [Parameter("Enabled?", Group = "Webhook", DefaultValue = false)]
        public bool WebhookEnabled { get; set; }

        [Parameter("EndPoint", Group = "Webhook", DefaultValue = "https://api.telegram.org/bot[ YOUR TOKEN ]/sendMessage")]
        public string EndPoint { get; set; }
        public Webhook MyWebook;

        [Parameter("POST", Group = "Webhook", DefaultValue = "chat_id=[ @CHATID ]&text={0}")]
        public string PostParams { get; set; }

        #endregion

        #region Property

        int LastIndex = -1,
            CountCandle = 0
            ;

        bool
            AlertInThisBar = true,
            IsANewBar = false,
            AllowToTrigger = true
            ;


        #endregion

        #region Indicator Events

        protected override void Initialize()
        {

            Print("{0} : {1}", NAME, VERSION);

            if (!WebhookEnabled) return;

            EndPoint = EndPoint.Trim();
            if (EndPoint.Length < 1)
            {

                MessageBox.Show("Wrong 'EndPoint', es. 'https://api.telegram.org/bot[ YOUR TOKEN ]/sendMessage'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;

            }

            PostParams = PostParams.Trim();
            if (PostParams.IndexOf("{0}") < 0)
            {

                MessageBox.Show("Wrong 'POST params', es. 'chat_id=[ @CHATID ]&text={0}'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;

            }

            MyWebook = new Webhook(EndPoint);

        }

        public override void Calculate(int index)
        {

            if (!IsLastBar)
                return;

            if (index != LastIndex)
            {

                if (LastIndex != -1)
                {

                    AlertInThisBar = false;
                    IsANewBar = true;

                    if (!AllowToTrigger)
                    {

                        CountCandle++;

                        if (CountCandle % MonitorReset == 0) AllowToTrigger = true;

                    }
                    else
                    {

                        CountCandle = 0;

                    }

                }

                LastIndex = index;

            }

            switch (MonitorAt)
            {

                case Monitoring.Bar:

                    if (IsANewBar) PerformeLogic(1);
                    break;

                case Monitoring.Tick:

                    PerformeLogic();
                    break;

            }

            IsANewBar = false;

        }

        #endregion

        #region Methods

        private void PerformeLogic(int index = 0)
        {

            switch (What)
            {

                case Logical.Major:

                    if (SourceA.Last(index) > SourceB.Last(index) && SourceA.Last(index + 1) <= SourceB.Last(index + 1))
                        TriggerNow("The first resource is greater than the second.");

                    break;

                case Logical.Minor:

                    if (SourceA.Last(index) < SourceB.Last(index) && SourceA.Last(index + 1) >= SourceB.Last(index + 1))
                        TriggerNow("The first resource is less than the second.");

                    break;

                case Logical.Equal:

                    if (SourceA.Last(index) == SourceB.Last(index) && SourceA.Last(index + 1) != SourceB.Last(index + 1))
                        TriggerNow("The first resource is the equal of the second.");

                    break;

                case Logical.MajorOrEqual:

                    if (SourceA.Last(index) >= SourceB.Last(index) && SourceA.Last(index + 1) < SourceB.Last(index + 1))
                        TriggerNow("The first resource is greater or equal of the second.");

                    break;

                case Logical.MinorOrEqual:

                    if (SourceA.Last(index) <= SourceB.Last(index) && SourceA.Last(index + 1) > SourceB.Last(index + 1))
                        TriggerNow("The first resource is less or equal of the second.");

                    break;

            }

        }

        private void ToWebhook(string mex)
        {

            if (MyWebook == null || !WebhookEnabled || mex == null || mex.Trim().Length == 0)
                return;

            mex = mex.Trim();

            Task<Webhook.WebhookResponse> webhook_result = Task.Run(async () => await MyWebook.SendAsync(string.Format(PostParams, mex)));

            // --> We don't know which webhook the client is using, probably a json response
            // --> var Telegram = JObject.Parse(webhook_result.Result.Response);
            // --> Print(Telegram["ok"]);

        }

        private void ToPopUp(string mex)
        {

            if (!PopUpEnabled || mex == null || mex.Trim().Length == 0)
                return;

            mex = mex.Trim();

            new Thread(new ThreadStart(delegate { MessageBox.Show(mex, "BreakOut", MessageBoxButtons.OK, MessageBoxIcon.Information); })).Start();

        }

        private void TriggerNow(string mex)
        {

            if (RunningMode != RunningMode.RealTime || AlertInThisBar || !AllowToTrigger)
                return;

            mex = string.Format("{0}: {1}", SymbolName, (Message.Trim().Length == 0 ? mex : Message.Trim()));

            ToPopUp(mex);

            ToWebhook(mex);

            AlertInThisBar = true;
            AllowToTrigger = false;

        }

        #endregion
    }

}
