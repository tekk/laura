using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace Laura1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string VOC_CULTURE = "sk-SK";
        const string VOC_CELSIUS = "Celzia";
        const string VOC_PROP_UNKNOWN = "nezistená";
        const string VOC_SOLAR_ACTIVITY_INDEX = "Solárny index";
        const string VOC_ALPHA_INDEX = "Alfa index";
        const string VOC_K_INDEX = "Ká index";
        const string VOC_TOMORROW = " Predpoveď na zajtra. ";
        const string VOC_TEMPERATURE_TO = "až";
        const string VOC_TEMPERATURE_UNKNOWN = "nezistené";
        const string VOC_TODAY = "dnes";
        const string VOC_FORECAST_WEEK = "Týždňová predpoveď počasia. ";
        const string VOC_FORECAST_FOR = "Predpoveď na ";

        public bool LauraTX { get; set; }
        public bool LauraRX { get; set; }
        public bool LauraRXWait { get; set; }
        public bool LauraPTT { get; set; }

        public long IdleSince { get; set; }
        public long RXSince { get; set; }
        public long TXSince { get; set; }

        public long RepeatSince { get; set; }
        public int RepeatSetMinutes { get; set; }
        private ulong LatinModulo { get; set; }
        private ulong TechNewsModulo { get; set; }

        public enum Status
        {
            INACTIVE, IDLE, RX, TX
        }

        public Status CurrentStatus { get; set; }

        SerialPort currentPort;
        DispatcherTimer timerTX = new DispatcherTimer();
        DispatcherTimer timerUpdateTime = new DispatcherTimer();
        DispatcherTimer timerRepeat = new DispatcherTimer();
        DispatcherTimer timerRepeatUpdate = new DispatcherTimer();

        bool portFound;
        byte[] ack;
        byte[] tx;

        public MainWindow()
        {
            InitializeComponent();

            timerUpdateTime.Tick += TimerUpdateTime_Tick;
            timerUpdateTime.Interval = new TimeSpan(0, 0, 0, 0, 100);

            timerRepeat.Tick += TimerRepeat_Tick;
            timerRepeatUpdate.Tick += TimerRepeatUpdate_Tick;
            timerRepeatUpdate.Interval = new TimeSpan(0, 0, 0, 0, 100);

            SetStatus(Status.INACTIVE);

            ack = new byte[7];
            ack[0] = Convert.ToByte('<');
            ack[1] = Convert.ToByte('L');
            ack[2] = Convert.ToByte('a');
            ack[3] = Convert.ToByte('u');
            ack[4] = Convert.ToByte('r');
            ack[5] = Convert.ToByte('a');
            ack[6] = Convert.ToByte('>');

            tx = new byte[4];
            tx[0] = Convert.ToByte('<');
            tx[1] = Convert.ToByte('T');
            tx[2] = Convert.ToByte('X');
            tx[3] = Convert.ToByte('>');

            timerTX.Tick += new EventHandler(timerTX_Tick);
            timerTX.Interval = new TimeSpan(0, 0, 0, 0, 50);

            try
            {
                string[] ports = SerialPort.GetPortNames();

                PortsCombo.Items.Clear();

                foreach (string port in ports)
                {
                    PortsCombo.Items.Add(port);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Cannot enumerate COM ports. Restart.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TimerRepeatUpdate_Tick(object sender, EventArgs e)
        {
            long elapsedTicks = 0;
            var elapsed = TimeSpan.FromTicks(elapsedTicks);

            elapsedTicks = DateTime.Now.Ticks - timerRepeat.Interval.Ticks - RepeatSince;
            elapsed = TimeSpan.FromTicks(elapsedTicks);
            RepeatLeftText.Text = String.Format("{0} to go...", elapsed);
        }

        private bool AckLaura(string port)
        {
            try
            {
                int intReturnASCII = 0;
                char charReturnValue = (Char)intReturnASCII;

                if (currentPort != null && currentPort.IsOpen) currentPort.Close();

                currentPort = new SerialPort(port, 115200, Parity.None);
                currentPort.DtrEnable = true;
                currentPort.ReadTimeout = 5000;
                currentPort.WriteTimeout = 500;

                currentPort.Open();
                currentPort.Write(ack, 0, 7);
                Thread.Sleep(100);

                string returnMessage = currentPort.ReadExisting();

                currentPort.DtrEnable = false;
                currentPort.Close();

                if (returnMessage.Contains("ACKOK"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }


        private void timerTX_Tick(object sender, EventArgs e)
        {
            if (currentPort != null && currentPort.IsOpen) currentPort.Write(tx, 0, 4);

            // TODO search reply for "TXOK"
        }

        private void Email_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }


        private void Web_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void TestConnectionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PortsCombo.SelectedValue == null || String.IsNullOrEmpty(PortsCombo.SelectedValue.ToString()))
            {
                MessageBox.Show("Please select valid COM port.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TestResultTxt.Text = "testing...";
            TestResultTxt.Refresh();

            string port = PortsCombo.SelectedValue.ToString();

            try
            {
                if (AckLaura(port))
                {
                    portFound = true;
                    // MessageBox.Show(String.Format("Laura HW interface found on {0}.", port), "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    portFound = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Cannot communicate with port {0}.", port), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }


            if (portFound)
            {
                TestResultTxt.Text = "Succeeded.";
            }
            else
            {
                TestResultTxt.Text = "Failed.";
            }

        }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            // first check if we are already connected
            if (ConnectBtn.Content.ToString() == "Disconnect")
            {
                // disconnect
                if (currentPort.IsOpen) currentPort.Close();
                ConnectBtn.Content = "Connect";
                ConnectedInfoTxt.Text = "";
                // and exit
                return;
            }

            if (PortsCombo.SelectedValue == null || String.IsNullOrEmpty(PortsCombo.SelectedValue.ToString()))
            {
                MessageBox.Show("Please select valid COM port.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TestConnectionBtn.IsEnabled = false;
            string port = PortsCombo.SelectedValue.ToString();

            try
            {
                int intReturnASCII = 0;
                char charReturnValue = (Char)intReturnASCII;

                currentPort = new SerialPort(port, 115200, Parity.None);
                currentPort.DtrEnable = true;
                //currentPort.ReadTimeout = 5000;
                //currentPort.WriteTimeout = 500;

                currentPort.DataReceived += CurrentPort_DataReceived;

                currentPort.Open();

                ConnectBtn.Content = "Disconnect";
                ConnectedInfoTxt.Text = "Connected";
                ConnectedInfoTxt.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0xff, 0x33));
            }
            catch (Exception ex)
            {
                if (currentPort != null && currentPort.IsOpen) currentPort.Close();
                ConnectBtn.Content = "Connect";
                ConnectedInfoTxt.Text = "";
            }
        }

        private void CurrentPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (currentPort != null && currentPort.IsOpen)
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    ConnectedInfoTxt.Text = "Connected";
                    ConnectedInfoTxt.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0xff, 0x33));
                }));

                while (currentPort.BytesToRead > 0)
                {
                    //Thread.Sleep(10);
                    string msg = currentPort.ReadLine();

                    try
                    {
                        if (msg.Contains("="))
                        {
                            string token = msg.Split('=')[0];
                            string value = msg.Split('=')[1].TrimEnd();

                            switch (token)
                            {
                                case "TX":
                                    LauraTX = (value == "1");
                                    if (LauraTX && CurrentStatus != Status.TX) SetStatus(Status.TX);
                                    break;

                                case "PTT":
                                    LauraPTT = (value == "1");
                                    if (LauraPTT && CurrentStatus != Status.TX) SetStatus(Status.TX);
                                    break;

                                case "RX":
                                    LauraRX = (value == "1");
                                    if (LauraRX && CurrentStatus != Status.RX) SetStatus(Status.RX);
                                    break;

                                case "RXWAIT":
                                    LauraRXWait = (value == "1");
                                    if (LauraRXWait && CurrentStatus != Status.RX) SetStatus(Status.RX);
                                    break;

                                case "DTMF":
                                    switch(value)
                                    {
                                        case "X":
                                            break;

                                        case "**0":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                TransmitOccasionaly("[DT]");
                                            }));
                                            break;

                                        case "**1":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                TransmitOccasionaly("[WEATHER]");
                                            }));
                                            break;

                                        case "**30":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                TransmitOccasionaly("[HF]");
                                            }));
                                            break;

                                        case "**2":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                TransmitOccasionaly("[PROPAGATION]");
                                            }));
                                            break;

                                        case "**3":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                TransmitOccasionaly("[TECHHEADLINE]");
                                            }));
                                            break;

                                        case "**4":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                TransmitOccasionaly("[LATINQUOTE]");
                                            }));
                                            break;

                                        case "**5":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                PlayFile("Prehráme si riff číslo jedna.", "sounds\\riff1v2.ogg", "Čauky.");
                                            }));
                                            break;

                                        case "**6":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                PlayFile("Riff číslo dva.", "sounds\\riff2v2.ogg", "");
                                            }));
                                            break;

                                        case "**7":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                PlayFile("Riff číslo tri.", "sounds\\riff3.ogg", "");
                                            }));
                                            break;

                                        case "**8":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                PlayFile("Ahoj.", "sounds\\icq.ogg", "");
                                            }));
                                            break;

                                        case "**10":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                TransmitOccasionaly("Laurinka ti ďakuje.");
                                            }));
                                            break;

                                        case "**11":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                TransmitOccasionaly("Laurinka ťa chváli.");
                                            }));
                                            break;

                                        case "**73":
                                            Dispatcher.Invoke((Action)(() =>
                                            {
                                                TransmitOccasionaly("Čau čau. Sedem tri.");
                                            }));
                                            break;

                                        default:

                                            if (value.Length == 4 && value.StartsWith("**2"))
                                            {
                                                Dispatcher.Invoke((Action)(() =>
                                                {
                                                    int dayPlusX = 0;
                                                    int.TryParse(value[3].ToString(), out dayPlusX);
                                                    TransmitOccasionaly(ConstructWeatherTextForDayPlusX(dayPlusX));
                                                }));

                                                break;
                                            }
                                            else
                                            {
                                                if (value.Length > 1 && value[0] == '*')
                                                {
                                                    Dispatcher.Invoke((Action)(() =>
                                                    {
                                                        TransmitOccasionaly("Neznámy príkaz.");
                                                    }));
                                                }
                                            }
                                            break;
                                    }
                                    break;
                                
                                default:
                                    break;
                            }

                            if (!LauraTX && !LauraRX && !LauraPTT && !LauraRXWait && CurrentStatus != Status.IDLE) SetStatus(Status.IDLE);
                        }
                    }
                    catch
                    {
                        // nothing special, parse did not found index IMO
                    }
                }

                currentPort.ReadExisting();

                currentPort.Write(ack, 0, 7);
                Thread.Sleep(50);

                string msgACK = "";

                try
                {
                    msgACK = currentPort.ReadExisting();
                }
                catch (Exception ex)
                {

                }
                

                if (msgACK.Contains("ACKOK"))
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ConnectedInfoTxt.Text = "Connected";
                        ConnectedInfoTxt.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0xff, 0x33));
                    }));
                }
                else
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ConnectedInfoTxt.Text = "Connection lost";
                        ConnectedInfoTxt.Foreground = new SolidColorBrush(Colors.Red);
                    }));
                }

            }
        }

        private void StartTX_Click(object sender, RoutedEventArgs e)
        {
            timerTX.Start();
        }

        private void StopTX_Click(object sender, RoutedEventArgs e)
        {
            timerTX.Stop();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            timerTX.Stop();
            timerUpdateTime.Stop();

            if (currentPort != null && currentPort.IsOpen)
            {
                try
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        currentPort.DtrEnable = false;
                        currentPort.DataReceived -= CurrentPort_DataReceived;
                        currentPort.DiscardOutBuffer();
                        currentPort.DiscardInBuffer();
                        Thread.Sleep(1000);
                        currentPort.Close();
                    }));
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void TimerUpdateTime_Tick(object sender, EventArgs e)
        {
            long elapsedTicks = 0;
            var elapsed = TimeSpan.FromTicks(elapsedTicks);

            switch (CurrentStatus)
            {
                case Status.INACTIVE:
                    DurationTxt.Text = "--";
                    return;

                case Status.IDLE:
                    elapsedTicks = DateTime.Now.Ticks - IdleSince;
                    elapsed = TimeSpan.FromTicks(elapsedTicks);
                    DurationTxt.Text = String.Format("{0}", elapsed);
                    break;

                case Status.RX:
                    elapsedTicks = DateTime.Now.Ticks - RXSince;
                    elapsed = TimeSpan.FromTicks(elapsedTicks);
                    break;

                case Status.TX:
                    elapsedTicks = DateTime.Now.Ticks - TXSince;
                    elapsed = TimeSpan.FromTicks(elapsedTicks);
                    DurationTxt.Text = String.Format("{0:G}", elapsed);
                    break;
            }

        }

        private void SetStatus(Status status)
        {
            switch (status)
            {
                case Status.INACTIVE:
                    timerTX.Stop();
                    timerUpdateTime.Stop();
                    CurrentStatus = Status.INACTIVE;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        RXTXTxt.Text = "Inactive";
                        RXTXTxt.Foreground = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xcc));
                        DurationTxt.Text = "--";
                        RXTXTxt.Refresh();
                        DurationTxt.Refresh();
                    }));
                    break;

                case Status.IDLE:
                    if (!timerUpdateTime.IsEnabled) timerUpdateTime.Start();
                    CurrentStatus = Status.IDLE;
                    IdleSince = DateTime.Now.Ticks;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        RXTXTxt.Text = "Idle";
                        RXTXTxt.Foreground = new SolidColorBrush(Color.FromRgb(0xdd, 0xdd, 0xcc));
                        RXTXTxt.Refresh();
                    }));
                    break;

                case Status.RX:
                    if (!timerUpdateTime.IsEnabled) timerUpdateTime.Start();
                    CurrentStatus = Status.RX;
                    RXSince = DateTime.Now.Ticks;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        RXTXTxt.Text = "RX";
                        RXTXTxt.Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0xff, 0x33));
                        RXTXTxt.Refresh();
                    }));
                    break;

                case Status.TX:
                    if (!timerUpdateTime.IsEnabled) timerUpdateTime.Start();
                    CurrentStatus = Status.TX;
                    TXSince = DateTime.Now.Ticks;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        RXTXTxt.Text = "TX";
                        RXTXTxt.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x33, 0x33));
                        RXTXTxt.Refresh();
                    }));
                    break;
            }
        }

        private string ConstructPropagationText()
        {
            string url = @"http://dx.qsl.net/propagation/propagation.html";
            string propagation = VOC_PROP_UNKNOWN;

            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load(url);

                propagation = doc.DocumentNode.SelectNodes("/html/body/center[1]/table/tr[2]/td/font[2]").First().InnerText.Replace("\n", "").Replace("&nbsp;", "");

            }
            catch (Exception e)
            {

            }

            return propagation.Replace("SFI =", VOC_SOLAR_ACTIVITY_INDEX).Replace("A =", ", " + VOC_ALPHA_INDEX).Replace("K =", ", " + VOC_K_INDEX);
        }

        public string ConstructTimeText()
        {
            CultureInfo ci = new CultureInfo(VOC_CULTURE);
            return String.Format("{0}.", DateTime.Now.ToString("dddd") + ", " + DateTime.Now.ToString("d. MMMM, HH:mm ", ci));
        }

        private string ConstructLatinText()
        {
            try
            {

                if (LatinModulo == 0)
                {
                    LatinModulo = (ulong)DateTime.Now.Ticks;
                }

                switch (LatinModulo++ % 20)
                {
                    case 0:
                        return "Gloria spernentem fovet, adversatur amantem. Sláva praje tomu, kto ňou pohŕda, a nepraje tomu, kto po nej túži.";

                    case 1:
                        return "Ignoskas aliis multa, nil tyby. Odpúšťaj iným mnohé, ale sebe nič.";

                    case 2:
                        return "In vino veritas, in aqua sanitas. Vo víne je pravda, vo vode je zdravie.";

                    case 3:
                        return "Memento homo, quia pulvis es et in pulverem reverteris. Pamätaj človeče, prach si a v prach sa obrátiš.";

                    case 4:
                        return "Multi multa, nemo omnia novit. Mnoho ľudí veľa vie.";

                    case 5:
                        return "Memento móri. Pamätaj na smrť, človeče.";

                    case 6:
                        return "Nondum omnium dierum sol oksidit. Ešte nezapadlo slnko všetkých dní.";

                    case 7:
                        return "Potius sero quam numquam. Lepšie neskoro ako nikdy.";

                    case 8:
                        return "Vanitas vanitatum et omnia vanitas. Márnosť nad márnosť, všetko je len márnosť.";

                    case 9:
                        return "Veritas vincit. Pravda víťazí.";

                    case 10:
                        return "Amicitia semper prodest, amor aliquando etiam nocet. Priateľstvo vždy prospieva, láska niekedy aj škodí.";

                    case 11:
                        return "Amikum in sekreto mone, palam lauda. Priateľa napomínaj v skrytosti (medzi štyrmi očami), chváľ ho verejne.";

                    case 12:
                        return "Bona fortuna. Veľa šťastia.";

                    case 13:
                        return "Damnant, quae non intelligunt. Odsudzujú to, čo nechápu.";

                    case 14:
                        return "De duobus malis minus est semper eligendum. Z dvoch ziel vždy treba voliť menšie.";

                    case 15:
                        return "De se ipso modifice, de aliis honorifice. O sebe skromne, o druhých s úctou.";

                    case 16:
                        return "Facilius kadimus quam resurgimus. Človek ľahšie padne, ako vstane.";

                    case 17:
                        return "Gloria et honor et pax omni operanti bonum. Sláva, česť a pokoj čakajú na každého, kto koná dobro.";

                    case 18:
                        return "Gloria in excelsis Deo et in terra pax hominibus bonae voluntatis. Sláva Bohu na výsostiach a na zemi pokoj ľuďom dobrej vôle.";

                    case 19:
                        return "Vita incerta, mors certissima. Život je neistý, smrť je najistejšia.";

                    default:
                        return "Quidquid latine diktum sit, altum videtur. Čokoľvek sa povie latinsky, stále to vyznie vznešene.";
                }
            }
            catch
            {
                return "Quidquid latine diktum sit, altum videtur. Čokoľvek sa povie latinsky, stále to vyznie vznešene.";
            }
        }

        private string ConstructTechHeadlineText()
        {
            string url = @"http://rss.sme.sk/rss/rss.asp?sek=tech"; // todo change to worldwide

            try
            {
                var feed = GetFeed(url);
                List<string> messages = new List<string>();

                foreach (var item in feed.Items)
                {
                    foreach (SyndicationElementExtension extension in item.ElementExtensions)
                    {
                        XElement ele = extension.GetObject<XElement>();
                        if (ele.Name.LocalName == "subject" && ele.Value == "Internet")
                        {
                            messages.Add(item.Title.Text + ". " + item.Summary.Text.Split('\n')[0] + " ");
                        }
                    }
                }

                if (TechNewsModulo == 0)
                {
                    TechNewsModulo = (ulong)DateTime.Now.Ticks;
                }

                return messages[(int)(TechNewsModulo++ % (ulong)messages.Count)];
            }
            catch (Exception e)
            {

            }

            return "Kde nič, tu nič. ";
        }

        public SyndicationFeed GetFeed(String url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            using (var response = request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            {
                Debug.Assert(responseStream != null, "responseStream != null");

                var xmlReaderSettings = new XmlReaderSettings { IgnoreComments = true };
                using (XmlReader xmlReader = XmlReader.Create(responseStream, xmlReaderSettings))
                {
                    var feed = SyndicationFeed.Load(xmlReader);
                    return feed;
                }
            }
        }

        private void Wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private string ConstructPropagationHFText()
        {
            //http://www.bandconditions.com/
            string url = @"http://75.35.171.117/index.htm";

            string m80 = "", m40 = "", m20 = "", m15 = "";

            string result = " nezistené ";

            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load(url);
                var items = doc.DocumentNode.SelectNodes("/html/body").First().InnerHtml.Split(new string[] { "<img src=\"http://www.bandcondx.com/", ".jpg\"" }, StringSplitOptions.RemoveEmptyEntries);

                m80 = items[3];
                m40 = items[5];
                m20 = items[9];
                m15 = items[13];

                result = $" Osemdesiatmetrové pásmo {IndexToBandOpened(m80)}.\n " +
                         $" Štyridsaťmetrové pásmo {IndexToBandOpened(m40)}.\n " +
                         $" Dvadsaťmetrové pásmo {IndexToBandOpened(m20)}.\n " +
                         $" Pätnásťmetrové pásmo {IndexToBandOpened(m15)}.\n ";

                //todaysForecast = doc.DocumentNode.SelectNodes("//*[@id=\"content\"]/section[1]/div[2]/div[1]/div[1]/div[2]/span[3]").First().InnerText;
                //tommorrowTemperature = doc.DocumentNode.SelectNodes("//*[@id=\"content\"]/section[2]/div/div/ul/li[1]/div/div[2]/div[2]/span[1]").First().InnerText;
                //tommorrowState = doc.DocumentNode.SelectNodes("//*[@id=\"content\"]/section[2]/div/div/ul/li[1]/div/div[2]/div[2]/span[2]").First().InnerText;
                //rainPercentual = doc.DocumentNode.SelectNodes("//*[@id=\"content\"]/section[2]/div/div/ul/li[1]/div/div[3]/span[2]/span").First().InnerText.Split(' ')[0];
            }
            catch (Exception e)
            {

            }

            return result;
        }

        private string IndexToBandOpened(string index)
        {
            int value = 0;
            if (!int.TryParse(index, out value))
            {
                return "";
            }

            return IndexToBandOpened(value);
        }

        private string IndexToBandOpened(int index)
        {
            if (index <= 33)
            {
                return "zatvorené";
            }
            else if (index > 33 && index < 66)
            {
                return "čiastočne otvorené";
            }
            else if (index >= 66)
            {
                return "otvorené";
            }

            return "";
        }

        private string ConstructWeatherText()
        {
            string url = @"https://pocasie.aktuality.sk/banska-bystrica/"; // todo change to worldwide
            string temperature = VOC_TEMPERATURE_UNKNOWN;
            string todaysForecast = "";
            string tommorrowTemperature = "";
            string tommorrowState = "";
            string rainPercentual = "";
            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load(url);
                temperature = doc.DocumentNode.SelectNodes("//*[@id=\"content\"]/section[1]/div[2]/div[1]/div[1]/div[2]/span[1]").First().InnerText.Split(' ')[0];
                todaysForecast = doc.DocumentNode.SelectNodes("//*[@id=\"content\"]/section[1]/div[2]/div[1]/div[1]/div[2]/span[3]").First().InnerText;
                tommorrowTemperature = doc.DocumentNode.SelectNodes("//*[@id=\"content\"]/section[2]/div/div/ul/li[1]/div/div[2]/div[2]/span[1]").First().InnerText;
                tommorrowState = doc.DocumentNode.SelectNodes("//*[@id=\"content\"]/section[2]/div/div/ul/li[1]/div/div[2]/div[2]/span[2]").First().InnerText;
                rainPercentual = doc.DocumentNode.SelectNodes("//*[@id=\"content\"]/section[2]/div/div/ul/li[1]/div/div[3]/span[2]/span").First().InnerText.Split(' ')[0];
                
            }
            catch (Exception e)
            {

            }

            return String.Format("{0}, " + VOC_TODAY + " {1}. " + VOC_TOMORROW + " {2}, {3}" + ", pravdepodobnosť zrážok {4} ", temperature + " stupňov " + VOC_CELSIUS, todaysForecast, tommorrowState, tommorrowTemperature.Replace("C", " " + VOC_CELSIUS).Replace("/", VOC_TEMPERATURE_TO), rainPercentual);
        }

        private string ConstructWeatherTextForDayPlusX(int plusDayX)
        {
            StringBuilder sb = new StringBuilder();

            if (plusDayX == 0)
                sb.Append(VOC_FORECAST_WEEK);
            else
                sb.Append(VOC_FORECAST_FOR);

            string url = @"https://pocasie.aktuality.sk/banska-bystrica/"; // todo change to worldwide
            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load(url);

                if (plusDayX == 0)
                {
                    for (int i = 1; i <= 6; i++)
                    {
                        // String.Format(" {0}, " + VOC_TODAY + " {1}, " + VOC_TOMORROW + " {2}, {3}" + ", pravdepodobnosť zrážok {4} ", temperature.Replace("C", " " + VOC_CELSIUS), todaysForecast, tommorrowState, tommorrowTemperature.Replace("C", " " + VOC_CELSIUS).Replace("/", VOC_TEMPERATURE_TO), rainPercentual);
                        CultureInfo ci = new CultureInfo(VOC_CULTURE);
                        string dayString = DateTime.Today.AddDays(i).ToString("dddd", ci);
                        sb.Append($" {dayString} "); // stvrtok
                        sb.Append(doc.DocumentNode.SelectNodes($"//*[@id=\"content\"]/section[2]/div/div/ul/li[{i}]/div/div[2]/div[2]/span[2]").First().InnerText); // slnecno
                        sb.Append(", ");
                        sb.Append(doc.DocumentNode.SelectNodes($"//*[@id=\"content\"]/section[2]/div/div/ul/li[{i}]/div/div[2]/div[2]/span[1]").First().InnerText.Split(' ').First() + "°, "); // 25 stupnov
                        sb.Append(doc.DocumentNode.SelectNodes($"//*[@id=\"content\"]/section[2]/div/div/ul/li[{i}]/div/div[3]/span[2]").First().InnerText.Split('/').First().Trim() + " "); // Zrazky: 0%
                        sb.Append(", ");
                    }
                }
                else
                {
                    int i = plusDayX;
                    string dayString = DateTime.Today.AddDays(i).ToString("dddd, d. MMMM");
                    sb.Append($" {dayString}. "); // stvrtok
                    sb.Append(doc.DocumentNode.SelectNodes($"//*[@id=\"content\"]/section[2]/div/div/ul/li[{i}]/div/div[2]/div[2]/span[2]").First().InnerText); // slnecno
                    sb.Append(", ");
                    sb.Append(doc.DocumentNode.SelectNodes($"//*[@id=\"content\"]/section[2]/div/div/ul/li[{i}]/div/div[2]/div[2]/span[1]").First().InnerText.Split(' ').First() + "°, "); // 25 stupnov
                    sb.Append(doc.DocumentNode.SelectNodes($"//*[@id=\"content\"]/section[2]/div/div/ul/li[{i}]/div/div[3]/span[2]").First().InnerText.Split('/').First().Trim() + " "); // Zrazky: 0%
                    sb.Append(", ");
                }

            }
            catch (Exception e)
            {

            }

            return sb.ToString();
        }

        private void Transmit_Click(object sender, RoutedEventArgs e)
        {
            ProgressTransmit.Visibility = Visibility.Visible;
            ProgressTransmit.IsIndeterminate = true;
            ExtensionMethods.Refresh(this);

            RepeatSetMinutes = 60;

            try
            {
                RepeatSetMinutes = int.Parse(this.RepeatMinutes.Text.Trim());
            }
            catch
            {
                MessageBox.Show("Cannot convert \"{0}\" to number.", this.RepeatMinutes.Text);
            }

            timerRepeat.Stop();
            timerRepeat.Interval = new TimeSpan(0, RepeatSetMinutes, 0);
            timerRepeat.Start();
            timerRepeatUpdate.Start();
            RepeatSince = DateTime.Now.Ticks;

            while (CurrentStatus == Status.RX)
            {
                ExtensionMethods.Refresh(this);
            }

            string transmitText = ReplaceVariables(TransmitText.Text);

            timerTX.Start();

            ExtensionMethods.Refresh(this);

            Process cmd = new Process();
            cmd.StartInfo.FileName = "balcon.exe";
            cmd.StartInfo.Arguments = String.Format("-n Laura -s 3 -t \"{0}\"", transmitText);
            cmd.EnableRaisingEvents = true;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Exited += Proc_Exited;
            cmd.Start();
        }

        public void TransmitOccasionaly(string text)
        {
            ProgressTransmit.Visibility = Visibility.Visible;
            ProgressTransmit.IsIndeterminate = true;
            ExtensionMethods.Refresh(this);

            string transmitText = ReplaceVariables(text);

            timerTX.Start();

            ExtensionMethods.Refresh(this);

            Process cmd = new Process();
            cmd.StartInfo.FileName = "balcon.exe";
            cmd.StartInfo.Arguments = String.Format("-n Laura -s 3 -t \"{0}\"", transmitText);
            cmd.EnableRaisingEvents = true;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Exited += Proc_Exited;
            cmd.Start();
        }

        private string ReplaceVariables(string input)
        {
            string text = input;

            if (text.Contains("[DT]")) text = text.Replace("[DT]", ConstructTimeText());
            if (text.Contains("[WEATHER]")) text = text.Replace("[WEATHER]", ConstructWeatherText());
            if (text.Contains("[PROPAGATION]")) text = text.Replace("[PROPAGATION]", ConstructPropagationText());
            if (text.Contains("[LATINQUOTE]")) text = text.Replace("[LATINQUOTE]", ConstructLatinText());
            if (text.Contains("[TECHHEADLINE]")) text = text.Replace("[TECHHEADLINE]", ConvertToSpeakableWords(ConstructTechHeadlineText()));
            if (text.Contains("[MEETUP]")) text = text.Replace("[MEETUP]", ConstructMeetupText());
            if (text.Contains("[LONGFORECAST]")) text = text.Replace("[LONGFORECAST]", ConstructWeatherTextForDayPlusX(0));
            if (text.Contains("[HF]")) text = text.Replace("[HF]", ConstructPropagationHFText());


            return text;
        }

        public string ConstructMeetupText()
        {
            DateTime lastDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1);
            var diff = lastDay - DateTime.Today;

            int fridaysCount = 0;

            for (DateTime d = DateTime.Today; d <= lastDay; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Friday) fridaysCount++;
            }

            bool lastWeek = (fridaysCount == 1);

            if (DateTime.Now.DayOfWeek == DayOfWeek.Friday)
            {
                if (lastWeek)
                {
                    if (DateTime.Now.Hour >= 20) return "";
                    if (DateTime.Now.Hour >= 16) return " Info. Práve prebieha stretko vo dvore Rumpľa. Si vítaný. ";
                    return " Info. Dnes je posledný piatok v mesiaci. O štvrtej je veľké stretko vo dvore Rumpľa v Podlaviciach. ";
                }
                else
                {
                    if (DateTime.Now.Hour >= 20) return "";
                    if (DateTime.Now.Hour >= 17) return " Info. Práve prebieha stretko v Tanku na Fončorde. Si vítaný. ";
                    return " Info. Dnes je stretko v Tanku na Fončorde o piatej. ";
                }
            }

            return "";
        }

        public string ConvertToSpeakableWords(string input)
        {
            string output = input;
            Dictionary<string, string> conversions = new Dictionary<string, string>();

            try
            {
                string[] conversionLines = System.IO.File.ReadAllLines("conversions.txt");

                foreach (var line in conversionLines)
                {
                    if (!String.IsNullOrEmpty(line) && line.Split(';').Count() == 2) conversions.Add(line.Split(';')[0], line.Split(';')[1]);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("The file could not be read:");
                Debug.WriteLine(e.Message);
            }

            foreach (var conversion in conversions)
            {
                output = output.Replace(conversion.Key, conversion.Value);
            }

            return output;
        }

        private void PlayFile(string intro, string filename, string outtro)
        {
            ProgressTransmit.Visibility = Visibility.Visible;
            ProgressTransmit.IsIndeterminate = true;

            timerTX.Start();

            ExtensionMethods.Refresh(this);

            Process cmd = new Process();
            cmd.StartInfo.FileName = "laura_play.bat";
            cmd.StartInfo.Arguments = String.Format("\"{0}\" \"{1}\" \"{2}\"", intro, filename, outtro);
            cmd.EnableRaisingEvents = true;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Exited += Proc_Exited;
            cmd.Start();
        }


        private void Proc_Exited(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                timerTX.Stop();
                ProgressTransmit.Visibility = Visibility.Collapsed;
            }));         
        }

        private void TimeButton_Click(object sender, RoutedEventArgs e)
        {
            string time = ConstructTimeText();
            TransmitText.Text += time + " ";
        }

        private void MeetButton_Click(object sender, RoutedEventArgs e)
        {
            TransmitText.Text += String.Format(" Stretko začína za pol hodinu. ");
        }

        private void RiffButton_Click(object sender, RoutedEventArgs e)
        {
            PlayFile("A teraz, prehráme si riff.", "sounds\\riff1v2.ogg", "Čauky.");
        }

        private void RunRepeat_Click(object sender, RoutedEventArgs e)
        {
            timerRepeat.Stop();
            RepeatSetMinutes = 60;

            try
            {
                RepeatSetMinutes = int.Parse(this.RepeatMinutes.Text.Trim());
            }
            catch
            {
                MessageBox.Show("Cannot convert \"{0}\" to number.", this.RepeatMinutes.Text);
            }

            timerRepeat.Interval = new TimeSpan(0, RepeatSetMinutes, 0);
            timerRepeat.Start();
            timerRepeatUpdate.Start();
            RepeatSince = DateTime.Now.Ticks;
        }

        private void TimerRepeat_Tick(object sender, EventArgs e)
        {
            Transmit_Click(this, new RoutedEventArgs());
        }

        private void StopRepeat_Click(object sender, RoutedEventArgs e)
        {
            timerRepeat.Stop();
            timerRepeatUpdate.Stop();
        }

        private void WholeHour_Click(object sender, RoutedEventArgs e)
        {
            timerRepeat.Stop();
            timerRepeat.Interval = TimeSpan.FromHours(1) - TimeSpan.FromSeconds(DateTime.Now.Minute * 60 + DateTime.Now.Second - 5);
            timerRepeat.Start();
            timerRepeatUpdate.Start();
            RepeatSince = DateTime.Now.Ticks;
        }

        private void HFButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
