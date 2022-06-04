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

        public const string VERSION = "1.0.1";

        #endregion

        #region Params

        [Parameter(NAME + " " + VERSION, Group = "Identity", DefaultValue = "https://www.google.com/search?q=ctrader+guru+trigger")]
        public string ProductInfo { get; set; }

        [Parameter("Check logic every", Group = "Monitoring", DefaultValue = Monitoring.Tick)]
        public Monitoring MonitorAt { get; set; }

        [Parameter("Reset after these bars", Group = "Monitoring", DefaultValue = 7, MinValue = 3)]
        public int MonitorReset { get; set; }

        [Parameter("Enabled?", Group = "First Logic", DefaultValue = false)]
        public bool FirstLogicEnabled { get; set; }

        [Parameter("'A' Trigger when this value", Group = "First Logic")]
        public DataSeries SourceA { get; set; }

        [Parameter("Is", Group = "First Logic", DefaultValue = Logical.Major)]
        public Logical What { get; set; }

        [Parameter("'B' Than this other value", Group = "First Logic")]
        public DataSeries SourceB { get; set; }

        [Parameter("'C' Or this level (greater than zero bypass 'B' )", Group = "First Logic", DefaultValue = 0, MinValue = 0)]
        public double SourceC { get; set; }

        [Parameter("With this message (empty = standard message)", Group = "First Logic", DefaultValue = "")]
        public string Message { get; set; }

        [Parameter("Enabled?", Group = "Second Logic", DefaultValue = false)]
        public bool SecondLogicEnabled { get; set; }

        [Parameter("'A' Trigger when this value", Group = "Second Logic")]
        public DataSeries SourceA2 { get; set; }

        [Parameter("Is", Group = "Second Logic", DefaultValue = Logical.Major)]
        public Logical What2 { get; set; }

        [Parameter("'B' Than this other value", Group = "Second Logic")]
        public DataSeries SourceB2 { get; set; }

        [Parameter("'C' Or this level (greater than zero bypass 'B' )", Group = "Second Logic", DefaultValue = 0, MinValue = 0)]
        public double SourceC2 { get; set; }

        [Parameter("With this message (empty = standard message)", Group = "Second Logic", DefaultValue = "")]
        public string Message2 { get; set; }

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

        int LastIndex = -1, CountCandle = 0;

        bool AlertInThisBar = true, IsANewBar = false, AllowToTrigger = true;


        #endregion

        #region Indicator Events

        protected override void Initialize()
        {

            Print("{0} : {1}", NAME, VERSION);

            if (!WebhookEnabled)
                return;

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

                        if (CountCandle % MonitorReset == 0)
                            AllowToTrigger = true;

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

                    if (IsANewBar)
                    {

                        PerformeFirstLogic(1);
                        PerformeSecondLogic(1);

                    }
                    break;

                case Monitoring.Tick:

                    PerformeFirstLogic();
                    PerformeSecondLogic();
                    break;

            }

            IsANewBar = false;

        }

        #endregion

        #region Methods

        private void PerformeFirstLogic(int index = 0)
        {

            if (!FirstLogicEnabled)
                return;

            switch (What)
            {

                case Logical.Major:

                    if (SourceC > 0 && SourceA.Last(index) > SourceC && SourceA.Last(index + 1) <= SourceC)
                        TriggerNow("first logic, 'A' is greater than the 'C'");
                    else if (SourceA.Last(index) > SourceB.Last(index) && SourceA.Last(index + 1) <= SourceB.Last(index + 1))
                        TriggerNow("first logic, 'A' is greater than the 'B'");

                    break;

                case Logical.Minor:

                    if (SourceC > 0 && SourceA.Last(index) < SourceC && SourceA.Last(index + 1) >= SourceC)
                        TriggerNow("first logic, 'A' is less than the 'C'");
                    else if (SourceA.Last(index) < SourceB.Last(index) && SourceA.Last(index + 1) >= SourceB.Last(index + 1))
                        TriggerNow("first logic, 'A' is less than the 'B'");

                    break;

                case Logical.Equal:

                    if (SourceC > 0 && SourceA.Last(index) == SourceC && SourceA.Last(index + 1) != SourceC)
                        TriggerNow("first logic, 'A' is equal than the 'C'");
                    else if (SourceA.Last(index) == SourceB.Last(index) && SourceA.Last(index + 1) != SourceB.Last(index + 1))
                        TriggerNow("first logic, 'A' is equal than the 'B'");

                    break;

                case Logical.MajorOrEqual:

                    if (SourceC > 0 && SourceA.Last(index) >= SourceC && SourceA.Last(index + 1) < SourceC)
                        TriggerNow("first logic, 'A' is greater or equal than the 'C'");
                    else if (SourceA.Last(index) >= SourceB.Last(index) && SourceA.Last(index + 1) < SourceB.Last(index + 1))
                        TriggerNow("first logic, 'A' is greater or equal than the 'B'");

                    break;

                case Logical.MinorOrEqual:

                    if (SourceC > 0 && SourceA.Last(index) <= SourceC && SourceA.Last(index + 1) > SourceC)
                        TriggerNow("first logic, 'A' is less or equal than the 'C'");
                    else if (SourceA.Last(index) <= SourceB.Last(index) && SourceA.Last(index + 1) > SourceB.Last(index + 1))
                        TriggerNow("first logic, 'A' is less or equal than the 'B'");

                    break;

            }

        }

        private void PerformeSecondLogic(int index = 0)
        {

            if (!SecondLogicEnabled)
                return;

            switch (What2)
            {

                case Logical.Major:

                    if (SourceC2 > 0 && SourceA2.Last(index) > SourceC2 && SourceA2.Last(index + 1) <= SourceC2)
                        TriggerNow("second logic, 'A' is greater than the 'C'", 2);
                    else if (SourceA2.Last(index) > SourceB2.Last(index) && SourceA2.Last(index + 1) <= SourceB2.Last(index + 1))
                        TriggerNow("second logic, 'A' is greater than the 'B'", 2);

                    break;

                case Logical.Minor:

                    if (SourceC2 > 0 && SourceA2.Last(index) < SourceC2 && SourceA2.Last(index + 1) >= SourceC2)
                        TriggerNow("second logic, 'A' is less than the 'C'", 2);
                    else if (SourceA2.Last(index) < SourceB2.Last(index) && SourceA2.Last(index + 1) >= SourceB2.Last(index + 1))
                        TriggerNow("second logic, 'A' is less than the 'B'", 2);

                    break;

                case Logical.Equal:

                    if (SourceC2 > 0 && SourceA2.Last(index) == SourceC2 && SourceA2.Last(index + 1) != SourceC2)
                        TriggerNow("second logic, 'A' is equal than the 'C'", 2);
                    else if (SourceA2.Last(index) == SourceB2.Last(index) && SourceA2.Last(index + 1) != SourceB2.Last(index + 1))
                        TriggerNow("second logic, 'A' is equal than the 'B'", 2);

                    break;

                case Logical.MajorOrEqual:

                    if (SourceC2 > 0 && SourceA2.Last(index) >= SourceC2 && SourceA2.Last(index + 1) < SourceC2)
                        TriggerNow("second logic, 'A' is greater or equal than the 'C'", 2);
                    else if (SourceA2.Last(index) >= SourceB2.Last(index) && SourceA2.Last(index + 1) < SourceB2.Last(index + 1))
                        TriggerNow("second logic, 'A' is greater or equal than the 'B'", 2);

                    break;

                case Logical.MinorOrEqual:

                    if (SourceC2 > 0 && SourceA2.Last(index) <= SourceC2 && SourceA2.Last(index + 1) > SourceC2)
                        TriggerNow("second logic, 'A' is less or equal than the 'C'", 2);
                    else if (SourceA2.Last(index) <= SourceB2.Last(index) && SourceA2.Last(index + 1) > SourceB2.Last(index + 1))
                        TriggerNow("second logic, 'A' is less or equal than the 'B'", 2);

                    break;

            }

        }

        private void ToWebhook(string mex)
        {

            bool canSendMessage = RunningMode == RunningMode.RealTime;

            if (!canSendMessage || MyWebook == null || !WebhookEnabled || mex == null || mex.Trim().Length == 0)
                return;

            mex = mex.Trim();

            Task<Webhook.WebhookResponse> webhook_result = Task.Run(async () => await MyWebook.SendAsync(string.Format(PostParams, mex)));

            // --> We don't know which webhook the client is using, probably a json response
            // --> var Telegram = JObject.Parse(webhook_result.Result.Response);
            // --> Print(Telegram["ok"]);

        }

        private void ToPopUp(string mex)
        {

            bool canShowPopUp = RunningMode == RunningMode.RealTime || RunningMode == RunningMode.VisualBacktesting;

            if (!canShowPopUp || !PopUpEnabled || mex == null || mex.Trim().Length == 0)
                return;

            mex = mex.Trim();

            new Thread(new ThreadStart(delegate { MessageBox.Show(mex, "BreakOut", MessageBoxButtons.OK, MessageBoxIcon.Information); })).Start();

        }

        private void TriggerNow(string mex, int logic = 1)
        {

            if (AlertInThisBar || !AllowToTrigger)
                return;

            string customMex = logic == 2 ? Message2 : Message;

            mex = string.Format("{0}: {1}", SymbolName, (customMex.Trim().Length == 0 ? mex : customMex.Trim()));

            ToPopUp(mex);

            ToWebhook(mex);

            AlertInThisBar = true;
            AllowToTrigger = false;

        }

        #endregion
    }

}
