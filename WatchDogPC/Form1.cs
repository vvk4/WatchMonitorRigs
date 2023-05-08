using connect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Globalization;
using System.Reflection;
using System.Deployment.Application;
using System.Diagnostics;

namespace WatchDogPC
{
    public partial class Form1 : Form
    {
        private static string URL_ROOT = "https://api2.nicehash.com";//"https://api-test.nicehash.com"; 
        private static string ORG_ID = "";//"facfcb77-d472-4cfe-b5ac-90b66b02bed3";//"f0ec0aab-7870-416e-8380-df48f32aabbe";//"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        private static string API_KEY = "";//"7d94c66d-4180-4d71-8350-0f73c4b3191c";//"491f2faf-845b-4176-812a-efe79a809cc3";//"ffffffff-gggg-hhhh-iiii-jjjjjjjjjjjj";
        private static string API_SECRET = "";//"2bac8f2a-a02b-4d22-ba39-c4e0df93f0dfc6d7c503-3fc1-4477-b2b4-f0af7fb75d9c";//"7326ccb9-e623-431d-b27a-2be862e348cc48d96bf7-b92a-4b40-80a6-b9dd694a3414";//"kkkkkkkk-llll-mmmm-nnnn-oooooooooooooooooooo-pppp-qqqq-rrrr-ssssssssssss";
        Thread portsThread, ReceiveThread;
        bool Connected = false, gettingPacket = false, WaitForReply, SettingsReceived, NoGPUsMonitoringFlag, FillComboGPUsFl;
        bool GetWatchRigWorkerFlag, SettingDevicesWorker, GettingWatchRigs, NiceInfoWorkFlag, RestartedGPUs, AllRigsInfo, GetWatchRigWorkerReady;
        bool MiningStatusMonitoring, TemperatureMonitoring, HashrateMonitoring, GPUsMonitoring, InternetMonitoring, RigDataToGridFl;
        byte[] TrmArray = new byte[65535];
        byte[] serialRecBuffer = new byte[500];
        UInt16 CntTrmArray, cntRec, TestCOMsTimeOut, TimerMonitorCnt, CntProgressBar, CntTestConnection, CntGPUs, TestInternetCnt;
        public UInt32 StatusFlags;
        bool KeysReceived = false, Monitoring, TooHotToTelegramm, MonitorAllRigs, SendStartingOff, enableOTA, WiFiConnected, TestInternet, FillStatusFl, FillStatusFlLf;
        bool GetAllRigsInfo = false, SendMsgList = false, PowerKeyOn, ResetKeyOn, NoInternetConnection, RestartedInternet, RestartedRejected;
        byte byteFromSerial, byteFromSerialPrev, cntTimeOut, BtPr, ReadyToRecMsg, Keys, RigDataToGridCnt;
        int MiningDevices, TotalDevices, MaxLines, TemperLo, RestartDelay, MessageCnt, DelayCnt, TemperHiLevel, CntHot, ResetCounter, Tsk, CntNoGPUsMonitoring, CntOff;
        int CntNoInternetConnection, ReconnectedWiFiCnt, checkBox9Cnt = 0, CntRejectedSpeed, OffCounterMem, cntXY = 0;
        long CntMonitoring, MonitoringCounter;
        float RejectedSpeed, RejectedSpeedPrev, RejectedSpeedThreshold;
        ulong RestartNoConnTime, CntTrm;
        ulong spikesOnDuration, spikesOffDuration;
        ushort RestartAttempts;
        float etcPriceFl, etcPriceFlAv;
        ushort ESP32comErr, PCcomErr;
        string SSID = null;
        string passwordWiFi = null;
        String FullMsgNiceStr = null;
        String RigMsgNiceStr = null;
        String RigNameToWatch = null;
        String RigIDToWatch = null;
        String SSIDsStr = null;
        String MACStr = null;
        String FirmwareVersion = null;
        String NameFmESP32 = null;
        StreamWriter sw;

        bool DBG = false;


        const byte HEADER1 = 0xff;//0x39;
        const byte HEADER2 = 0xff;//0xC3;
        String StrTmp, IPStr;
        String StrToTelegramm, NewRigName;
        List<String> MonitorList = new List<String>();
        List<String> TemperatureList = new List<String>();
        List<String> HighTemperatureList = new List<String>();
        List<String> IDHighTemperatureList = new List<String>();
        List<String> RSSIList = new List<String>();
        String OrgID, Ky, KySecr;
        String BOTtoken, CHAT_ID;
        String Version;
        RigsGPUInfo[] RgGPUInf;
        HashratesMonitoring[] HM = new HashratesMonitoring[1];
        ProcessesMonitoring[] PM = new ProcessesMonitoring[0];
        RigInfo WatchRigInfo = new RigInfo();


        //STATUS:
        const int ResetRigFl = 1;
        const int PCOffFl = 2;
        const int PCResetFl = 4;
        const int WiFiOn = 8;
        const int spikesFl = 0x10;
        const int testOnOff = 0x20;
        const int switchOnPCWhenStart = 0x40;
        const int debugInfo = 0x80;
        const int telegramEnabled = 0x100;

        uint cntSpikesPeriod, cntSpikesPeriodMem, cntSpikesPeriodMemCnt;



        struct RgData
        {
            public String name;
            public String Algorithm;
            public String MiningStatus;
            public double hashrate;
            public float Temperature;
            public bool ToMonitor;
            public int Counter;
            public bool CounterOn;

        }

        RgData[] RigData = new RgData[100];
        RgData[] RigDataPrev = new RgData[100];

        struct RgList
        {
            public String name;
            public String id;
        }
        List<RgList> RigList = new List<RgList>();

        enum UART1_CMD
        {
            SET_WiFi_NET = 2,
            TEST_CONNECTION,
            GET_WIFI,
            SET_KY,
            GET_KY,
            SET_RESTART_DELAY,
            SET_RESTART_ONOFF,
            TIMER_EXPIRED,
            POWER_KEY_ON,
            POWER_KEY_OFF,
            RESET_KEY_ON,
            RESET_KEY_OFF,
            SET_PC_OFF,
            SET_PC_RESET,
            SET_RIG_NAME,
            STARTING_OFF,
            MESSAGE,
            RESET_RESETCOUNTER,
            SET_TOKEN_CHATID_TELEGRAM,
            GET_TOKEN_CHATID_TELEGRAM,
            CLEAR_FLASH,
            SCAN,
            GET_MAC,
            SET_RESTART_NO_CONN_TIME,
            SET_RESTART_ATTEMPTS,
            CLEAR_ReconnectedWiFiCnt,
            ENABLE_OTA,
            DISABLE_OTA,
            TEST_W,
            WiFi_ON,
            WiFi_OFF,
            SPIKES_ON_DURATION,
            SPIKES_ON,
            TEST_ON_OFF,
            SWITCH_ON_PC_START,
            SPIKES_OFF_DURATION,
            OFF_KEY_COUNTER_MEM,
            DEBUG_INFO,
            ENABLE_TELEGRAM
        };
        enum UART1_CMD_TO_PC
        {
            TEXT_RECEIVED = 1,
            CONNECTION_REPLY,
            WIFI_INFO,
            KY_INFO,
            GET_ALLTEMPERATURE,
            RIG_NAME,
            TOKEN_CHATID_INFO,
            GET_ALLHASHRATES,
            GET_ALLRIGS,
            WIFI_SSIDs,
            MAC_INFO,
            GET_STATUS,
            SEND_OPTIONS_TO_PC,
            STOP_MONITORING,
            START_MONITORING,
            SEND_ETC_PRICE
        };



        public Form1()
        {
            InitializeComponent();
            TrmArray[0] = HEADER1;
            TrmArray[1] = HEADER2;
            //            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //Version = Application.ProductVersion.ToString(); // ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString(); 

            ToolTip toolTip1 = new ToolTip();

            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 500;
            toolTip1.ReshowDelay = 500;
            toolTip1.ShowAlways = true;

            // Set up the ToolTip text for the Button and Checkbox.
            toolTip1.SetToolTip(this.button12, "Restart PC by power off and then on.");

            ToolTip toolTip2 = new ToolTip();

            toolTip2.AutoPopDelay = 5000;
            toolTip2.InitialDelay = 500;
            toolTip2.ReshowDelay = 500;
            toolTip2.ShowAlways = true;

            // Set up the ToolTip text for the Button and Checkbox.
            toolTip2.SetToolTip(this.button13, "Restart PC by reset line.");



            portsThread = new Thread(new ThreadStart(Prts));
            portsThread.Start();

            RigNameToWatch = Properties.Settings.Default.RigName_Options;
            RigIDToWatch = Properties.Settings.Default.RigID_Options;
            MiningDevices = Properties.Settings.Default.MiningDevices;
            TotalDevices = Properties.Settings.Default.TotalDevices;

            RigData[0].ToMonitor = Properties.Settings.Default.ToMonitor0;
            RigData[1].ToMonitor = Properties.Settings.Default.ToMonitor1;
            RigData[2].ToMonitor = Properties.Settings.Default.ToMonitor2;
            RigData[3].ToMonitor = Properties.Settings.Default.ToMonitor3;
            RigData[4].ToMonitor = Properties.Settings.Default.ToMonitor4;
            RigData[5].ToMonitor = Properties.Settings.Default.ToMonitor5;
            RigData[6].ToMonitor = Properties.Settings.Default.ToMonitor6;
            RigData[7].ToMonitor = Properties.Settings.Default.ToMonitor7;
            RigData[8].ToMonitor = Properties.Settings.Default.ToMonitor8;
            RigData[9].ToMonitor = Properties.Settings.Default.ToMonitor9;
            RigData[10].ToMonitor = Properties.Settings.Default.ToMonitor10;
            RigData[11].ToMonitor = Properties.Settings.Default.ToMonitor11;
            RigData[12].ToMonitor = Properties.Settings.Default.ToMonitor12;
            RigData[13].ToMonitor = Properties.Settings.Default.ToMonitor13;
            RigData[14].ToMonitor = Properties.Settings.Default.ToMonitor14;
            RigData[15].ToMonitor = Properties.Settings.Default.ToMonitor15;
            RigData[16].ToMonitor = Properties.Settings.Default.ToMonitor16;
            RigData[17].ToMonitor = Properties.Settings.Default.ToMonitor17;
            RigData[18].ToMonitor = Properties.Settings.Default.ToMonitor18;
            RigData[19].ToMonitor = Properties.Settings.Default.ToMonitor19;
            RigData[20].ToMonitor = Properties.Settings.Default.ToMonitor20;
            RigData[21].ToMonitor = Properties.Settings.Default.ToMonitor21;
            RigData[22].ToMonitor = Properties.Settings.Default.ToMonitor22;
            ReadHM();
            RestoreMonitoringProcesses();
            FillLowThresholdcomboBox();

            MaxLines = Properties.Settings.Default.MaxLines;
            Monitoring = Properties.Settings.Default.Monitoring;
            CntMonitoring = Properties.Settings.Default.CntMonitoring;
            TemperLo = Properties.Settings.Default.TemperLo;
            RestartDelay = Properties.Settings.Default.RestartDelay;
            TemperHiLevel = Properties.Settings.Default.TemperHiLevel;
            MonitorAllRigs = Properties.Settings.Default.MonitorAllRigs;
            MiningStatusMonitoring = Properties.Settings.Default.MiningStatusMonitoring;
            TemperatureMonitoring = Properties.Settings.Default.TemperatureMonitoring;
            HashrateMonitoring = Properties.Settings.Default.HashrateMonitoring;
            GPUsMonitoring = Properties.Settings.Default.GPUsMonitoring;
            InternetMonitoring = Properties.Settings.Default.InternetMonitoring;
            AllRigsInfo = Properties.Settings.Default.AllRigsInfo;
            RejectedSpeedThreshold = Properties.Settings.Default.RejectedSpeedThreshold;


            if (AllRigsInfo)
                checkBox8.Checked = true;
            else
                checkBox8.Checked = false;

            if (MiningStatusMonitoring)
                checkBox3.Checked = true;
            else
                checkBox3.Checked = false;
            if (TemperatureMonitoring)
                checkBox4.Checked = true;
            else
                checkBox4.Checked = false;
            if (HashrateMonitoring)
                checkBox5.Checked = true;
            else
                checkBox5.Checked = false;
            if (GPUsMonitoring)
                checkBox6.Checked = true;
            else
                checkBox6.Checked = false;
            if (InternetMonitoring)
                checkBox7.Checked = true;
            else
                checkBox7.Checked = false;



            if (MonitorAllRigs)
                checkBox2.Checked = true;
            else
                checkBox2.Checked = false;
            textBox7.Text = MaxLines.ToString();
            textBox8.Text = TemperLo.ToString();
            textBox9.Text = RestartDelay.ToString();
            textBox11.Text = TemperHiLevel.ToString();
            textBox16.Text = RejectedSpeedThreshold.ToString();
            label9.Text = MiningDevices.ToString();
            label8.Text = RigIDToWatch;
            //label11.Text = RigNameToWatch;
            label13.Text = TotalDevices.ToString();

            textBox1.Text = CntMonitoring.ToString();

            backgroundWorker1.RunWorkerAsync();
            backgroundWorker2.RunWorkerAsync();

            String phrase = "a cold, dark night";
            Console.WriteLine("Before: {0}", phrase);
            phrase = phrase.Remove(0, 5);
            Console.WriteLine("After: {0}", phrase);




            string[] arr = new string[4];
            arr[1] = " ";
            arr[2] = " ";
            arr[3] = " ";

            arr[0] = "Rejected speed";
            ListViewItem itms;
            itms = new ListViewItem(arr);
            listView1.Items.Add(itms);

            arr[0] = "";
            itms = new ListViewItem(arr);
            listView1.Items.Add(itms);



            for (int i = 0; i < TotalDevices; i++)
            {
                //dataGridView1.Rows.Add("Name");
                //dataGridView1.Rows.Add("Mining Status");
                //dataGridView1.Rows.Add("Temperature");
                //dataGridView1.Rows.Add("Algorithm");
                //dataGridView1.Rows.Add("hashrate");
                //dataGridView1.Rows.Add("Counter");
                //dataGridView1.Rows.Add("");

                ListViewItem itm;
                arr[1] = " ";
                arr[2] = " ";
                arr[3] = " ";

                arr[0] = "Name";
                itm = new ListViewItem(arr);
                listView1.Items.Add(itm);

                arr[0] = "Mining Status";
                itm = new ListViewItem(arr);
                listView1.Items.Add(itm);

                arr[0] = "Temperature";
                itm = new ListViewItem(arr);
                listView1.Items.Add(itm);

                arr[0] = "Algorithm";
                itm = new ListViewItem(arr);
                listView1.Items.Add(itm);

                arr[0] = "hashrate";
                itm = new ListViewItem(arr);
                listView1.Items.Add(itm);

                arr[0] = "Counter";
                itm = new ListViewItem(arr);
                listView1.Items.Add(itm);

                arr[0] = "";
                itm = new ListViewItem(arr);
                listView1.Items.Add(itm);
            }

            for (int i = 0; i < listView1.Items.Count; i++)
                listView1.Items[i].UseItemStyleForSubItems = false;

            GetGPUs();
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (checkBox9Cnt != 0)
                    checkBox9Cnt--;

                if (checkBox1.Checked)
                {
                    panel4.Enabled = true;
                }
                else
                {
                    panel4.Enabled = false;
                }



                if (Monitoring)
                {
                    button9.Text = "Stop monitoring";
                    button9.ForeColor = Color.Green;
                }
                else
                {
                    button9.Text = "Start monitoring";
                    button9.ForeColor = Color.Red;
                }



                if (Connected && !SettingsReceived)
                {
                    GetKeys();
                }

                if (Connected)
                {
                    CntProgressBar++;
                    progressBar1.PerformStep();
                    if (CntProgressBar >= 150)
                    {
                        progressBar1.Value = 0;
                        CntProgressBar = 0;
                    }
                    label1.Text = "Connected.";
                    label1.ForeColor = Color.Green;

                    if (CntTestConnection != 0)
                    {
                        CntTestConnection--;
                        if (CntTestConnection == 0)
                        {
                            if (!WaitForReply)
                            {
                                TestConnection();
                                WaitForReply = true;
                                CntTestConnection = 100;
                            }
                            else
                            {
                                WaitForReply = false;
                                Connected = false;
                                SettingsReceived = false;
                            }
                        }
                    }
                    else
                    {
                        TestConnection();
                        WaitForReply = true;
                        CntTestConnection = 100;
                    }

                }
                else
                {
                    progressBar1.Value = 0;
                    CntProgressBar = 0;
                    label1.Text = "Device not found";
                    label1.ForeColor = Color.Red;
                }


                if (TestCOMsTimeOut > 0)
                    TestCOMsTimeOut--;

                if (TimerMonitorCnt > 0)
                {
                    TimerMonitorCnt--;
                    if (TimerMonitorCnt == 0)
                    {
                        for (int i = 0; i < MonitorList.Count; i++)
                        {
                            richTextBox1.Text = richTextBox1.Text + MonitorList[i];
                        }
                        MonitorList.Clear();
                        removeLines();
                    }
                }

                if (cntTimeOut != 0)
                {
                    cntTimeOut--;
                    if (cntTimeOut == 0)
                    {
                        gettingPacket = false;
                    }
                }
            }
            catch
            {
                richTextBox1.AppendText("\r\nEXEPTION!!! ( Timer1 exeption )\r\n");
                SendToTelegramm("Timer1 exeption");
            }

        }


        void Prts()
        {
            while (true)
            {

                try
                {
                    string[] ports = System.IO.Ports.SerialPort.GetPortNames();
                    for (int i = 0; i < ports.Length; i++)
                    {
                        if (!Connected)
                        {

                            if (serialPort1.IsOpen)
                                CloseSerialPort();


                            serialPort1.PortName = ports[i];

                            serialPort1.BaudRate = 460800;//115200;//  256000;//460800;
                            serialPort1.ReadBufferSize = 1000000;

                            serialPort1.Parity = Parity.None;
                            serialPort1.DataBits = 8;
                            serialPort1.StopBits = StopBits.One;
                            serialPort1.Handshake = Handshake.None;

                            serialPort1.ReadTimeout = 100;
                            serialPort1.WriteTimeout = 100;



                            try
                            {
                                serialPort1.Open();
                            }
                            catch (IOException)
                            {
                                CloseSerialPort();
                            }
                            catch (System.UnauthorizedAccessException)
                            {
                                CloseSerialPort();
                            }
                            catch (System.ArgumentOutOfRangeException)
                            {
                                CloseSerialPort();
                            }


                            Thread.Sleep(200);

                            TestCOMsTimeOut = 50;
                            if (serialPort1.IsOpen)
                            {
                                while ((TestCOMsTimeOut != 0) && (!Connected))
                                {
                                    try
                                    {
                                        byteFromSerial = (byte)serialPort1.ReadByte();
                                        if (ReadPacket())
                                            if (serialRecBuffer[4] == (byte)UART1_CMD_TO_PC.CONNECTION_REPLY)
                                            {
                                                if (!Connected)
                                                    SendStartingOff = true;
                                                Connected = true;
                                                GetKeys();
                                            }
                                    }
                                    catch (System.UnauthorizedAccessException)
                                    { }
                                    catch (System.TimeoutException)
                                    { }
                                    catch (System.ArgumentNullException)
                                    { }
                                    catch (System.InvalidOperationException)
                                    { }
                                    catch (System.IO.IOException)
                                    {
                                        CloseSerialPort();
                                    }

                                }
                            }
                        }
                        else
                            Thread.Sleep(1);


                    }
                }
                catch (System.IO.IOException)
                { }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox4.PasswordChar == '\0')
                {
                    button3.Image = Image.FromFile("C:\\Prog\\WatchDog\\PC\\WatchDogPC\\ShowPass.png");
                    textBox4.PasswordChar = '*';
                }
                else
                {
                    button3.Image = Image.FromFile("C:\\Prog\\WatchDog\\PC\\WatchDogPC\\HidePass.png");
                    textBox4.PasswordChar = '\0';
                }
            }
            catch (ArgumentException w)
            {
                richTextBox1.AppendText("\r\nEXEPTION!!! ( password view exeption ):\r\n" + w.Message + "\r\n");
                //SendToTelegramm("password view exeption");
            }

        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.GET_WIFI;
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int i;
            String SSIDSet = comboBox2.Text, passwordSet = textBox4.Text;


            if (SSIDSet.Length == 0)
            {
                MessageBox.Show("SSID is not set", "error", MessageBoxButtons.OK);
                return;
            }

            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.SET_WiFi_NET;


            TrmArray[CntTrmArray++] = (byte)SSIDSet.Length;
            for (i = 0; i < SSIDSet.Length; i++)
            {
                TrmArray[CntTrmArray++] = (byte)SSIDSet[i];
            }

            TrmArray[CntTrmArray++] = (byte)passwordSet.Length;
            for (i = 0; i < passwordSet.Length; i++)
            {
                TrmArray[CntTrmArray++] = (byte)passwordSet[i];
            }

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();

        }


        void SendRigName()
        {
            int i;
            int CntTrmArray;
            String SSIDSet = comboBox2.Text, passwordSet = textBox4.Text;


            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.SET_RIG_NAME;


            TrmArray[CntTrmArray++] = (byte)RigNameToWatch.Length;
            for (i = 0; i < RigNameToWatch.Length; i++)
            {
                TrmArray[CntTrmArray++] = (byte)RigNameToWatch[i];
            }

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();

        }

        private void button8_Click(object sender, EventArgs e)
        {
            int i;
            String OrgID = textBox2.Text, Ky = textBox6.Text, KySecr = textBox5.Text;


            if ((OrgID.Length == 0) || (Ky.Length == 0) || (KySecr.Length == 0))
            {
                MessageBox.Show("Not all fields are filled", "error", MessageBoxButtons.OK);
                return;
            }

            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.SET_KY;


            TrmArray[CntTrmArray++] = (byte)OrgID.Length;
            for (i = 0; i < OrgID.Length; i++)
            {
                TrmArray[CntTrmArray++] = (byte)OrgID[i];
            }

            TrmArray[CntTrmArray++] = (byte)Ky.Length;
            for (i = 0; i < Ky.Length; i++)
            {
                TrmArray[CntTrmArray++] = (byte)Ky[i];
            }

            TrmArray[CntTrmArray++] = (byte)KySecr.Length;
            for (i = 0; i < KySecr.Length; i++)
            {
                TrmArray[CntTrmArray++] = (byte)KySecr[i];
            }

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();

        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox5.PasswordChar == '\0')
                {
                    button7.Image = Image.FromFile("C:\\Prog\\WatchDog\\PC\\WatchDogPC\\ShowPass.png");
                    textBox5.PasswordChar = '*';
                }
                else
                {
                    button7.Image = Image.FromFile("C:\\Prog\\WatchDog\\PC\\WatchDogPC\\HidePass.png");
                    textBox5.PasswordChar = '\0';
                }
            }
            catch (ArgumentException w)
            {
                richTextBox1.AppendText("\r\nEXEPTION!!! ( key view exeption ):\r\n" + w.Message + "\r\n");
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            GetKeys();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1)
                return;
            RgList RgL = RigList[comboBox1.SelectedIndex];
            Properties.Settings.Default.RigName_Options = RgL.name;
            Properties.Settings.Default.RigID_Options = RgL.id;

            RigNameToWatch = Properties.Settings.Default.RigName_Options;
            RigIDToWatch = Properties.Settings.Default.RigID_Options;

            label8.Text = RigIDToWatch;
            //    label11.Text = RigNameToWatch;

            GettingWatchRigs = true;

            GetWatchRig(true);

        }
        void ClearCnt()
        {
            for (int i = 0; i < PM.Count(); i++)
            {
                PM[i].Cnt = 0;
            }
            for (int i = 0; i < RigData.Count(); i++)
            {
                RigData[i].Counter = 0;
            }
            CntNoGPUsMonitoring = CntNoInternetConnection = 0;
        }
        private void button9_Click(object sender, EventArgs e)
        {
            if (!Monitoring)
            {
                ClearCnt();
                MonitoringCounter = 0;
                Monitoring = true;
                Properties.Settings.Default.Monitoring = Monitoring;
                button9.Text = "Stop monitoring";
                button9.ForeColor = Color.Green;
                GetWatchRig(false);
                RigDataToGrid();
            }
            else
            {
                Monitoring = false;
                Properties.Settings.Default.Monitoring = Monitoring;
                button9.Text = "Start monitoring";
                button9.ForeColor = Color.Red;
            }
            Properties.Settings.Default.Save();
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {


        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                CntTrmArray = 4;
                TrmArray[CntTrmArray++] = (byte)UART1_CMD.SET_PC_OFF;
                CntTrmArray -= 2;
                TrmArray[2] = (byte)CntTrmArray;//N
                TrmArray[3] = (byte)(CntTrmArray >> 8);
                TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
                Trm();
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.RESET_RESETCOUNTER;
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();
        }

        private void button16_Click(object sender, EventArgs e)
        {
            //  var Bot = new Telegram.Bot.TelegramBotClient("1964015870:AAEqrZOkRD4nK0M3-vtNtpmF7YUeGQSmAZk");
            //            Bot.GetUpdatesAsync();

            //.SendTextMessageAsync(chat_id, "sdfsdf");

            //            string urlString = $"https://api.telegram.org/bot{apilToken}/sendMessage?chat_id={destID}&text={text}";

            string urlString = "https://api.telegram.org/bot" + textBox12.Text + "/deleteWebhook";

            WebClient webclient = new WebClient();


            try
            {
                String str = webclient.DownloadString(urlString);
            }
            catch (System.Net.WebException m)
            {
                MessageBox.Show(m.Message, "error", MessageBoxButtons.OK);
                return;
            }



            urlString = "https://api.telegram.org/bot" + textBox12.Text + "/getUpdates";




            String st;
            try
            {
                st = webclient.DownloadString(urlString);
            }
            catch (System.Net.WebException m)
            {
                MessageBox.Show(m.Message, "error", MessageBoxButtons.OK);
                return;
            }

            TelegramResponse telegramResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<TelegramResponse>(st);
            if (telegramResponse.result.Count() != 0)
            {
                if (telegramResponse.result[0].my_chat_member != null)
                {
                    textBox13.Text = telegramResponse.result[0].my_chat_member.chat.id;
                    label32.Text = telegramResponse.result[0].my_chat_member.chat.title;
                }
                else
                    if (telegramResponse.result[0].channel_post != null)
                {
                    textBox13.Text = telegramResponse.result[0].channel_post.chat.id;
                    label32.Text = telegramResponse.result[0].channel_post.chat.title;
                }
                else
                {
                    MessageBox.Show("Chat id was not found, error 11", "error", MessageBoxButtons.OK);
                    return;
                }
                label32.ForeColor = Color.Blue;
            }
            else
            {
                MessageBox.Show("Chat id was not found, error 12", "error", MessageBoxButtons.OK);
                return;
            }

        }

        private void button17_Click(object sender, EventArgs e)
        {
            int i;
            String Token = textBox12.Text, ChatID = textBox13.Text;

            String BOTtoken = "-1001302052849";
            int BOTtokens = BOTtoken.Length;


            if (Token.Length == 0)
            {
                MessageBox.Show("Token is not set", "error", MessageBoxButtons.OK);
                return;
            }

            if (ChatID.Length == 0)
            {
                MessageBox.Show("CHAT_ID is not set", "error", MessageBoxButtons.OK);
                return;
            }


            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.SET_TOKEN_CHATID_TELEGRAM;


            TrmArray[CntTrmArray++] = (byte)Token.Length;
            for (i = 0; i < Token.Length; i++)
            {
                TrmArray[CntTrmArray++] = (byte)Token[i];
            }

            TrmArray[CntTrmArray++] = (byte)ChatID.Length;
            for (i = 0; i < ChatID.Length; i++)
            {
                TrmArray[CntTrmArray++] = (byte)ChatID[i];
            }

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();

        }

        private void button18_Click(object sender, EventArgs e)
        {
            DialogResult result;
            result = MessageBox.Show(
            "Are you shure to clear all user data in this microcontroller ?",
            "", MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Yes)
            {
                CntTrmArray = 4;
                TrmArray[CntTrmArray++] = (byte)UART1_CMD.CLEAR_FLASH;
                CntTrmArray -= 2;
                TrmArray[2] = (byte)CntTrmArray;//N
                TrmArray[3] = (byte)(CntTrmArray >> 8);
                TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
                Trm();

                DefaultAll();
            }
        }

        void GetGPUs()
        {
            NoGPUsMonitoringFlag = false;
            richTextBox2.Clear();
            var searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                richTextBox2.AppendText(obj["Name"] + ":\r\n");
                //richTextBox1.AppendText("DeviceID  -  " + obj["DeviceID"] + "\r\n");
                //richTextBox1.AppendText("AdapterRAM  -  " + obj["AdapterRAM"] + "\r\n");
                //richTextBox1.AppendText("AdapterDACType  -  " + obj["AdapterDACType"] + "\r\n");
                //richTextBox1.AppendText("Monochrome  -  " + obj["Monochrome"] + "\r\n");
                //richTextBox1.AppendText("InstalledDisplayDrivers  -  " + obj["InstalledDisplayDrivers"] + "\r\n");
                //richTextBox1.AppendText("DriverVersion  -  " + obj["DriverVersion"] + "\r\n");
                //richTextBox1.AppendText("VideoProcessor  -  " + obj["VideoProcessor"] + "\r\n");
                //richTextBox1.AppendText("VideoArchitecture  -  " + obj["VideoArchitecture"] + "\r\n");
                //richTextBox1.AppendText("VideoMemoryType  -  " + obj["VideoMemoryType"] + "\r\n");
                richTextBox2.AppendText("Status  -  " + obj["Status"] + "\r\n\r\n");
                if (GPUsMonitoring)
                {
                    if ((string)obj["Status"] != "OK")
                        NoGPUsMonitoringFlag = true;
                }
            }
        }


        private void button15_Click_1(object sender, EventArgs e)
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.GET_TOKEN_CHATID_TELEGRAM;
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();
        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.SCAN;
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();

            label38.Text = "Scanning...";
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            label39.Text = RSSIList[comboBox2.SelectedIndex];
        }

        private void button19_Click(object sender, EventArgs e)
        {
            GetNiceInfo(5);
            FillComboGPUsFl = true;

        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                try
                {

                    if (TestInternet)
                    {
                        if (checkBox7.Checked)
                            NoInternetConnection = CheckForInternetConnection();
                        else
                            NoInternetConnection = false;
                        TestInternet = false;
                    }


                    if (GetWatchRigWorkerFlag)
                    {
                        GetWatchRigWorkerFlag = false;
                        GetWatchRigWorker(SettingDevicesWorker);
                        if (FillStatusFl)
                        {
                            FillStatusFl = false;
                            FillStatusFlLf = true;
                        }

                        GetWatchRigWorkerReady = true;
                    }
                    if (NiceInfoWorkFlag)
                    {
                        NiceInfoWorkFlag = false;
                        GetNiceInfoWork(Tsk);
                    }
                    Thread.Sleep(1);
                }
                catch
                {
                    richTextBox1.AppendText("\r\nEXEPTION!!! ( backgroundWorker2 exeption )\r\n");
                    SendToTelegramm("backgroundWorker2 exeption");
                }

            }

        }

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case 1:
                    try
                    {
                        FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                        richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                        removeLines();
                    }
                    catch (Exception ex)
                    { }
                    break;
                case 2:
                    if (AllRigsInfo)
                    {
                        try
                        {
                            richTextBox1.AppendText(RigMsgNiceStr);
                            removeLines();
                        }
                        catch (Exception ex)
                        { }
                    }
                    else
                    {
                        richTextBox1.Text = richTextBox1.Text + "\r\nWatch rig packet received.\r\n";
                    }
                    break;
                case 3:
                    try
                    {
                        richTextBox1.AppendText(RigMsgNiceStr);
                        removeLines();

                        Properties.Settings.Default.MiningDevices = MiningDevices;
                        Properties.Settings.Default.TotalDevices = TotalDevices;

                        Properties.Settings.Default.ToMonitor0 = RigData[0].ToMonitor;
                        Properties.Settings.Default.ToMonitor1 = RigData[1].ToMonitor;
                        Properties.Settings.Default.ToMonitor2 = RigData[2].ToMonitor;
                        Properties.Settings.Default.ToMonitor3 = RigData[3].ToMonitor;
                        Properties.Settings.Default.ToMonitor4 = RigData[4].ToMonitor;
                        Properties.Settings.Default.ToMonitor5 = RigData[5].ToMonitor;
                        Properties.Settings.Default.ToMonitor6 = RigData[6].ToMonitor;
                        Properties.Settings.Default.ToMonitor7 = RigData[7].ToMonitor;
                        Properties.Settings.Default.ToMonitor8 = RigData[8].ToMonitor;
                        Properties.Settings.Default.ToMonitor9 = RigData[9].ToMonitor;
                        Properties.Settings.Default.ToMonitor10 = RigData[10].ToMonitor;
                        Properties.Settings.Default.ToMonitor11 = RigData[11].ToMonitor;
                        Properties.Settings.Default.ToMonitor12 = RigData[12].ToMonitor;
                        Properties.Settings.Default.ToMonitor13 = RigData[13].ToMonitor;
                        Properties.Settings.Default.ToMonitor14 = RigData[14].ToMonitor;
                        Properties.Settings.Default.ToMonitor15 = RigData[15].ToMonitor;
                        Properties.Settings.Default.ToMonitor16 = RigData[16].ToMonitor;
                        Properties.Settings.Default.ToMonitor17 = RigData[17].ToMonitor;
                        Properties.Settings.Default.ToMonitor18 = RigData[18].ToMonitor;
                        Properties.Settings.Default.ToMonitor19 = RigData[19].ToMonitor;
                        Properties.Settings.Default.ToMonitor20 = RigData[20].ToMonitor;
                        Properties.Settings.Default.ToMonitor21 = RigData[21].ToMonitor;
                        Properties.Settings.Default.ToMonitor22 = RigData[22].ToMonitor;

                        label9.Text = MiningDevices.ToString();
                        label13.Text = TotalDevices.ToString();

                        Properties.Settings.Default.Save();

                        //dataGridView1.Rows.Clear();


                        while (listView1.Items.Count != 0)
                        {
                            listView1.Items[listView1.Items.Count - 1].Remove();
                        }

                        string[] arr = new string[4];


                        arr[1] = " ";
                        arr[2] = " ";
                        arr[3] = " ";

                        arr[0] = "Rejected speed";
                        ListViewItem itms;
                        itms = new ListViewItem(arr);
                        listView1.Items.Add(itms);

                        arr[0] = "";
                        itms = new ListViewItem(arr);
                        listView1.Items.Add(itms);



                        for (int i = 0; i < TotalDevices; i++)
                        {
                            //dataGridView1.Rows.Add("Name");
                            //dataGridView1.Rows.Add("Mining Status");
                            //dataGridView1.Rows.Add("Temperature");
                            //dataGridView1.Rows.Add("Algorithm");
                            //dataGridView1.Rows.Add("hashrate");
                            //dataGridView1.Rows.Add("Counter");
                            //dataGridView1.Rows.Add("");


                            ListViewItem itm;
                            arr[1] = " ";
                            arr[2] = " ";
                            arr[3] = " ";

                            arr[0] = "Name";
                            itm = new ListViewItem(arr);
                            listView1.Items.Add(itm);

                            arr[0] = "Mining Status";
                            itm = new ListViewItem(arr);
                            listView1.Items.Add(itm);

                            arr[0] = "Temperature";
                            itm = new ListViewItem(arr);
                            listView1.Items.Add(itm);

                            arr[0] = "Algorithm";
                            itm = new ListViewItem(arr);
                            listView1.Items.Add(itm);

                            arr[0] = "hashrate";
                            itm = new ListViewItem(arr);
                            listView1.Items.Add(itm);

                            arr[0] = "Counter";
                            itm = new ListViewItem(arr);
                            listView1.Items.Add(itm);

                            arr[0] = "";
                            itm = new ListViewItem(arr);
                            listView1.Items.Add(itm);

                        }

                        for (int i = 0; i < listView1.Items.Count; i++)
                            listView1.Items[i].UseItemStyleForSubItems = false;

                        SendRigName();

                        RigDataToGrid();
                    }
                    catch (Exception ex)
                    { }
                    break;

                case 4:
                    try
                    {
                        comboBox1.Items.Clear();

                        for (int i = 0; i < RigList.Count; i++)
                        {
                            RgList RgL = RigList[i];
                            comboBox1.Items.Add(RgL.name);
                            comboBox1.SelectedIndex = 0;
                        }

                        if (Tsk != 4)
                        {
                            if (AllRigsInfo)
                            {
                                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                                removeLines();
                            }
                            else
                            {
                                richTextBox1.Text = richTextBox1.Text + "All rigs packet received.";
                            }
                            TemperatureList.Add("\r\n");
                            for (int j = 0; j < TemperatureList.Count; j++)
                                richTextBox1.Text = richTextBox1.Text + TemperatureList[j];
                            removeLines();
                        }
                        if (Tsk == 2)
                        {
                            if (TooHotToTelegramm)
                            {
                                StrToTelegramm = "";
                                SendMsgList = true;
                                MessageCnt = 1;
                                DelayCnt = 3;
                            }
                        }
                    }
                    catch (Exception ex)
                    { }
                    if (FillComboGPUsFl)
                    {
                        FillComboGPUsFl = false;
                        try
                        {
                            FillCombo3GPUs();
                        }
                        catch (Exception ex)
                        { }
                    }


                    break;

            }
        }

        private void checkBox3_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
                MiningStatusMonitoring = true;
            else
                MiningStatusMonitoring = false;
            Properties.Settings.Default.MiningStatusMonitoring = MiningStatusMonitoring;
            Properties.Settings.Default.Save();
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillCombo5GPUs(comboBox3.SelectedIndex, comboBox4.SelectedIndex);
            try
            {
                label42.Text = RgGPUInf[comboBox3.SelectedIndex].gpu[comboBox4.SelectedIndex].sp[comboBox5.SelectedIndex].spds.ToString();
                label46.Text = RgGPUInf[comboBox3.SelectedIndex].gpu[comboBox4.SelectedIndex].id;
            }
            catch (System.IndexOutOfRangeException)
            {
                //              label42.Text = "0";
                //              label46.Text = "No";
            }
            IndicateHashrateRhreshold();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                FillCombo4GPUs(comboBox3.SelectedIndex);
                label42.Text = RgGPUInf[comboBox3.SelectedIndex].gpu[comboBox4.SelectedIndex].sp[comboBox5.SelectedIndex].spds.ToString();
                label46.Text = RgGPUInf[comboBox3.SelectedIndex].gpu[comboBox4.SelectedIndex].id;
            }
            catch (System.IndexOutOfRangeException)
            {
                //              label42.Text = "0";
                //             label46.Text = "No";
            }
            IndicateHashrateRhreshold();
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                label42.Text = RgGPUInf[comboBox3.SelectedIndex].gpu[comboBox4.SelectedIndex].sp[comboBox5.SelectedIndex].spds.ToString();
                label46.Text = RgGPUInf[comboBox3.SelectedIndex].gpu[comboBox4.SelectedIndex].id;
            }
            catch (System.IndexOutOfRangeException)
            { }
            IndicateHashrateRhreshold();
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                //CntMonitoring = long.Parse(textBox1.Text);
                //Properties.Settings.Default.CntMonitoring = CntMonitoring;

                //Properties.Settings.Default.Save();

                if ((comboBox4.Text == null) || (comboBox4.Text == ""))
                {
                    richTextBox1.AppendText("Error: name of GPU is not set" + "\r\n");
                    removeLines();
                    return;
                }
                if ((label46.Text == null) || (label46.Text == ""))
                {
                    richTextBox1.AppendText("Error: GPU ID is not set" + "\r\n");
                    removeLines();
                    return;
                }


                int index = 0;
                bool tmpfl = false;
                float tmp;


                HM[HM.Length - 1] = new HashratesMonitoring(false);

                CultureInfo ci = new CultureInfo("");
                CultureInfo[] providers = { new CultureInfo("en-US"), ci };

                try
                {
                    tmp = float.Parse(textBox3.Text, providers[0]);
                }
                catch (FormatException)
                {
                    richTextBox1.AppendText("Error: invalid format" + "\r\n");
                    removeLines();
                    return;
                }


                for (int i = 0; i < HM.Length; i++)
                {
                    if (label46.Text == HM[i].GPUid)
                    {
                        index = i;
                        tmpfl = true;
                        if (tmp == 0)
                        {
                            for (int j = i; j < HM.Length - 1; j++)
                                HM[j] = HM[j + 1];
                            Array.Resize(ref HM, HM.Length - 1);
                            richTextBox1.AppendText("Total GPU hashrate low thresholds to monitor: " + (HM.Length - 1).ToString() + "\r\n\r\n");
                            removeLines();
                        }
                        break;
                    }
                }
                if (tmp != 0)
                {
                    if (!tmpfl)
                        index = HM.Length - 1;

                    HM[index].hashrateThreshold = tmp;
                    HM[index].nameGPU = comboBox4.Text;
                    HM[index].GPUid = label46.Text;
                    HM[index].algorithm = comboBox5.Text;
                    HM[index].Rigname = comboBox3.Text;

                    richTextBox1.AppendText("Set low hashrate threshold to" + HM[index].hashrateThreshold.ToString() + "\r\n");
                    richTextBox1.AppendText("GPU: " + HM[index].nameGPU + "\r\n");
                    richTextBox1.AppendText("GPUid: " + HM[index].GPUid + "\r\n\r\n");

                    if (!tmpfl)
                        Array.Resize(ref HM, HM.Length + 1);
                }
                else
                    richTextBox1.AppendText("GPU removed" + "\r\n\r\n");

                richTextBox1.AppendText("Total GPU hashrate low thresholds to monitor: " + (HM.Length - 1).ToString() + "\r\n\r\n");
                removeLines();
                SaveHM();
                ReadHM();
                FillLowThresholdcomboBox();
            }

            if ((!Char.IsDigit(e.KeyChar)) && (e.KeyChar != 8) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

        }
        void SaveHM()
        {
            String Str = "";
            for (int i = 0; i < HM.Length; i++)
            {
                Str = Str + HM[i].GPUid + '%' + HM[i].nameGPU + '%' + HM[i].hashrateThreshold.ToString() + '%' + HM[i].Rigname + '%' + HM[i].algorithm + ';';
            }
            Str = Str.Substring(0, Str.Length - 1);

            Properties.Settings.Default.HM = Str;

            Properties.Settings.Default.Save();
        }
        void ReadHM()
        {
            String[] St = Properties.Settings.Default.HM.Split(';');

            if (HM.Length > 1)
                Array.Resize(ref HM, 1);

            int k = St.Length - 1;
            if (k > 0)
                for (int i = 0; i < k; i++)
                {
                    String[] S = St[i].Split('%');
                    try
                    {
                        HM[i].GPUid = S[0];
                        HM[i].nameGPU = S[1];
                        HM[i].hashrateThreshold = float.Parse(S[2]);
                        HM[i].Rigname = S[3];
                        HM[i].algorithm = S[4];
                        Array.Resize(ref HM, HM.Length + 1);
                    }
                    catch (System.IndexOutOfRangeException)
                    { }
                }

        }
        void IndicateHashrateRhreshold()
        {
            textBox3.Text = "0";
            for (int i = 0; i < HM.Length; i++)
            {
                if (HM[i].GPUid == label46.Text)
                    textBox3.Text = HM[i].hashrateThreshold.ToString();
            }
        }

        private void button21_Click(object sender, EventArgs e)
        {
            MACStr = "";
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.GET_MAC;
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();

            if (sw == null)
            {
                saveFileDialog1.FileName = "";
                saveFileDialog1.ShowDialog();
                if (MACStr != "")
                {
                    if (saveFileDialog1.FileName != "")
                    {
                        sw = new StreamWriter(saveFileDialog1.FileName, true, Encoding.ASCII);

                        String[] St = MACStr.Split(':');
                        sw.Write("\r\n" + "0x" + St[4] + ", " + "0x" + St[5] + ", //" + MACStr);

                        sw.Close();
                        sw = null;
                    }
                }
                else
                    MessageBox.Show("Не получен MAC адрес", "error", MessageBoxButtons.OK);

            }
            else
            {
                sw.Close();
                sw = null;
            }

        }

        private void textBox14_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                RestartNoConnTime = ulong.Parse(textBox14.Text);

                if ((RestartNoConnTime == 0) || (RestartNoConnTime > 1000000))
                {
                    MessageBox.Show("Wrong number", "error", MessageBoxButtons.OK);
                    return;
                }
                ulong Tmp = RestartNoConnTime;
                Tmp = Tmp * 1000;
                CntTrmArray = 4;
                TrmArray[CntTrmArray++] = (byte)UART1_CMD.SET_RESTART_NO_CONN_TIME;

                TrmArray[CntTrmArray++] = (byte)Tmp;
                TrmArray[CntTrmArray++] = (byte)(Tmp >> 8);
                TrmArray[CntTrmArray++] = (byte)(Tmp >> 16);
                TrmArray[CntTrmArray++] = (byte)(Tmp >> 24);

                CntTrmArray -= 2;
                TrmArray[2] = (byte)CntTrmArray;//N
                TrmArray[3] = (byte)(CntTrmArray >> 8);
                TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
                Trm();

                richTextBox1.AppendText("Set restart PC when lost connection " + textBox14.Text + " sec\r\n");
                removeLines();
            }


            if ((!Char.IsDigit(e.KeyChar)) && (e.KeyChar != 8))// && (e.KeyChar != 45))
            {
                e.Handled = true;
            }

        }

        private void textBox15_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                RestartAttempts = ushort.Parse(textBox15.Text);

                if ((RestartAttempts == 0) || (RestartAttempts > 65535))
                {
                    MessageBox.Show("Wrong number", "error", MessageBoxButtons.OK);
                    return;
                }

                CntTrmArray = 4;
                TrmArray[CntTrmArray++] = (byte)UART1_CMD.SET_RESTART_ATTEMPTS;

                TrmArray[CntTrmArray++] = (byte)RestartAttempts;
                TrmArray[CntTrmArray++] = (byte)(RestartAttempts >> 8);

                CntTrmArray -= 2;
                TrmArray[2] = (byte)CntTrmArray;//N
                TrmArray[3] = (byte)(CntTrmArray >> 8);
                TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
                Trm();

                richTextBox1.AppendText("Set restart PC number of attempts: " + textBox15.Text + "\r\n");
                removeLines();
            }


            if ((!Char.IsDigit(e.KeyChar)) && (e.KeyChar != 8))// && (e.KeyChar != 45))
            {
                e.Handled = true;
            }

        }

        private void button22_Click(object sender, EventArgs e)
        {
            try
            {
                Version = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            catch (Exception ex)
            { }

            MessageBox.Show("PC version: " + Version + "\r\n" + FirmwareVersion + "\r\n\r\nby VVKElectro", "Versions", MessageBoxButtons.OK);

        }

        private void button25_Click(object sender, EventArgs e)
        {
            GetProcesses();
        }


        void GetProcesses()
        {
            String[] StrArray = new String[0];
            Process[] processes = Process.GetProcesses();

            int count = 0;
            foreach (Process process in processes)
            {
                Array.Resize(ref StrArray, StrArray.Length + 1);
                StrArray[count++] = process.ProcessName;
                //if (process.Responding)
                //  StrArray[count++] = process.ProcessName + "  ( working )";
                //else
                //StrArray[count++] = process.ProcessName + "  ( not responding )";
            }

            Array.Sort(StrArray);
            listView2.Items.Clear();
            foreach (String str in StrArray)
            {
                if (!isProcessMonitoring(str))
                    listView2.Items.Add(str);
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
            List<int> intList = new List<int>();

            for (int i = 0; i < listView2.Items.Count; i++)
            {
                if (listView2.Items[i].Selected)
                {
                    if (!isProcessMonitoring(listView2.Items[i].Text))
                    {
                        listView3.Items.Add(listView2.Items[i].Text);
                    }
                    intList.Add(i);
                }
            }

            for (int i = intList.Count - 1; i >= 0; i--)
            {
                listView2.Items[intList[i]].Remove();
            }



            String[] StrArray = new String[0];

            int count = 0;
            for (int i = 0; i < listView3.Items.Count; i++)
            {
                Array.Resize(ref StrArray, StrArray.Length + 1);
                StrArray[count++] = listView3.Items[i].Text;
            }


            Array.Sort(StrArray);
            listView3.Items.Clear();

            for (int i = 0; i < StrArray.Length; i++)
            {
                listView3.Items.Add(StrArray[i]);
            }

            label67.Text = listView2.Items.Count.ToString();
            label68.Text = listView3.Items.Count.ToString();

            SaveMonitoringProcesses();
        }

        void SaveMonitoringProcesses()
        {
            String Str = "";

            for (int i = 0; i < listView3.Items.Count; i++)
            {
                Str = Str + listView3.Items[i].Text + "%";
            }
            if (Str.Length > 0)
                Str = Str.Substring(0, Str.Length - 1);


            Properties.Settings.Default.MonitoringProcesses = Str;
            Properties.Settings.Default.Save();

            GetProcessesFromLw3();
        }

        void GetProcessesFromLw3()
        {
            Array.Resize(ref PM, 0);
            for (int i = 0; i < listView3.Items.Count; i++)
            {
                Array.Resize(ref PM, PM.Length + 1);
                PM[i].nameProcess = listView3.Items[i].Text;
                PM[i].Cnt = 0;
            }
        }

        void MonitoringProcesses()
        {
            for (int i = 0; i < PM.Length; i++)
            {
                Process[] localByName = Process.GetProcessesByName(PM[i].nameProcess);
                PM[i].Responding = true;
                if (localByName.Count() == 0)
                    PM[i].Responding = false;
                else
                {
                    foreach (Process process in localByName)
                    {
                        if (!process.Responding)
                            PM[i].Responding = false;
                    }
                }
            }
        }

        int MonitoringProcessesCnt()
        {
            int Cn = 0;
            for (int i = 0; i < PM.Length; i++)
            {
                if (!PM[i].Responding)
                {

                    if (PM[i].Cnt < RestartDelay + 1)
                        PM[i].Cnt++;

                    if (PM[i].Cnt == RestartDelay)
                    {
                        StrToTelegramm = "\r\n" + "Rig not mining" + "\r\n" + "Name: " + RigNameToWatch + "\r\n" + "ID: " + RigIDToWatch + "\r\n" +
    "Process ( " + PM[i].nameProcess + " ) is not responding\r\n";
                        if ((StatusFlags & ResetRigFl) == 1)
                            StrToTelegramm = StrToTelegramm + "This rig will be restarted" + "\r\n" + "\r\n";

                        richTextBox1.AppendText("Process ( " + PM[i].nameProcess + " ) is not responding. RESET!!!\r\n");
                        OffResetPC();
                        removeLines();
                    }
                }
                else
                    PM[i].Cnt = 0;
                if (Cn < PM[i].Cnt)
                    Cn = PM[i].Cnt;
            }
            return Cn;
        }


        void RestoreMonitoringProcesses()
        {
            String Str = Properties.Settings.Default.MonitoringProcesses;
            if ((Str == "") || (Str == null))
                return;
            String[] S = Str.Split('%');
            listView3.Items.Clear();

            for (int i = 0; i < S.Length; i++)
            {
                listView3.Items.Add(S[i]);
            }
            GetProcessesFromLw3();
        }


        bool isProcessMonitoring(String str)
        {
            for (int i = 0; i < listView3.Items.Count; i++)
            {
                if (listView3.Items[i].Text == str)
                {
                    return true;
                }
            }
            return false;
        }

        private void button27_Click(object sender, EventArgs e)
        {
            List<int> intList = new List<int>();

            for (int i = 0; i < listView3.Items.Count; i++)
            {
                if (listView3.Items[i].Selected)
                {
                    intList.Add(i);
                }
            }

            for (int i = intList.Count - 1; i >= 0; i--)
            {
                listView3.Items[intList[i]].Remove();
            }

            GetProcesses();
            label67.Text = listView2.Items.Count.ToString();
            label68.Text = listView3.Items.Count.ToString();
            SaveMonitoringProcesses();
        }

        private void label64_Click(object sender, EventArgs e)
        {

        }

        private void button29_Click(object sender, EventArgs e)
        {
            chart1.Series[0].Points.Clear();
            chart1.Series[1].Points.Clear();

        }

        private void textBox17_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                spikesOnDuration = (ulong.Parse(textBox17.Text)) * 1000;

                if (spikesOnDuration == 0)
                {
                    MessageBox.Show("Wrong number", "error", MessageBoxButtons.OK);
                    return;
                }
                CntTrmArray = 4;
                TrmArray[CntTrmArray++] = (byte)UART1_CMD.SPIKES_ON_DURATION;

                TrmArray[CntTrmArray++] = (byte)spikesOnDuration;
                TrmArray[CntTrmArray++] = (byte)(spikesOnDuration >> 8);
                TrmArray[CntTrmArray++] = (byte)(spikesOnDuration >> 16);
                TrmArray[CntTrmArray++] = (byte)(spikesOnDuration >> 24);

                CntTrmArray -= 2;
                TrmArray[2] = (byte)CntTrmArray;//N
                TrmArray[3] = (byte)(CntTrmArray >> 8);
                TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
                Trm();

                richTextBox1.AppendText("Set spikesOnDuration: " + textBox17.Text + " sec\r\n");
                removeLines();
            }


            if ((!Char.IsDigit(e.KeyChar)) && (e.KeyChar != 8))// && (e.KeyChar != 45))
            {
                e.Handled = true;
            }

        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.SPIKES_ON;

            if (checkBox11.Checked)
            {
                TrmArray[CntTrmArray++] = 1;
                Monitoring = false;
                Properties.Settings.Default.Monitoring = Monitoring;
                button9.Text = "Start monitoring";
                button9.ForeColor = Color.Red;
                checkBox1.Checked = false;
            }
            else
                TrmArray[CntTrmArray++] = 0;

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();

        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.TEST_ON_OFF;

            if (checkBox12.Checked)
                TrmArray[CntTrmArray++] = 1;
            else
                TrmArray[CntTrmArray++] = 0;

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();

        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.SWITCH_ON_PC_START;

            if (checkBox13.Checked)
                TrmArray[CntTrmArray++] = 1;
            else
                TrmArray[CntTrmArray++] = 0;

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();
        }

        private void checkBox14_CheckedChanged(object sender, EventArgs e)
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.DEBUG_INFO;

            if (checkBox14.Checked)
                TrmArray[CntTrmArray++] = 1;
            else
                TrmArray[CntTrmArray++] = 0;

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();
        }

        private void checkBox15_CheckedChanged(object sender, EventArgs e)
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.ENABLE_TELEGRAM;
            if (checkBox15.Checked)
                TrmArray[CntTrmArray++] = 1;
            else
                TrmArray[CntTrmArray++] = 0;

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void textBox19_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                OffCounterMem = (int.Parse(textBox19.Text)) * 1000;

                if (OffCounterMem == 0)
                {
                    MessageBox.Show("Wrong number", "error", MessageBoxButtons.OK);
                    return;
                }
                CntTrmArray = 4;
                TrmArray[CntTrmArray++] = (byte)UART1_CMD.OFF_KEY_COUNTER_MEM;

                TrmArray[CntTrmArray++] = (byte)OffCounterMem;
                TrmArray[CntTrmArray++] = (byte)(OffCounterMem >> 8);

                CntTrmArray -= 2;
                TrmArray[2] = (byte)CntTrmArray;//N
                TrmArray[3] = (byte)(CntTrmArray >> 8);
                TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
                Trm();

                richTextBox1.AppendText("Set OffCounterMem: " + textBox19.Text + " sec\r\n");
                removeLines();

            }
        }

        private void textBox18_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                spikesOffDuration = (ulong.Parse(textBox18.Text)) * 1000;


                CntTrmArray = 4;
                TrmArray[CntTrmArray++] = (byte)UART1_CMD.SPIKES_OFF_DURATION;

                TrmArray[CntTrmArray++] = (byte)spikesOffDuration;
                TrmArray[CntTrmArray++] = (byte)(spikesOffDuration >> 8);
                TrmArray[CntTrmArray++] = (byte)(spikesOffDuration >> 16);
                TrmArray[CntTrmArray++] = (byte)(spikesOffDuration >> 24);

                CntTrmArray -= 2;
                TrmArray[2] = (byte)CntTrmArray;//N
                TrmArray[3] = (byte)(CntTrmArray >> 8);
                TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
                Trm();

                richTextBox1.AppendText("Set spikesOffDuration: " + textBox18.Text + " sec\r\n");
                removeLines();
            }
        }

        private void label65_Click(object sender, EventArgs e)
        {

        }

        private void label62_Click(object sender, EventArgs e)
        {

        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void button28_Click(object sender, EventArgs e)
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.TEST_W;
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();

        }

        private void textBox16_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                RejectedSpeedThreshold = (float)int.Parse(textBox16.Text);
                Properties.Settings.Default.RejectedSpeedThreshold = RejectedSpeedThreshold;

                Properties.Settings.Default.Save();


                richTextBox1.AppendText("Set rejected speed threshold threshold to " + textBox16.Text + "\r\n");
                removeLines();
            }


            if ((!Char.IsDigit(e.KeyChar)) && (e.KeyChar != 8) && (e.KeyChar != 45))
            {
                e.Handled = true;
            }

        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void label63_Click(object sender, EventArgs e)
        {

        }

        private void label66_Click(object sender, EventArgs e)
        {

        }

        private void button24_Click(object sender, EventArgs e)
        {
            ESP32comErr = PCcomErr = 0;
            CntTrm = 0;
        }

        private void button20_Click(object sender, EventArgs e)
        {
            Array.Resize(ref HM, 1);
            richTextBox1.AppendText("GPU hashrate low thresholds to monitor are cleared." + "\r\n\r\n");
            removeLines();
            HM[0].Rigname = "";
            HM[0].GPUid = "";
            HM[0].algorithm = "";
            HM[0].hashrateThreshold = 0;
            HM[0].nameGPU = "";
            textBox3.Text = "0";

            comboBox6.Items.Clear();
            SaveHM();
            FillLowThresholdcomboBox();
            comboBox6.Text = "";
            label56.Text = "-";
            label58.Text = "-";

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked)
                AllRigsInfo = true;
            else
                AllRigsInfo = false;

            Properties.Settings.Default.AllRigsInfo = AllRigsInfo;
            Properties.Settings.Default.Save();
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox9Cnt == 0)
            {
                CntTrmArray = 4;
                if (checkBox9.Checked)
                    TrmArray[CntTrmArray++] = (byte)UART1_CMD.ENABLE_OTA;
                else
                    TrmArray[CntTrmArray++] = (byte)UART1_CMD.DISABLE_OTA;
                CntTrmArray -= 2;
                TrmArray[2] = (byte)CntTrmArray;//N
                TrmArray[3] = (byte)(CntTrmArray >> 8);
                TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
                Trm();
            }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.CLEAR_ReconnectedWiFiCnt;
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            label56.Text = HM[comboBox6.SelectedIndex].Rigname;
            label58.Text = HM[comboBox6.SelectedIndex].hashrateThreshold.ToString() + " (" + HM[comboBox6.SelectedIndex].algorithm + ")";
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox7.Checked)
                InternetMonitoring = true;
            else
                InternetMonitoring = false;
            Properties.Settings.Default.InternetMonitoring = InternetMonitoring;
            Properties.Settings.Default.Save();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
                TemperatureMonitoring = true;
            else
                TemperatureMonitoring = false;
            Properties.Settings.Default.TemperatureMonitoring = TemperatureMonitoring;
            Properties.Settings.Default.Save();

        }
        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
                HashrateMonitoring = true;
            else
                HashrateMonitoring = false;
            Properties.Settings.Default.HashrateMonitoring = HashrateMonitoring;
            Properties.Settings.Default.Save();
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox6.Checked)
                GPUsMonitoring = true;
            else
                GPUsMonitoring = false;
            Properties.Settings.Default.GPUsMonitoring = GPUsMonitoring;
            Properties.Settings.Default.Save();
        }

        private void button15_Click(object sender, EventArgs e)
        {
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                CntTrmArray = 4;
                TrmArray[CntTrmArray++] = (byte)UART1_CMD.SET_PC_RESET;
                CntTrmArray -= 2;
                TrmArray[2] = (byte)CntTrmArray;//N
                TrmArray[3] = (byte)(CntTrmArray >> 8);
                TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
                Trm();
            }
        }

        private void checkBox2_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
                MonitorAllRigs = true;
            else
                MonitorAllRigs = false;
            Properties.Settings.Default.MonitorAllRigs = MonitorAllRigs;
            Properties.Settings.Default.Save();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            backgroundWorker1.CancelAsync();
            backgroundWorker2.CancelAsync();
            portsThread.Abort();
            portsThread = null;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                CntMonitoring = long.Parse(textBox1.Text);
                Properties.Settings.Default.CntMonitoring = CntMonitoring;

                Properties.Settings.Default.Save();


                richTextBox1.AppendText("Set monitoring interval to" + textBox1.Text + " sec\r\n");
                removeLines();
            }


            if ((!Char.IsDigit(e.KeyChar)) && (e.KeyChar != 8) && (e.KeyChar != 45))
            {
                e.Handled = true;
            }


        }

        private void button13_Click(object sender, EventArgs e)
        {
            int CntTrmArray = 4;
            if (ResetKeyOn)
            {
                button13.ForeColor = Color.Black;
                TrmArray[CntTrmArray++] = (byte)UART1_CMD.RESET_KEY_OFF;
                ResetKeyOn = false;
            }
            else
            {
                DialogResult result;
                if (checkBox1.Checked && radioButton2.Checked)
                    result = MessageBox.Show(
                    "Are you shure to reset your PC ?",
                    "", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                else
                    result = MessageBox.Show(
                    "The 'Reset rig' and 'Reset' flags will be set. Are you shure to reset your PC ?",
                    "", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (result == DialogResult.Yes)
                {
                    button13.ForeColor = Color.Red;
                    TrmArray[CntTrmArray++] = (byte)UART1_CMD.RESET_KEY_ON;
                    ResetKeyOn = true;
                }
                else
                    return;
            }

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();

        }

        private void button12_Click(object sender, EventArgs e)
        {
            int CntTrmArray;
            CntTrmArray = 4;
            if (PowerKeyOn)
            {
                button12.ForeColor = Color.Black;
                TrmArray[CntTrmArray++] = (byte)UART1_CMD.POWER_KEY_OFF;
                PowerKeyOn = false;
            }
            else
            {
                DialogResult result;
                if (checkBox1.Checked && radioButton1.Checked)
                    result = MessageBox.Show(
                    "Are you shure to switch off your PC ?",
                    "", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                else
                    result = MessageBox.Show(
                    "The 'Reset rig' and 'Off' flags will be set. Are you shure to switch off your PC ?",
                    "", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (result == DialogResult.Yes)
                {
                    button12.ForeColor = Color.Red;
                    TrmArray[CntTrmArray++] = (byte)UART1_CMD.POWER_KEY_ON;
                    PowerKeyOn = true;
                }
                else
                    return;
            }

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();


        }

        private void textBox11_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                TemperHiLevel = int.Parse(textBox11.Text);
                Properties.Settings.Default.TemperHiLevel = TemperHiLevel;

                Properties.Settings.Default.Save();


                richTextBox1.AppendText("Set high threshold of temperature to" + textBox11.Text + "\r\n");
                removeLines();
            }


            if ((!Char.IsDigit(e.KeyChar)) && (e.KeyChar != 8) && (e.KeyChar != 45))
            {
                e.Handled = true;
            }

        }

        void RestartRigCmd(int i)
        {
            if ((RigData[i].MiningStatus == "MINING") && (RigData[i].Temperature >= TemperLo))
                StrToTelegramm = "\r\n" + "Rig not mining" + "\r\n" + "Name: " + RigNameToWatch + "\r\n" + "ID: " + RigIDToWatch + "\r\n" + "GPU: " + RigData[i].name + "\r\n" +
                    "Its parameters:" + "\r\n" + "Mining status: " + RigData[i].MiningStatus + "\r\n" + "Temperature: " + RigData[i].Temperature + "\r\n" +
                    "Algorithm: " + RigData[i].Algorithm + "\r\n" + "Hashrate: " + RigData[i].hashrate + " (it is not changing)" + "\r\n";
            else
            {
                if (RigData[i].Temperature <= TemperLo)
                    StrToTelegramm = "\r\n" + "Rig not mining" + "\r\n" + "Name: " + RigNameToWatch + "\r\n" + "ID: " + RigIDToWatch + "\r\n" + "GPU: " + RigData[i].name + "\r\n" +
                        "Its parameters:" + "\r\n" + "Mining status: " + RigData[i].MiningStatus + "\r\n" + "Temperature: " + RigData[i].Temperature + " (< " + TemperLo.ToString() + ")\r\n" +
                        "Algorithm: " + RigData[i].Algorithm + "\r\n" + "Hashrate: " + RigData[i].hashrate + "\r\n";
                else
                    StrToTelegramm = "\r\n" + "Rig not mining" + "\r\n" + "Name: " + RigNameToWatch + "\r\n" + "ID: " + RigIDToWatch + "\r\n" + "GPU: " + RigData[i].name + "\r\n" +
                        "Its parameters:" + "\r\n" + "Mining status: " + RigData[i].MiningStatus + "\r\n" + "Temperature: " + RigData[i].Temperature + "\r\n" +
                        "Algorithm: " + RigData[i].Algorithm + "\r\n" + "Hashrate: " + RigData[i].hashrate + "\r\n";


            }

            if ((StatusFlags & ResetRigFl) == 1)
                StrToTelegramm = StrToTelegramm + "This rig will be restarted" + "\r\n" + "\r\n";

            richTextBox1.Text = richTextBox1.Text + StrToTelegramm;
            OffResetPC();
            removeLines();
        }

        void RestartRigCmdGPUs()
        {
            StrToTelegramm = "\r\n" + "Rig not mining" + "\r\n" + "Name: " + RigNameToWatch + "\r\n" + "ID: " + RigIDToWatch + "\r\n" +
                "One of GPUs is off." + "\r\n";
            if ((StatusFlags & ResetRigFl) == 1)
                StrToTelegramm = StrToTelegramm + "This rig will be restarted" + "\r\n" + "\r\n";

            richTextBox1.Text = richTextBox1.Text + StrToTelegramm;
            OffResetPC();
            removeLines();
        }
        void RestartRigCmdRejected()
        {
            StrToTelegramm = "\r\n" + "Rig not mining" + "\r\n" + "Name: " + RigNameToWatch + "\r\n" + "ID: " + RigIDToWatch + "\r\n" +
                "Rejected speed is too high" + "\r\n";
            if ((StatusFlags & ResetRigFl) == 1)
                StrToTelegramm = StrToTelegramm + "This rig will be restarted" + "\r\n" + "\r\n";

            richTextBox1.Text = richTextBox1.Text + StrToTelegramm;
            OffResetPC();
            removeLines();
        }

        void RestartRigCmdInternet()
        {
            StrToTelegramm = "\r\n" + "Rig not mining" + "\r\n" + "Name: " + RigNameToWatch + "\r\n" + "ID: " + RigIDToWatch + "\r\n" +
                "No internet connection." + "\r\n";
            if ((StatusFlags & ResetRigFl) == 1)
                StrToTelegramm = StrToTelegramm + "This rig will be restarted" + "\r\n" + "\r\n";

            richTextBox1.Text = richTextBox1.Text + StrToTelegramm;
            OffResetPC();
            removeLines();
        }




        void SendMsgToTelegramm()
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.MESSAGE;

            for (int j = 0; j < StrToTelegramm.Length; j++)
                TrmArray[CntTrmArray++] = (byte)StrToTelegramm[j];
            TrmArray[CntTrmArray++] = 0;
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();
            CntTrm++;
        }

        void OffResetPC()
        {
            richTextBox1.AppendText("SEND RESET!!!  SEND RESET!!!  SEND RESET!!!  \r\n");

            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.TIMER_EXPIRED;

            for (int j = 0; j < StrToTelegramm.Length; j++)
                TrmArray[CntTrmArray++] = (byte)StrToTelegramm[j];
            TrmArray[CntTrmArray++] = 0;
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            int IMax = 0;
            CntOff = 0;
            int CntHM = 0;

            label66.Text = CntTrm.ToString();

            try
            {
                if (RigDataToGridFl && GetWatchRigWorkerReady)
                {
                    GetWatchRigWorkerReady = false;
                    RigDataToGridFl = false;
                    RigDataToGridW();
                }


                if (FillStatusFlLf)
                {
                    FillStatusFlLf = false;
                    FillStatus();
                }

                if (Monitoring)
                    CntOff = MonitoringProcessesCnt();

                if (TestInternetCnt > 0)
                {
                    TestInternetCnt--;
                    if (TestInternetCnt == 0)
                    {
                        TestInternetCnt = 10;
                        TestInternet = true;
                        if (Monitoring)
                            MonitoringProcesses();
                    }
                }
                else
                    TestInternetCnt = 10;
            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 1");
            }
            try
            {
                for (int i = 0; i < HM.Length; i++)
                {
                    if (HM[i].hasrateNok)
                    {
                        if (HM[i].Cnt < RestartDelay)
                            HM[i].Cnt++;
                        if (HM[i].Cnt > CntHM)
                            CntHM = HM[i].Cnt;
                        if (HM[i].Cnt >= RestartDelay)
                        {
                            if (!HM[i].isLow)
                            {
                                richTextBox1.Text = richTextBox1.Text + "\r\n" + "Warning!\r\n" + "Rig " + HM[i].Rigname + "\r\n" + "GPU " + HM[i].nameGPU + " has low hashrate\r\n";
                                TemperatureList.Add("\r\n");
                                TemperatureList.Add("Warning!\r\n");
                                TemperatureList.Add("Rig " + HM[i].Rigname + "\r\n");
                                TemperatureList.Add("GPU " + HM[i].nameGPU + " has low hashrate: " + HM[i].lowHashrate.ToString() + "\r\n");
                                StrToTelegramm = "";
                                SendMsgList = true;
                                MessageCnt = 1;
                                DelayCnt = 3;
                            }
                            HM[i].isLow = true;
                        }
                    }
                    else
                    {
                        HM[i].isLow = false;
                        HM[i].Cnt = 0;
                    }
                }
            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 2");
            }
            try
            {
                label40.Text = CntHM.ToString();


                if (InternetMonitoring && Monitoring)
                {
                    if (NoInternetConnection)
                    {
                        if (!RestartedInternet)
                        {
                            CntNoInternetConnection++;
                            if (CntNoInternetConnection >= RestartDelay)
                            {
                                RestartedInternet = true;
                                RestartRigCmdInternet();
                            }
                        }
                    }
                    else
                    {
                        CntNoInternetConnection = 0;
                        RestartedInternet = false;
                    }
                }
                else
                {
                    CntNoInternetConnection = 0;
                    RestartedInternet = false;
                }
            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 3");
            }
            try
            {
                if (CntGPUs > 0)
                {
                    CntGPUs--;
                    if (CntGPUs == 0)
                    {
                        CntGPUs = 15;
                        GetGPUs();
                    }
                }
                else
                    CntGPUs = 15;
            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 4");
            }
            try
            {
                if (GPUsMonitoring && Monitoring)
                {
                    if (NoGPUsMonitoringFlag)
                    {
                        if (!RestartedGPUs)
                        {
                            CntNoGPUsMonitoring++;
                            if (CntNoGPUsMonitoring >= RestartDelay)
                            {
                                RestartedGPUs = true;
                                RestartRigCmdGPUs();
                            }
                        }
                    }
                    else
                    {
                        CntNoGPUsMonitoring = 0;
                        RestartedGPUs = false;
                    }
                }
                else
                {
                    CntNoGPUsMonitoring = 0;
                    RestartedGPUs = false;
                }
            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 5");
            }
            try
            {
                if (SendStartingOff)
                {
                    SendStartingOff = false;
                    SendStartingOffFunc();
                }
            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 6");
            }
            try
            {
                if (MonitorAllRigs)
                {
                    if (CntHot > 0)
                    {
                        CntHot--;
                        if (CntHot == 0)
                        {
                            CntHot = 63;
                            GetNiceInfo(2);
                        }
                    }
                    else
                        CntHot = 63;
                }
            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 7");
            }
            try
            {
                SendListToTelegramm();
            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 8");
            }
            try
            {
                if (GetAllRigsInfo)
                {
                    GetAllRigsInfo = false;
                    GetNiceInfo(2);
                    MonitoringCounter = CntMonitoring - 1;
                }
            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 9");
            }

            try
            {
                if (Monitoring)
                {
                    for (int i = 0; i < TotalDevices; i++)
                    {
                        if (RigData[i].CounterOn)
                        {
                            if (RigData[i].Counter <= RestartDelay)
                            {
                                if (Connected)
                                {
                                    RigData[i].Counter++;
                                    if (RigData[i].Counter > CntOff)
                                    {
                                        CntOff = RigData[i].Counter;
                                        IMax = i;
                                    }
                                }
                            }
                            else
                            {
                                CntOff = RestartDelay + 1;
                            }
                            if (CntOff == RestartDelay)
                                RestartRigCmd(IMax);
                        }
                        else
                            RigData[i].Counter = 0;
                    }



                    if (RigDataToGridCnt > 0)
                        RigDataToGridCnt--;
                    else
                    {
                        RigDataToGridCnt = 0;
                        for (int i = 0; i < TotalDevices; i++)
                        {
                            //if (Connected)
                            //    dataGridView1.Rows[i * 7 + 5].Cells[3].Value = RigData[i].Counter;
                            //else
                            //    dataGridView1.Rows[i * 7 + 5].Cells[3].Value = "No connection with watchdog";

                            if (Connected)
                                listView1.Items[i * 7 + 7].SubItems[3].Text = RigData[i].Counter.ToString();
                            else
                                listView1.Items[i * 7 + 7].SubItems[3].Text = "No connection with watchdog";
                        }
                    }




                }
            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 10");
            }
            try
            {
                if (KeysReceived && Monitoring)
                {
                    if (MonitoringCounter < CntMonitoring)
                    {
                        MonitoringCounter++;
                        if (MonitoringCounter >= CntMonitoring)
                        {
                            GetWatchRig(false);
                            RigDataToGrid();
                        }
                    }
                    else
                        MonitoringCounter = 0;
                }
            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 11");
            }
            try
            {
                if (CntNoGPUsMonitoring > CntOff)
                    CntOff = CntNoGPUsMonitoring;

                if (CntNoInternetConnection > CntOff)
                    CntOff = CntNoInternetConnection;

                if (Monitoring)
                {
                    if (RejectedSpeed > RejectedSpeedThreshold)
                    {
                        CntRejectedSpeed++;
                        if (CntRejectedSpeed > CntOff)
                            CntOff = CntRejectedSpeed;
                        if (CntRejectedSpeed >= RestartDelay)
                        {
                            if (!RestartedRejected)
                            {
                                RestartedRejected = true;
                                RestartRigCmdRejected();
                            }
                        }
                        listView1.Items[0].SubItems[3].Text = CntRejectedSpeed.ToString();
                        listView1.Items[0].SubItems[3].ForeColor = Color.Red;
                    }
                    else
                    {
                        listView1.Items[0].SubItems[3].Text = "0";
                        listView1.Items[0].SubItems[3].ForeColor = Color.Black;
                        CntRejectedSpeed = 0;
                        RestartedRejected = false;
                    }
                }
                else
                {
                    listView1.Items[0].SubItems[3].Text = "0";
                    listView1.Items[0].SubItems[3].ForeColor = Color.Black;
                    CntRejectedSpeed = 0;
                    RestartedRejected = false;
                }

                label36.Text = CntOff.ToString();
                label22.Text = MonitoringCounter.ToString();

                if (NoGPUsMonitoringFlag)
                    label36.Text = label36.Text + "  Lost GPU!";
                if (NoInternetConnection)
                    label36.Text = label36.Text + "  No internet connection!";

            }
            catch
            {
                if (DBG)
                    SendToTelegramm("exeption 11");
            }

        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.SET_RESTART_ONOFF;

            if (checkBox1.Checked)
                TrmArray[CntTrmArray++] = 1;
            else
                TrmArray[CntTrmArray++] = 0;

            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();


        }

        private void button10_Click(object sender, EventArgs e)
        {
            StrToTelegramm = textBox10.Text;
            SendMsgToTelegramm();
            richTextBox1.AppendText("Sent test message to telegramm\r\n");
        }

        private void textBox9_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                if (textBox9.Text.Length == 0)
                {
                    MessageBox.Show("The field is empty", "error", MessageBoxButtons.OK);
                    return;
                }
                RestartDelay = int.Parse(textBox9.Text);
                Properties.Settings.Default.RestartDelay = RestartDelay;

                Properties.Settings.Default.Save();

                if ((RestartDelay == 0) || (RestartDelay > 65535))
                {
                    MessageBox.Show("Wrong number", "error", MessageBoxButtons.OK);
                    return;
                }

                CntTrmArray = 4;
                TrmArray[CntTrmArray++] = (byte)UART1_CMD.SET_RESTART_DELAY;

                TrmArray[CntTrmArray++] = (byte)RestartDelay;
                TrmArray[CntTrmArray++] = (byte)(RestartDelay >> 8);

                CntTrmArray -= 2;
                TrmArray[2] = (byte)CntTrmArray;//N
                TrmArray[3] = (byte)(CntTrmArray >> 8);
                TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
                Trm();

                richTextBox1.AppendText("Set reset PC time to" + textBox9.Text + " sec\r\n");
                removeLines();
            }


            if ((!Char.IsDigit(e.KeyChar)) && (e.KeyChar != 8) && (e.KeyChar != 45))
            {
                e.Handled = true;
            }


        }

        private void textBox8_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                TemperLo = int.Parse(textBox8.Text);
                Properties.Settings.Default.TemperLo = TemperLo;

                Properties.Settings.Default.Save();


                richTextBox1.AppendText("Set low threshold of temperature to" + textBox8.Text + "\r\n");
                removeLines();
            }


            if ((!Char.IsDigit(e.KeyChar)) && (e.KeyChar != 8) && (e.KeyChar != 45))
            {
                e.Handled = true;
            }

        }

        private void textBox7_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                MaxLines = int.Parse(textBox7.Text);
                Properties.Settings.Default.MaxLines = MaxLines;

                Properties.Settings.Default.Save();


                richTextBox1.AppendText("Set maximum number of lines for log." + "\r\n");
                removeLines();
            }


            if ((!Char.IsDigit(e.KeyChar)) && (e.KeyChar != 8) && (e.KeyChar != 45))
            {
                e.Handled = true;
            }



        }

        void GetKeys()
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.GET_KY;
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();
        }
        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        void CloseSerialPort()
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Close();
                }
                catch (System.IO.IOException)
                { }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Receive();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case (byte)UART1_CMD_TO_PC.TEXT_RECEIVED:
                    break;
                case (byte)UART1_CMD_TO_PC.CONNECTION_REPLY:
                    if ((Keys & 1) == 1)
                    {
                        label26.Text = "Reset key on";
                        label26.ForeColor = Color.Red;
                        button13.ForeColor = Color.Red;
                        ResetKeyOn = true;
                    }
                    else
                    {
                        label26.Text = "Reset key off";
                        label26.ForeColor = Color.Black;
                        button13.ForeColor = Color.Black;
                        ResetKeyOn = false;
                    }
                    if ((Keys & 2) == 2)
                    {
                        label25.Text = "Power key on";
                        label25.ForeColor = Color.Red;
                        button12.ForeColor = Color.Red;
                        PowerKeyOn = true;
                    }
                    else
                    {
                        label25.Text = "Power key off";
                        label25.ForeColor = Color.Black;
                        button12.ForeColor = Color.Black;
                        PowerKeyOn = false;
                    }

                    label28.Text = ResetCounter.ToString();
                    label59.Text = ReconnectedWiFiCnt.ToString();

                    label63.Text = ESP32comErr.ToString();
                    label65.Text = PCcomErr.ToString();

                    if (enableOTA)
                    {
                        checkBox9.Text = "disable firmware updating";
                        checkBox9.ForeColor = Color.Red;
                        if (!checkBox9.Checked)
                        {
                            checkBox9Cnt = 200;
                            checkBox9.Checked = true;
                        }
                    }
                    else
                    {
                        checkBox9.Text = "enable firmware updating";
                        checkBox9.ForeColor = Color.Black;
                        if (checkBox9.Checked)
                        {
                            checkBox9Cnt = 200;
                            checkBox9.Checked = false;
                        }
                    }

                    label61.Text = IPStr;
                    if (WiFiConnected)
                        label61.ForeColor = Color.Green;
                    else
                        label61.ForeColor = Color.Red;


                    WaitForReply = false;




                    break;
                case (byte)UART1_CMD_TO_PC.WIFI_INFO:
                    comboBox2.Text = SSID;
                    textBox4.Text = passwordWiFi;
                    break;
                case (byte)UART1_CMD_TO_PC.KY_INFO:
                    textBox2.Text = OrgID;// "a07c1fe2-3170-416e-8380-df48f32cabb3";
                    textBox6.Text = Ky;// "61ce33e5-74c5-7a26-18a8-d918d2802e6d";
                    textBox5.Text = "";//KySecr;
                    textBox9.Text = RestartDelay.ToString();
                    if ((StatusFlags & ResetRigFl) == ResetRigFl)
                        checkBox1.Checked = true;
                    else
                        checkBox1.Checked = false;

                    if ((StatusFlags & PCOffFl) == PCOffFl)
                        radioButton1.Checked = true;
                    else
                        radioButton1.Checked = false;

                    if ((StatusFlags & PCResetFl) == PCResetFl)
                        radioButton2.Checked = true;
                    else
                        radioButton2.Checked = false;

                    //                    if ((StatusFlags & WiFiOn) == WiFiOn)
                    //                      checkBox10.Checked = true;
                    //                else
                    //                  checkBox10.Checked = false;

                    if ((StatusFlags & spikesFl) == spikesFl)
                        checkBox11.Checked = true;
                    else
                        checkBox11.Checked = false;
                    if ((StatusFlags & testOnOff) == testOnOff)
                        checkBox12.Checked = true;
                    else
                        checkBox12.Checked = false;
                    if ((StatusFlags & switchOnPCWhenStart) == switchOnPCWhenStart)
                        checkBox13.Checked = true;
                    else
                        checkBox13.Checked = false;
                    if ((StatusFlags & debugInfo) == debugInfo)
                        checkBox14.Checked = true;
                    else
                        checkBox14.Checked = false;
                    if ((StatusFlags & telegramEnabled) == telegramEnabled)
                        checkBox15.Checked = true;
                    else
                        checkBox15.Checked = false;




                    break;
                case (byte)UART1_CMD_TO_PC.GET_ALLTEMPERATURE:
                    GetNiceInfo(1);
                    StrToTelegramm = "";
                    SendMsgList = true;
                    MessageCnt = 1;
                    DelayCnt = 3;
                    break;
                case (byte)UART1_CMD_TO_PC.GET_ALLHASHRATES:
                    GetNiceInfo(3);
                    StrToTelegramm = "";
                    SendMsgList = true;
                    MessageCnt = 1;
                    DelayCnt = 3;
                    break;
                case (byte)UART1_CMD_TO_PC.GET_ALLRIGS:
                    GetNiceInfo(4);
                    StrToTelegramm = "";
                    SendMsgList = true;
                    MessageCnt = 1;
                    DelayCnt = 3;
                    break;
                case (byte)UART1_CMD_TO_PC.RIG_NAME:
                    if (NewRigName == RigNameToWatch)
                        richTextBox1.Text = richTextBox1.Text + "Set new rig name: OK" + "\r\n";
                    else
                        richTextBox1.Text = richTextBox1.Text + "Set new rig name: Fail. Please repeat operation" + "\r\n";
                    break;
                case (byte)UART1_CMD_TO_PC.TOKEN_CHATID_INFO:
                    textBox12.Text = BOTtoken;
                    textBox13.Text = CHAT_ID;
                    break;
                case (byte)UART1_CMD_TO_PC.WIFI_SSIDs:
                    SSIDsStr = SSIDsStr.Substring(0, SSIDsStr.Length - 1);
                    //                        .Remove(SSIDsStr.Length - 1);
                    String[] St = SSIDsStr.Split(',');

                    comboBox2.Items.Clear();
                    RSSIList.Clear();

                    foreach (String S in St)
                    {
                        String[] Str = S.Split('.');
                        comboBox2.Items.Add(Str[0]);
                        RSSIList.Add(Str[1]);
                    }

                    label38.Text = "Signal:";

                    comboBox2.SelectedIndex = 0;
                    break;
                case (byte)UART1_CMD_TO_PC.MAC_INFO:
                    richTextBox1.AppendText(MACStr + "\r\n");
                    break;

                case (byte)UART1_CMD_TO_PC.SEND_OPTIONS_TO_PC:
                    textBox14.Text = RestartNoConnTime.ToString();
                    textBox15.Text = RestartAttempts.ToString();

                    textBox17.Text = spikesOnDuration.ToString();
                    textBox18.Text = spikesOffDuration.ToString();
                    textBox19.Text = OffCounterMem.ToString();

                    if (NameFmESP32 == "Your rig name")
                        label11.Text = "Your rig name !!!!!!!!!";
                    else
                        label11.Text = NameFmESP32;
                    break;
                case (byte)UART1_CMD_TO_PC.STOP_MONITORING:
                    if (!Monitoring)
                        SendToTelegramm("Monitoring was stopped earlier");
                    else
                        SendToTelegramm("Monitoring stopped");
                    Monitoring = false;
                    Properties.Settings.Default.Monitoring = Monitoring;
                    button9.Text = "Start monitoring";
                    button9.ForeColor = Color.Red;
                    Properties.Settings.Default.Save();

                    break;
                case (byte)UART1_CMD_TO_PC.START_MONITORING:
                    if (Monitoring)
                        SendToTelegramm("Monitoring was started earlier");
                    else
                        SendToTelegramm("Monitoring started");
                    Monitoring = true;
                    Properties.Settings.Default.Monitoring = Monitoring;
                    button9.Text = "Stop monitoring";
                    button9.ForeColor = Color.Green;
                    GetWatchRig(false);
                    RigDataToGrid();
                    Properties.Settings.Default.Save();
                    break;
                case (byte)UART1_CMD_TO_PC.SEND_ETC_PRICE:

                    chart1.Series[0].Points.AddXY(cntXY, etcPriceFl);
                    if (etcPriceFlAv > 51)
                        chart1.Series[1].Points.AddXY(cntXY, 6);
                    else
                    if (etcPriceFlAv > 49)
                        chart1.Series[1].Points.AddXY(cntXY, 5);


                    cntXY++;


                    TimeSpan interval = TimeSpan.FromMilliseconds(cntSpikesPeriod);
                    label73.Text = interval.Hours.ToString() + ":" + interval.Minutes.ToString() + ":" + interval.Seconds.ToString();// + "::" + interval.Milliseconds.ToString();

                    interval = TimeSpan.FromMilliseconds(cntSpikesPeriodMem);
                    label75.Text = interval.Hours.ToString() + ":" + interval.Minutes.ToString() + ":" + interval.Seconds.ToString();// + "::" + interval.Milliseconds.ToString();

                    interval = TimeSpan.FromMilliseconds(cntSpikesPeriodMem - cntSpikesPeriodMemCnt);
                    label77.Text = interval.Hours.ToString() + ":" + interval.Minutes.ToString() + ":" + interval.Seconds.ToString();// + "::" + interval.Milliseconds.ToString();


                    break;


                default:
                    break;
            }
        }
        public void Trm()
        {
            if (serialPort1.IsOpen == false)
                return;

            try
            {
                serialPort1.Write(TrmArray, 0, (TrmArray[2] + (((int)TrmArray[3]) << 8) + 3));
            }
            catch (System.UnauthorizedAccessException)
            {
                try
                {
                    serialPort1.Close();
                }
                catch (System.IO.IOException)
                {
                }

                return;
            }
            catch (System.TimeoutException)
            {
                try
                {
                    serialPort1.Close();
                }
                catch (System.IO.IOException)
                {
                }

                return;
            }
            catch (System.IO.IOException)
            {
                try
                {
                    serialPort1.Close();
                }
                catch (System.IO.IOException)
                {
                }
                return;
            }

        }




        public byte CalcCheckSumm(byte[] ChkArray, int n, int Strt)
        {
            uint summ = 0, j;

            for (j = 0; j < n; j++)
                summ += ChkArray[j + Strt];

            summ = ~summ;

            return (byte)summ;

        }






        void TrmArrayAddStr(string Str, int size)
        {
            for (int i = 0; i < size; i++)
            {
                TrmArray[CntTrmArray++] = (byte)Str[i];
            }
        }



        bool ReadPacket()
        {
            int NumBytes;

            if (!gettingPacket)
            {
                if ((byteFromSerialPrev == HEADER1) && (byteFromSerial == HEADER2))
                {
                    byteFromSerialPrev = 0;
                    gettingPacket = true;
                    cntRec = 2;
                }
                else
                    byteFromSerialPrev = byteFromSerial;
            }
            else
            {
                if (cntRec > 256)
                    gettingPacket = false;
                else
                {
                    serialRecBuffer[cntRec] = byteFromSerial;
                    cntRec++;
                    if (cntRec > 3)
                    {
                        NumBytes = serialRecBuffer[2] + ((int)serialRecBuffer[3] << 8);
                        if (cntRec > (NumBytes + 2))
                        {
                            gettingPacket = false;

                            byte check = CalcCheckSumm(serialRecBuffer, NumBytes, 2);

                            if (check != serialRecBuffer[serialRecBuffer[2] + 2])
                            {
                                return false;
                            }
                            else
                            {
                                cntTimeOut = 50;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        void Receive()
        {
            while (true)
            {
                try
                {
                    if (Connected)
                    {
                        try
                        {
                            byteFromSerial = (byte)serialPort1.ReadByte();

                            if ((!gettingPacket) && (byteFromSerial != HEADER1))
                            {
                                //StrTmp += Convert.ToChar((int)byteFromSerial) + ' ';
                                StrTmp += Convert.ToChar((int)byteFromSerial).ToString();
                                int ddd = StrTmp.Length;
                            }
                            if ((BtPr == 0xd) && (byteFromSerial == 0xa))
                            {
                                MonitorList.Add(StrTmp);
                                StrTmp = StrTmp.Substring(0, 0);
                                TimerMonitorCnt = 10;
                                //  backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.TEXT_RECEIVED);
                            }
                            BtPr = byteFromSerial;
                            if (ReadPacket())
                                ProcessPacket();
                            else
                                PCcomErr++;
                        }
                        catch (System.UnauthorizedAccessException)
                        { }
                        catch (System.TimeoutException)
                        { }
                        catch (System.ArgumentNullException)
                        { }
                        catch (System.InvalidOperationException)
                        { }
                        catch (System.IO.IOException)
                        {
                            CloseSerialPort();
                            Connected = false;
                            SettingsReceived = false;
                        }
                    }
                    else
                        Thread.Sleep(1);
                }
                catch
                {
                    richTextBox1.AppendText("\r\nEXEPTION!!! ( receive exeption )\r\n");
                    SendToTelegramm("receive exeption");
                }
            }


        }

        unsafe void ProcessPacket()
        {
            switch (serialRecBuffer[4])
            {
                case (byte)UART1_CMD_TO_PC.CONNECTION_REPLY:
                    if (!Connected)
                        SendStartingOff = true;
                    Connected = true;
                    byte[] IPbts = new byte[4];
                    IPbts[0] = serialRecBuffer[5];
                    IPbts[1] = serialRecBuffer[6];
                    IPbts[2] = serialRecBuffer[7];
                    IPbts[3] = serialRecBuffer[8];
                    ReadyToRecMsg = serialRecBuffer[9];
                    Keys = serialRecBuffer[10];
                    IPAddress IPaddr = new IPAddress(IPbts);
                    IPStr = IPaddr.ToString();

                    ResetCounter = ((int)serialRecBuffer[12]) << 8;
                    ResetCounter = ResetCounter + serialRecBuffer[11];

                    ReconnectedWiFiCnt = ((int)serialRecBuffer[14]) << 8;
                    ReconnectedWiFiCnt = ReconnectedWiFiCnt + serialRecBuffer[13];

                    ESP32comErr = (ushort)(((int)serialRecBuffer[16]) << 8);
                    ESP32comErr = (ushort)(ESP32comErr + (ushort)serialRecBuffer[15]);

                    if (serialRecBuffer[17] != 0)
                        enableOTA = true;
                    else
                        enableOTA = false;

                    if (serialRecBuffer[18] != 0)
                        WiFiConnected = true;
                    else
                        WiFiConnected = false;


                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.CONNECTION_REPLY);
                    break;
                case (byte)UART1_CMD_TO_PC.SEND_ETC_PRICE:
                    short ccnt = 5;
                    fixed (byte* Ptr = &serialRecBuffer[ccnt])
                    {
                        float* p = (float*)Ptr;
                        etcPriceFl = *p;
                    }
                    ccnt += 4;
                    fixed (byte* Ptr = &serialRecBuffer[ccnt])
                    {
                        float* p = (float*)Ptr;
                        etcPriceFlAv = *p;
                    }
                    ccnt += 4;

                    fixed (byte* Ptr = &serialRecBuffer[ccnt])
                    {
                        uint* p = (uint*)Ptr;
                        cntSpikesPeriod = *p;
                    }
                    ccnt += 4;

                    fixed (byte* Ptr = &serialRecBuffer[ccnt])
                    {
                        uint* p = (uint*)Ptr;
                        cntSpikesPeriodMem = *p;
                    }
                    ccnt += 4;

                    fixed (byte* Ptr = &serialRecBuffer[ccnt])
                    {
                        uint* p = (uint*)Ptr;
                        cntSpikesPeriodMemCnt = *p;
                    }
                    ccnt += 4;


                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.SEND_ETC_PRICE);

                    break;
                case (byte)UART1_CMD_TO_PC.WIFI_INFO:
                    int SizeSSID = serialRecBuffer[5];
                    int j;
                    SSID = null;

                    for (j = 0; j < SizeSSID; j++)
                    {
                        if (serialRecBuffer[j + 6] != 0)
                            SSID = SSID + (char)serialRecBuffer[j + 6];
                        else
                            j = SizeSSID;
                    }


                    int SizePasswordWiFi = serialRecBuffer[6 + SizeSSID];

                    passwordWiFi = null;

                    for (j = 0; j < SizePasswordWiFi; j++)
                    {
                        if (serialRecBuffer[j + 7 + SizeSSID] != 0)
                        {
                            passwordWiFi = passwordWiFi + (char)serialRecBuffer[j + 7 + SizeSSID];
                        }
                        else
                            j = SizePasswordWiFi;
                    }
                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.WIFI_INFO);
                    break;
                case (byte)UART1_CMD_TO_PC.KY_INFO:
                    int SizeOrgID = serialRecBuffer[5];
                    SettingsReceived = true;

                    OrgID = null;

                    for (j = 0; j < SizeOrgID; j++)
                    {
                        if (serialRecBuffer[j + 6] != 0)
                            OrgID = OrgID + (char)serialRecBuffer[j + 6];
                        else
                            j = SizeOrgID;
                    }


                    int SizeKy = serialRecBuffer[6 + SizeOrgID];

                    Ky = null;

                    for (j = 0; j < SizeKy; j++)
                    {
                        if (serialRecBuffer[j + 7 + SizeOrgID] != 0)
                        {
                            Ky = Ky + (char)serialRecBuffer[j + 7 + SizeOrgID];
                        }
                        else
                            j = SizeKy;
                    }



                    int SizeKySecr = serialRecBuffer[7 + SizeOrgID + SizeKy];

                    KySecr = null;

                    for (j = 0; j < SizeKySecr; j++)
                    {
                        if (serialRecBuffer[j + 8 + SizeOrgID + SizeKy] != 0)
                        {
                            KySecr = KySecr + (char)serialRecBuffer[j + 8 + SizeOrgID + SizeKy];
                        }
                        else
                            j = SizeKySecr;
                    }



                    int Cnt = SizeKySecr + SizeOrgID + SizeKy + 8;
                    RestartDelay = ((UInt16)serialRecBuffer[Cnt + 1]) << 8;
                    RestartDelay = RestartDelay + ((UInt16)serialRecBuffer[Cnt]);
                    Cnt += 2;
                    StatusFlags = ((UInt32)serialRecBuffer[Cnt + 3]) << 24;
                    StatusFlags = StatusFlags + (((UInt32)serialRecBuffer[Cnt + 2]) << 16);
                    StatusFlags = StatusFlags + (((UInt32)serialRecBuffer[Cnt + 1]) << 8);
                    StatusFlags = StatusFlags + (UInt32)serialRecBuffer[Cnt];
                    Cnt += 4;


                    KeysReceived = true;
                    GetAllRigsInfo = true;
                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.KY_INFO);
                    break;
                case (byte)UART1_CMD_TO_PC.GET_ALLTEMPERATURE:
                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.GET_ALLTEMPERATURE);
                    break;
                case (byte)UART1_CMD_TO_PC.GET_ALLHASHRATES:
                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.GET_ALLHASHRATES);
                    break;
                case (byte)UART1_CMD_TO_PC.GET_ALLRIGS:
                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.GET_ALLRIGS);
                    break;
                case (byte)UART1_CMD_TO_PC.RIG_NAME:
                    int SizeRigName = serialRecBuffer[5];

                    NewRigName = "";

                    for (j = 0; j < SizeRigName; j++)
                    {
                        if (serialRecBuffer[j + 6] != 0)
                            NewRigName = NewRigName + (char)serialRecBuffer[j + 6];
                        else
                            j = SizeRigName;
                    }
                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.RIG_NAME);
                    break;
                case (byte)UART1_CMD_TO_PC.TOKEN_CHATID_INFO:
                    int SizeBOTtoken = serialRecBuffer[5];


                    BOTtoken = "";
                    CHAT_ID = "";
                    for (j = 0; j < SizeBOTtoken; j++)
                    {
                        if (serialRecBuffer[j + 6] != 0)
                            BOTtoken = BOTtoken + (char)serialRecBuffer[j + 6];
                        else
                            j = SizeBOTtoken;
                    }


                    int SizeCHAT_ID = serialRecBuffer[6 + SizeBOTtoken];

                    for (j = 0; j < SizeCHAT_ID; j++)
                    {
                        if (serialRecBuffer[j + 7 + SizeBOTtoken] != 0)
                        {
                            CHAT_ID = CHAT_ID + (char)serialRecBuffer[j + 7 + SizeBOTtoken];
                        }
                        else
                            j = SizeCHAT_ID;
                    }
                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.TOKEN_CHATID_INFO);
                    break;
                case (byte)UART1_CMD_TO_PC.WIFI_SSIDs:
                    int SizeSSIDs = serialRecBuffer[5] + (serialRecBuffer[6] << 8);

                    SSIDsStr = "";

                    for (j = 0; j < SizeSSIDs; j++)
                    {
                        if (serialRecBuffer[j + 7] != 0)
                            SSIDsStr = SSIDsStr + (char)serialRecBuffer[j + 7];
                        else
                            j = SizeSSIDs;
                    }
                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.WIFI_SSIDs);
                    break;
                case (byte)UART1_CMD_TO_PC.MAC_INFO:
                    int SizeMAC = serialRecBuffer[5] + (serialRecBuffer[6] << 8);

                    MACStr = "";

                    for (j = 0; j < SizeMAC; j++)
                    {
                        if (serialRecBuffer[j + 7] != 0)
                            MACStr = MACStr + (char)serialRecBuffer[j + 7];
                        else
                            j = SizeMAC;
                    }
                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.MAC_INFO);
                    break;
                case (byte)UART1_CMD_TO_PC.GET_STATUS:
                    GetWatchRig(false);
                    FillStatusFl = true;
                    break;
                case (byte)UART1_CMD_TO_PC.SEND_OPTIONS_TO_PC:
                    int Cntt = 5;
                    RestartNoConnTime = ((ulong)serialRecBuffer[Cntt++] + ((ulong)serialRecBuffer[Cntt++] << 8) + ((ulong)serialRecBuffer[Cntt++] << 16) + ((ulong)serialRecBuffer[Cntt++] << 24));
                    RestartNoConnTime = RestartNoConnTime / 1000;
                    RestartAttempts = (ushort)(serialRecBuffer[Cntt++] + (serialRecBuffer[Cntt++] << 8));


                    int FirmwareVersionLenght = serialRecBuffer[Cntt++];

                    FirmwareVersion = "";

                    for (j = 0; j < FirmwareVersionLenght; j++)
                    {
                        FirmwareVersion = FirmwareVersion + (char)serialRecBuffer[Cntt++];
                    }


                    int NameFmESP32Lenght = serialRecBuffer[Cntt++];

                    NameFmESP32 = "";

                    for (j = 0; j < NameFmESP32Lenght; j++)
                    {
                        NameFmESP32 = NameFmESP32 + (char)serialRecBuffer[Cntt++];
                    }

                    spikesOnDuration = ((ulong)serialRecBuffer[Cntt++] + ((ulong)serialRecBuffer[Cntt++] << 8) + ((ulong)serialRecBuffer[Cntt++] << 16) + ((ulong)serialRecBuffer[Cntt++] << 24));
                    spikesOnDuration = spikesOnDuration / 1000;

                    spikesOffDuration = ((ulong)serialRecBuffer[Cntt++] + ((ulong)serialRecBuffer[Cntt++] << 8) + ((ulong)serialRecBuffer[Cntt++] << 16) + ((ulong)serialRecBuffer[Cntt++] << 24));
                    spikesOffDuration = spikesOffDuration / 1000;

                    OffCounterMem = (int)serialRecBuffer[Cntt++] + ((int)serialRecBuffer[Cntt++] << 8);
                    OffCounterMem = OffCounterMem / 1000;

                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.SEND_OPTIONS_TO_PC);
                    break;
                case (byte)UART1_CMD_TO_PC.STOP_MONITORING:
                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.STOP_MONITORING);
                    break;
                case (byte)UART1_CMD_TO_PC.START_MONITORING:
                    backgroundWorker1.ReportProgress((byte)UART1_CMD_TO_PC.START_MONITORING);
                    break;
                default:
                    break;
            }

        }
        void TestConnection()
        {

            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.TEST_CONNECTION;
            string Str = "12345";
            TrmArrayAddStr(Str, Str.Length);
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            byte tmp = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            TrmArray[CntTrmArray + 2] = tmp;
            Trm();

        }


        void FillStatus()
        {
            TemperatureList.Add("Status of " + label11.Text + ":\r\n");
            TemperatureList.Add("Restarts counter: " + label28.Text + "\r\n");
            TemperatureList.Add("Restart delay counter: " + label36.Text + "\r\n");
            if (Monitoring)
            {
                TemperatureList.Add("Monitoring: YES\r\n");
                TemperatureList.Add(" Monitoring events: \r\n");
                if (checkBox3.Checked)
                    TemperatureList.Add("--mining status\r\n");
                if (checkBox4.Checked)
                    TemperatureList.Add("--GPU temperature\r\n");
                if (checkBox5.Checked)
                    TemperatureList.Add("--GPU hashrate\r\n");
                if (checkBox6.Checked)
                    TemperatureList.Add("--GPU status\r\n");
                if (checkBox7.Checked)
                    TemperatureList.Add("--internet connection\r\n");


                if (PM.Length == 0)
                    TemperatureList.Add("Monitoring Processes: None\r\n");
                else
                {
                    TemperatureList.Add("Monitoring Processes:\r\n");
                    for (int i = 0; i < PM.Length; i++)
                    {
                        bool TmpBool = true;
                        Process[] localByName = Process.GetProcessesByName(PM[i].nameProcess);
                        if (localByName.Count() != 0)
                        {
                            foreach (Process process in localByName)
                            {
                                if (!process.Responding)
                                    TmpBool = false;
                            }
                        }
                        else
                            TmpBool = false;

                        if (TmpBool)
                            TemperatureList.Add("\"" + PM[i].nameProcess + "\" (OK)\r\n");
                        else
                            TemperatureList.Add("\"" + PM[i].nameProcess + "\" (not working)\r\n");


                    }
                }


                if (checkBox1.Checked)
                {
                    TemperatureList.Add(" Restart rig on event: YES\r\n");
                    if (radioButton1.Checked)
                        TemperatureList.Add("--Restart key: Off/On\r\n");
                    if (radioButton2.Checked)
                        TemperatureList.Add("--Restart key: Reset\r\n");
                }
                else
                    TemperatureList.Add(" Restart rig on event: NO\r\n");
            }
            else
                TemperatureList.Add("Monitoring: NO\r\n");

            TemperatureList.Add("WiFi reconnect counter: " + label59.Text + "\r\n\r\n");

            TemperatureList.Add("Monitoring rig: " + WatchRigInfo.name + "\r\n");

            int DevicesCnt = 1;
            if (WatchRigInfo.devices != null)
            {
                foreach (Dev D in WatchRigInfo.devices)
                {
                    TemperatureList.Add(DevicesCnt.ToString() + ":" + "\r\n");
                    TemperatureList.Add("Dev name: " + D.name + "\r\n");
                    TemperatureList.Add("Status: " + D.status.enumName + "\r\n");
                    TemperatureList.Add("Load: " + D.load.ToString() + "\r\n");
                    TemperatureList.Add("Fan: " + D.revolutionsPerMinutePercentage.ToString() + "\r\n");
                    TemperatureList.Add("Temperature: " + D.temperature.ToString().ToString() + "\r\n");

                    foreach (SpeedsRg S in D.speeds)
                    {

                        TemperatureList.Add("Algorithm: " + S.algorithm + "\r\n");
                        TemperatureList.Add("Speed: " + S.speed.ToString() + "\r\n");


                    }

                    if (RigData[DevicesCnt - 1].ToMonitor)
                        TemperatureList.Add("Monitoring: YES" + "\r\n");
                    else
                        TemperatureList.Add("Monitoring: NO" + "\r\n");

                    DevicesCnt++;
                }
            }



            StrToTelegramm = "";
            SendMsgList = true;
            MessageCnt = 1;
            DelayCnt = 3;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GetNiceInfo(2);
            if (TooHotToTelegramm)
            {
                StrToTelegramm = "";
                SendMsgList = true;
                MessageCnt = 1;
                DelayCnt = 3;
            }

        }

        void GetWatchRig(bool SettingDevices)
        {
            GetWatchRigWorkerFlag = true;
            SettingDevicesWorker = SettingDevices;
        }

        void GetWatchRigWorker(bool SettingDevices)
        {
            String StrTmp;
            int Tmp;
            ORG_ID = OrgID;
            API_KEY = Ky;
            API_SECRET = KySecr;
            int RigDataCnt = 0;
            //            NoInternetConnection = false;
            Api api = new Api(URL_ROOT, ORG_ID, API_KEY, API_SECRET);
            string timeResponse = api.get("/api/v2/time");
            if (timeResponse == "")
            {
                FullMsgNiceStr = "No connection with server ( error 3 )" + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                //              NoInternetConnection = true;
                return;
            }

            ServerTime serverTimeObject;
            try
            {
                serverTimeObject = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerTime>(timeResponse);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                FullMsgNiceStr = e.Message + " ( error 9 )" + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                return;
            }
            string time = serverTimeObject.serverTime;

            var time1 = TimeSpan.FromMilliseconds(Convert.ToInt64(time));
            DateTime ServerDateTime = new DateTime(1970, 1, 1) + time1;

            RigMsgNiceStr = "\r\n" + "Nicehash server time: " + ServerDateTime.ToString() + "\r\n";

            //Tmp = richTextBox1.Text.Length;
            //richTextBox1.AppendText(RigMsgNiceStr);
            //richTextBox1.SelectionStart = Tmp;
            //richTextBox1.SelectionLength = RigMsgNiceStr.Length;
            //richTextBox1.SelectionStart = richTextBox1.Text.Length;
            //richTextBox1.ScrollToCaret();

            string RigInfoJSON = api.get("/main/api/v2/mining/rig2/" + RigIDToWatch, true, time);
            if (RigInfoJSON == "")
            {
                FullMsgNiceStr = "No connection with Nicehash server. Check your rig name to watch ( error 4 )" + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                return;
            }
            ErrorMessage ErrorMsg;
            try
            {
                ErrorMsg = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorMessage>(RigInfoJSON);
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                FullMsgNiceStr = "Server error (error 6)" + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                return;
            }
            if (ErrorMsg.error_id != null)
            {
                FullMsgNiceStr = FullMsgNiceStr + "error_id: " + ErrorMsg.error_id + "\r\n";

                foreach (Err E in ErrorMsg.errors)
                {
                    FullMsgNiceStr = FullMsgNiceStr + "error code: " + E.code.ToString() + "\r\n";
                    FullMsgNiceStr = FullMsgNiceStr + "error message: " + E.message + "\r\n";
                }
                backgroundWorker2.ReportProgress((byte)1);

                return;
            }

            RigInfo RgInf = Newtonsoft.Json.JsonConvert.DeserializeObject<RigInfo>(RigInfoJSON);
            WatchRigInfo = RgInf;
            //       Tmp = richTextBox1.Text.Length;

            RigMsgNiceStr = RigMsgNiceStr + "Name: ";
            //       richTextBox1.AppendText("Name: ");
            StrTmp = RgInf.name + "\r\n";
            RigMsgNiceStr = RigMsgNiceStr + StrTmp;

            //richTextBox1.AppendText(StrTmp);
            //richTextBox1.Select(Tmp, richTextBox1.Text.Length);
            //richTextBox1.SelectionFont = new Font(richTextBox1.Font.FontFamily, this.Font.Size, FontStyle.Bold);

            StrTmp = "ID: " + RgInf.rigid + "\r\n";
            RigMsgNiceStr = RigMsgNiceStr + StrTmp;
            //    richTextBox1.AppendText(StrTmp);

            if (SettingDevices)
            {
                MiningDevices = 0;
                TotalDevices = 0;
            }
            RejectedSpeedPrev = RejectedSpeed;
            try
            {
                if (RgInf.stats != null)
                    RejectedSpeed = RgInf.stats[0].speedRejectedTotal;
                else
                    RejectedSpeed = 0;

            }
            catch
            {
                RejectedSpeed = 0;
            }

            if (RgInf.devices != null)
            {
                foreach (Dev D in RgInf.devices)
                {
                    StrTmp = "++++++++++++++++++++++++" + "\r\n";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                    //               richTextBox1.AppendText(StrTmp);

                    StrTmp = "Dev name: " + D.name + "\r\n";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                    //              richTextBox1.AppendText(StrTmp);

                    RigData[RigDataCnt].name = D.name;

                    StrTmp = "Status: " + D.status.enumName + "\r\n";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                    //               richTextBox1.AppendText(StrTmp);

                    if (SettingDevices)
                        TotalDevices++;
                    if (D.status.enumName == "MINING")
                    {
                        RigData[RigDataCnt].MiningStatus = D.status.enumName;
                        if (SettingDevices)
                        {
                            RigData[RigDataCnt].ToMonitor = true;
                            MiningDevices++;
                        }
                    }
                    else
                    {
                        if (SettingDevices)
                        {
                            RigData[RigDataCnt].ToMonitor = false;
                        }
                        RigData[RigDataCnt].MiningStatus = D.status.enumName;
                    }


                    StrTmp = "Load: " + D.load.ToString() + "\r\n";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                    //             richTextBox1.AppendText(StrTmp);

                    StrTmp = "Fan: " + D.revolutionsPerMinutePercentage.ToString() + "\r\n";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                    //            richTextBox1.AppendText(StrTmp);



                    //           Tmp = richTextBox1.Text.Length;

                    StrTmp = "Temperature: ";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;

                    //         richTextBox1.AppendText(StrTmp);
                    //          Tmp = richTextBox1.Text.Length;

                    StrTmp = D.temperature.ToString();
                    RigData[RigDataCnt].Temperature = D.temperature;

                    //if (D.temperature < 70)
                    //{
                    //    StrTmp = D.temperature.ToString() + "\r\n";
                    //    richTextBox1.AppendText(StrTmp);
                    //    richTextBox1.SelectionStart = Tmp;
                    //    richTextBox1.SelectionLength = StrTmp.Length;
                    //    richTextBox1.SelectionColor = Color.Green;
                    //}
                    //else
                    //    if (D.temperature <= 75)
                    //{
                    //    StrTmp = D.temperature.ToString() + "\r\n";
                    //    richTextBox1.AppendText(StrTmp);
                    //    richTextBox1.SelectionStart = Tmp;
                    //    richTextBox1.SelectionLength = StrTmp.Length;
                    //    richTextBox1.SelectionColor = Color.Orange;
                    //}
                    //else
                    //{
                    //    StrTmp = D.temperature.ToString() + "\r\n";
                    //    richTextBox1.AppendText(StrTmp);
                    //    richTextBox1.SelectionStart = Tmp;
                    //    richTextBox1.SelectionLength = StrTmp.Length;
                    //    richTextBox1.SelectionColor = Color.Red;
                    //}

                    RigMsgNiceStr = RigMsgNiceStr + StrTmp + "\r\n";
                    foreach (SpeedsRg S in D.speeds)
                    {
                        StrTmp = "Algorithm: " + S.algorithm + "\r\n";
                        RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                        //     richTextBox1.AppendText(StrTmp);

                        StrTmp = "Speed: " + S.speed.ToString() + "\r\n";
                        RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                        //      richTextBox1.AppendText(StrTmp);

                        RigData[RigDataCnt].Algorithm = S.algorithm;
                        RigData[RigDataCnt].hashrate = D.speeds[0].speed;
                    }

                    RigDataCnt++;


                }
            }
            if (SettingDevices)
            {
                StrTmp = "Mining devices: " + MiningDevices.ToString() + "\r\n" + "\r\n";
                RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                //  richTextBox1.AppendText(StrTmp);
            }

            //richTextBox1.AppendText("////////////////////////////////////////////////\r\n");
            RigMsgNiceStr = RigMsgNiceStr + "////////////////////////////////////////////////\r\n";

            if (!GettingWatchRigs)
                backgroundWorker2.ReportProgress((byte)2);
            else
            {
                backgroundWorker2.ReportProgress((byte)3);
                GettingWatchRigs = false;
            }

        }

        void GetWatchRigWorker_old(bool SettingDevices)
        {
            String StrTmp;
            int Tmp;
            ORG_ID = OrgID;
            API_KEY = Ky;
            API_SECRET = KySecr;
            int RigDataCnt = 0;
            Api api = new Api(URL_ROOT, ORG_ID, API_KEY, API_SECRET);
            string timeResponse = api.get("/api/v2/time");
            if (timeResponse == "")
            {
                FullMsgNiceStr = "No connection with server ( error 3 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();

                backgroundWorker2.ReportProgress((byte)1);

                return;
            }

            ServerTime serverTimeObject;
            try
            {
                serverTimeObject = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerTime>(timeResponse);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                FullMsgNiceStr = e.Message + " ( error 9 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();
                return;
            }
            string time = serverTimeObject.serverTime;

            var time1 = TimeSpan.FromMilliseconds(Convert.ToInt64(time));
            DateTime ServerDateTime = new DateTime(1970, 1, 1) + time1;

            RigMsgNiceStr = "Server time: " + ServerDateTime.ToString() + "\r\n";

            Tmp = richTextBox1.Text.Length;
            richTextBox1.AppendText(RigMsgNiceStr);
            richTextBox1.SelectionStart = Tmp;
            richTextBox1.SelectionLength = RigMsgNiceStr.Length;
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();

            string RigInfoJSON = api.get("/main/api/v2/mining/rig2/" + RigIDToWatch, true, time);
            if (RigInfoJSON == "")
            {
                FullMsgNiceStr = "No connection with server ( error 4 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();
                return;
            }
            ErrorMessage ErrorMsg;
            try
            {
                ErrorMsg = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorMessage>(RigInfoJSON);
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                FullMsgNiceStr = "Server error (error 6)" + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();

                return;
            }
            if (ErrorMsg.error_id != null)
            {
                FullMsgNiceStr = FullMsgNiceStr + "error_id: " + ErrorMsg.error_id + "\r\n";

                foreach (Err E in ErrorMsg.errors)
                {
                    FullMsgNiceStr = FullMsgNiceStr + "error code: " + E.code.ToString() + "\r\n";
                    FullMsgNiceStr = FullMsgNiceStr + "error message: " + E.message + "\r\n";
                }
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();

                return;
            }

            RigInfo RgInf = Newtonsoft.Json.JsonConvert.DeserializeObject<RigInfo>(RigInfoJSON);

            Tmp = richTextBox1.Text.Length;

            RigMsgNiceStr = RigMsgNiceStr + "Name: ";
            richTextBox1.AppendText("Name: ");
            StrTmp = RgInf.name + "\r\n";
            RigMsgNiceStr = RigMsgNiceStr + StrTmp;

            richTextBox1.AppendText(StrTmp);
            richTextBox1.Select(Tmp, richTextBox1.Text.Length);
            richTextBox1.SelectionFont = new Font(richTextBox1.Font.FontFamily, this.Font.Size, FontStyle.Bold);

            StrTmp = "ID: " + RgInf.rigid + "\r\n";
            RigMsgNiceStr = RigMsgNiceStr + StrTmp;
            richTextBox1.AppendText(StrTmp);

            if (SettingDevices)
            {
                MiningDevices = 0;
                TotalDevices = 0;
            }

            if (RgInf.devices != null)
            {
                foreach (Dev D in RgInf.devices)
                {
                    StrTmp = "++++++++++++++++++++++++" + "\r\n";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                    richTextBox1.AppendText(StrTmp);

                    StrTmp = "Dev name: " + D.name + "\r\n";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                    richTextBox1.AppendText(StrTmp);

                    RigData[RigDataCnt].name = D.name;

                    StrTmp = "Status: " + D.status.enumName + "\r\n";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                    richTextBox1.AppendText(StrTmp);

                    if (SettingDevices)
                        TotalDevices++;
                    if (D.status.enumName == "MINING")
                    {
                        RigData[RigDataCnt].MiningStatus = D.status.enumName;
                        if (SettingDevices)
                        {
                            RigData[RigDataCnt].ToMonitor = true;
                            MiningDevices++;
                        }
                    }
                    else
                    {
                        if (SettingDevices)
                        {
                            RigData[RigDataCnt].ToMonitor = false;
                        }
                        RigData[RigDataCnt].MiningStatus = D.status.enumName;
                    }


                    StrTmp = "Load: " + D.load.ToString() + "\r\n";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                    richTextBox1.AppendText(StrTmp);

                    StrTmp = "Fan: " + D.revolutionsPerMinutePercentage.ToString() + "\r\n";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                    richTextBox1.AppendText(StrTmp);



                    Tmp = richTextBox1.Text.Length;

                    StrTmp = "Temperature: ";
                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;

                    richTextBox1.AppendText(StrTmp);
                    Tmp = richTextBox1.Text.Length;

                    StrTmp = D.temperature.ToString();
                    RigData[RigDataCnt].Temperature = D.temperature;

                    if (D.temperature < 70)
                    {
                        StrTmp = D.temperature.ToString() + "\r\n";
                        richTextBox1.AppendText(StrTmp);
                        richTextBox1.SelectionStart = Tmp;
                        richTextBox1.SelectionLength = StrTmp.Length;
                        richTextBox1.SelectionColor = Color.Green;
                    }
                    else
                        if (D.temperature <= 75)
                    {
                        StrTmp = D.temperature.ToString() + "\r\n";
                        richTextBox1.AppendText(StrTmp);
                        richTextBox1.SelectionStart = Tmp;
                        richTextBox1.SelectionLength = StrTmp.Length;
                        richTextBox1.SelectionColor = Color.Orange;
                    }
                    else
                    {
                        StrTmp = D.temperature.ToString() + "\r\n";
                        richTextBox1.AppendText(StrTmp);
                        richTextBox1.SelectionStart = Tmp;
                        richTextBox1.SelectionLength = StrTmp.Length;
                        richTextBox1.SelectionColor = Color.Red;
                    }

                    RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                    foreach (SpeedsRg S in D.speeds)
                    {
                        StrTmp = "Algorithm: " + S.algorithm + "\r\n";
                        RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                        richTextBox1.AppendText(StrTmp);

                        StrTmp = "Speed: " + S.speed.ToString() + "\r\n";
                        RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                        richTextBox1.AppendText(StrTmp);

                        RigData[RigDataCnt].Algorithm = S.algorithm;
                        RigData[RigDataCnt].hashrate = D.speeds[0].speed;
                    }

                    RigDataCnt++;


                }
            }
            if (SettingDevices)
            {
                StrTmp = "Mining devices: " + MiningDevices.ToString() + "\r\n" + "\r\n";
                RigMsgNiceStr = RigMsgNiceStr + StrTmp;
                richTextBox1.AppendText(StrTmp);
            }

            richTextBox1.AppendText("////////////////////////////////////////////////\r\n");


            removeLines();
        }

        void RigDataToGrid()
        {
            RigDataToGridFl = true;
        }


        void RigDataToGridW()
        {
            bool NotMining;


            listView1.Items[0].SubItems[1].Text = RejectedSpeed.ToString();
            listView1.Items[0].SubItems[2].Text = RejectedSpeedPrev.ToString();

            for (int i = 0; i < TotalDevices; i++)
            {
                NotMining = false;
                //                dataGridView1.Rows[i * 7].Cells[1].Value = RigData[i].name;
                listView1.Items[i * 7 + 2].SubItems[1].Text = RigData[i].name;

                //              dataGridView1.Rows[i * 7 + 1].Cells[1].Value = RigData[i].MiningStatus;
                listView1.Items[i * 7 + 3].SubItems[1].Text = RigData[i].MiningStatus;

                if (RigData[i].ToMonitor)
                    if (MiningStatusMonitoring)
                    {
                        if ((RigData[i].MiningStatus != "MINING") && (RigData[i].MiningStatus != "BENCHMARKING"))
                        {
                            //dataGridView1.Rows[i * 7 + 1].Cells[3].Value = "Device not mining!";
                            //dataGridView1[3, i * 7 + 1].Style.ForeColor = Color.Red;
                            listView1.Items[i * 7 + 3].SubItems[3].Text = "Device not mining!";
                            listView1.Items[i * 7 + 3].SubItems[3].ForeColor = Color.Red;
                            NotMining = true;
                        }
                        else
                        {
                            //dataGridView1.Rows[i * 7 + 1].Cells[3].Value = "Ok";
                            //dataGridView1[3, i * 7 + 1].Style.ForeColor = Color.Green;

                            listView1.Items[i * 7 + 3].SubItems[3].Text = "Ok";
                            listView1.Items[i * 7 + 3].SubItems[3].ForeColor = Color.Green;
                        }
                    }
                    else
                    {
                        listView1.Items[i * 7 + 3].SubItems[3].Text = "Not monitoring";
                        listView1.Items[i * 7 + 3].SubItems[3].ForeColor = Color.Black;
                    }


                //                dataGridView1.Rows[i * 7 + 2].Cells[1].Value = RigData[i].Temperature;
                if (RigData[i].Temperature != -1)
                    listView1.Items[i * 7 + 4].SubItems[1].Text = RigData[i].Temperature.ToString();
                else
                    listView1.Items[i * 7 + 4].SubItems[1].Text = "-";


                if (RigData[i].ToMonitor)
                    if (TemperatureMonitoring)
                    {
                        if ((RigData[i].Temperature < TemperLo) && (RigData[i].MiningStatus != "BENCHMARKING"))
                        {
                            //dataGridView1.Rows[i * 7 + 2].Cells[3].Value = "Device temperature too low!";
                            //dataGridView1[3, i * 7 + 2].Style.ForeColor = Color.Red;
                            listView1.Items[i * 7 + 4].SubItems[3].Text = "Device temperature too low!";
                            listView1.Items[i * 7 + 4].SubItems[3].ForeColor = Color.Red;
                            NotMining = true;
                        }
                        else
                        {
                            //dataGridView1.Rows[i * 7 + 2].Cells[3].Value = "Ok";
                            //dataGridView1[3, i * 7 + 2].Style.ForeColor = Color.Green;

                            listView1.Items[i * 7 + 4].SubItems[3].Text = "Ok";
                            listView1.Items[i * 7 + 4].SubItems[3].ForeColor = Color.Green;
                        }
                    }
                    else
                    {
                        listView1.Items[i * 7 + 4].SubItems[3].Text = "Not monitoring";
                        listView1.Items[i * 7 + 4].SubItems[3].ForeColor = Color.Black;
                    }



                if (RigData[i].Temperature < 70)
                {
                    //dataGridView1[1, i * 7 + 2].Style.ForeColor = Color.Green;
                    listView1.Items[i * 7 + 4].SubItems[1].ForeColor = Color.Green;
                }
                else
                {
                    if (RigData[i].Temperature <= 75)
                    {
                        //  dataGridView1[1, i * 7 + 2].Style.ForeColor = Color.Orange;
                        listView1.Items[i * 7 + 4].SubItems[1].ForeColor = Color.Orange;
                    }
                    else
                    {
                        //dataGridView1[1, i * 7 + 2].Style.ForeColor = Color.Red;
                        listView1.Items[i * 7 + 4].SubItems[1].ForeColor = Color.Red;
                    }
                }


                //dataGridView1.Rows[i * 7 + 3].Cells[1].Value = RigData[i].Algorithm;
                //dataGridView1.Rows[i * 7 + 4].Cells[1].Value = RigData[i].hashrate;

                listView1.Items[i * 7 + 5].SubItems[1].Text = RigData[i].Algorithm;
                listView1.Items[i * 7 + 6].SubItems[1].Text = RigData[i].hashrate.ToString();


                if (RigData[i].ToMonitor)
                    if (HashrateMonitoring)
                    {
                        if (((RigData[i].hashrate == RigDataPrev[i].hashrate) || (RigData[i].hashrate == 0)) && (RigData[i].MiningStatus != "BENCHMARKING"))
                        {
                            //dataGridView1.Rows[i * 7 + 4].Cells[3].Value = "Hashrate is not changing!";
                            //dataGridView1[3, i * 7 + 4].Style.ForeColor = Color.Red;
                            listView1.Items[i * 7 + 6].SubItems[3].Text = "Hashrate is not changing!";
                            listView1.Items[i * 7 + 6].SubItems[3].ForeColor = Color.Red;
                            NotMining = true;
                        }
                        else
                        {
                            //dataGridView1.Rows[i * 7 + 4].Cells[3].Value = "Ok";
                            //dataGridView1[3, i * 7 + 4].Style.ForeColor = Color.Green;

                            listView1.Items[i * 7 + 6].SubItems[3].Text = "Ok";
                            listView1.Items[i * 7 + 6].SubItems[3].ForeColor = Color.Green;
                        }
                    }
                    else
                    {
                        listView1.Items[i * 7 + 6].SubItems[3].Text = "Not monitoring";
                        listView1.Items[i * 7 + 6].SubItems[3].ForeColor = Color.Black;
                    }



                //       dataGridView1.Rows[i * 7 + 5].Cells[3].Value = RigData[i].Counter;



                if (!RigData[i].ToMonitor)
                {
                    //                    dataGridView1.Rows[i * 7].Cells[3].Value = "No monitoring";
                    listView1.Items[i * 7 + 2].SubItems[3].Text = "No monitoring";
                }




                //              dataGridView1.Rows[i * 7 + 1].Cells[2].Value = RigDataPrev[i].MiningStatus;
                listView1.Items[i * 7 + 3].SubItems[2].Text = RigDataPrev[i].MiningStatus;

                //            dataGridView1.Rows[i * 7 + 2].Cells[2].Value = RigDataPrev[i].Temperature;
                if (RigDataPrev[i].Temperature != -1)
                    listView1.Items[i * 7 + 4].SubItems[2].Text = RigDataPrev[i].Temperature.ToString();
                else
                    listView1.Items[i * 7 + 4].SubItems[2].Text = "-";


                if (RigDataPrev[i].Temperature < 70)
                {
                    //              dataGridView1[2, i * 7 + 2].Style.ForeColor = Color.Green;
                    listView1.Items[i * 7 + 4].SubItems[2].ForeColor = Color.Green;
                }
                else
                {
                    if (RigDataPrev[i].Temperature <= 75)
                    {
                        //                dataGridView1[2, i * 7 + 2].Style.ForeColor = Color.Orange;
                        listView1.Items[i * 7 + 4].SubItems[2].ForeColor = Color.Orange;
                    }
                    else
                    {
                        //              dataGridView1[2, i * 7 + 2].Style.ForeColor = Color.Red;
                        listView1.Items[i * 7 + 4].SubItems[2].ForeColor = Color.Red;
                    }
                }

                //dataGridView1.Rows[i * 7 + 3].Cells[2].Value = RigDataPrev[i].Algorithm;
                //dataGridView1.Rows[i * 7 + 4].Cells[2].Value = RigDataPrev[i].hashrate;
                listView1.Items[i * 7 + 5].SubItems[2].Text = RigDataPrev[i].Algorithm;
                listView1.Items[i * 7 + 6].SubItems[2].Text = RigDataPrev[i].hashrate.ToString();

                if (NotMining)
                    RigData[i].CounterOn = true;
                else
                    RigData[i].CounterOn = false;
            }

            for (int i = 0; i < 100; i++)
            {
                RigDataPrev[i].name = RigData[i].name;
                RigDataPrev[i].MiningStatus = RigData[i].MiningStatus;
                RigDataPrev[i].Temperature = RigData[i].Temperature;
                RigDataPrev[i].Algorithm = RigData[i].Algorithm;
                RigDataPrev[i].hashrate = RigData[i].hashrate;
                RigDataPrev[i].ToMonitor = RigData[i].ToMonitor;
            }

            //  dataGridView1[i, j].Style.BackColor = Color.Gray;


        }
        void GetNiceInfo(int Task)
        {
            NiceInfoWorkFlag = true;
            Tsk = Task;
        }

        void GetNiceInfoWork(int Task)
        {
            ORG_ID = OrgID;
            API_KEY = Ky;
            API_SECRET = KySecr;
            string time;
            Api api = new Api(URL_ROOT, ORG_ID, API_KEY, API_SECRET);

            string timeResponse = api.get("/api/v2/time");
            TooHotToTelegramm = false;

            if (timeResponse == "")
            {
                FullMsgNiceStr = "No connection with server ( errr 1 )" + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                return;
            }

            ServerTime serverTimeObject;
            try
            {
                serverTimeObject = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerTime>(timeResponse);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                FullMsgNiceStr = e.Message + " ( error 8 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                return;
            }
            try
            {
                time = serverTimeObject.serverTime;
            }
            catch (System.NullReferenceException)
            {
                FullMsgNiceStr = "No connection with server ( error 1 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                return;
            }
            TemperatureList.Clear();

            var time1 = TimeSpan.FromMilliseconds(Convert.ToInt64(time));
            DateTime ServerDateTime = new DateTime(1970, 1, 1) + time1;
            FullMsgNiceStr = "Server time: " + ServerDateTime.ToString() + "\r\n";
            if (Task == 4)
            {
                TemperatureList.Add("Server time: " + ServerDateTime.ToString() + "\r\n");
            }


            //            string accountsResponse1 = api.get("/main/api/v2/mining/rig2/0--wQwYO82VlWVZA4sn67oGg", true, time);
            string AllRigsInfo = "";
            try
            {
                //                AllRigsInfo = api.get("/main/api/v2/mining/rigs2", true, time);
                AllRigsInfo = api.get("/main/api/v2/mining/rigs2", true, time);

            }


            catch (System.ArgumentNullException)
            {
                FullMsgNiceStr = "Keys error ( error 5 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);

                return;
            }


            /*
            api = new Api(URL_ROOT, ORG_ID, API_KEY, API_SECRET);

            timeResponse = api.get("/api/v2/time");
            TooHotToTelegramm = false;

            if (timeResponse == "")
            {
                FullMsgNiceStr = "No connection with server ( errr 1 )" + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                return;
            }

            
            try
            {
                serverTimeObject = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerTime>(timeResponse);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                FullMsgNiceStr = e.Message + " ( error 8 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                return;
            }
            try
            {
                time = serverTimeObject.serverTime;
            }
            catch (System.NullReferenceException)
            {
                FullMsgNiceStr = "No connection with server ( error 1 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                return;
            }

            */













            if (AllRigsInfo == "")
            {
                FullMsgNiceStr = "No connection with server ( error 2 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);

                return;
            }
            ErrorMessage ErrorMsg;
            try
            {
                ErrorMsg = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorMessage>(AllRigsInfo);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                FullMsgNiceStr = e.Message + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                return;
            }
            if (ErrorMsg.error_id != null)
            {
                FullMsgNiceStr = FullMsgNiceStr + "error_id: " + ErrorMsg.error_id + "\r\n";

                foreach (Err E in ErrorMsg.errors)
                {
                    FullMsgNiceStr = FullMsgNiceStr + "error code: " + E.code.ToString() + "\r\n";
                    FullMsgNiceStr = FullMsgNiceStr + "error message: " + E.message + "\r\n";
                }
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);

                return;
            }

            AllRigsInfo AllRgsInfo;
            try
            {
                AllRgsInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<AllRigsInfo>(AllRigsInfo);
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                FullMsgNiceStr = "Server error (error 7)" + "\r\n";
                backgroundWorker2.ReportProgress((byte)1);
                return;
            }



            RigList.Clear();

            FullMsgNiceStr = FullMsgNiceStr + "Mining: " + AllRgsInfo.minerStatuses.MINING.ToString() + "\r\n";
            if (Task == 4)
                TemperatureList.Add("Mining: " + AllRgsInfo.minerStatuses.MINING.ToString() + "\r\n");


            FullMsgNiceStr = FullMsgNiceStr + "Total rigs: " + AllRgsInfo.totalRigs.ToString() + "\r\n";
            if ((Task == 1) || (Task == 3))
            {
                TemperatureList.Add("Total rigs: " + AllRgsInfo.totalRigs.ToString() + "\r\n");
                TemperatureList.Add("Mining rigs: " + AllRgsInfo.minerStatuses.MINING.ToString() + "\r\n" + "\r\n");
            }
            FullMsgNiceStr = FullMsgNiceStr + "Total devices: " + AllRgsInfo.totalDevices.ToString() + "\r\n";
            if (Task == 4)
                TemperatureList.Add("Total devices: " + AllRgsInfo.totalDevices.ToString() + "\r\n");

            FullMsgNiceStr = FullMsgNiceStr + "Devices statuses: " + "\r\n";
            if (Task == 4)
                TemperatureList.Add("Devices statuses: " + "\r\n");

            FullMsgNiceStr = FullMsgNiceStr + "    disabled: " + AllRgsInfo.devicesStatuses.DISABLED.ToString() + "\r\n";
            if (Task == 4)
                TemperatureList.Add("    disabled: " + AllRgsInfo.devicesStatuses.DISABLED.ToString() + "\r\n");

            FullMsgNiceStr = FullMsgNiceStr + "    inactive: " + AllRgsInfo.devicesStatuses.INACTIVE.ToString() + "\r\n";
            if (Task == 4)
                TemperatureList.Add("    inactive: " + AllRgsInfo.devicesStatuses.INACTIVE.ToString() + "\r\n");

            FullMsgNiceStr = FullMsgNiceStr + "    mining: " + AllRgsInfo.devicesStatuses.MINING.ToString() + "\r\n";
            if (Task == 4)
                TemperatureList.Add("    mining: " + AllRgsInfo.devicesStatuses.MINING.ToString() + "\r\n");

            FullMsgNiceStr = FullMsgNiceStr + "    unpaid amount: " + AllRgsInfo.unpaidAmount.ToString() + " BTC" + "\r\n";
            if (Task == 4)
                TemperatureList.Add("    unpaid amount: " + AllRgsInfo.unpaidAmount.ToString() + " BTC" + "\r\n");


            FullMsgNiceStr = FullMsgNiceStr + "\r\n";
            FullMsgNiceStr = FullMsgNiceStr + "\r\n";

            FullMsgNiceStr = FullMsgNiceStr + "Rigs details: " + "\r\n";
            FullMsgNiceStr = FullMsgNiceStr + "\r\n";
            if (Task == 4)
                TemperatureList.Add("\r\n" + "\r\n" + "Rigs details: " + "\r\n" + "\r\n");

            RgGPUInf = new RigsGPUInfo[AllRgsInfo.totalRigs];



            if (AllRgsInfo.miningRigs != null)
            {
                int RigsCounter = 0;
                foreach (MiningRigs R in AllRgsInfo.miningRigs)
                {
                    RgGPUInf[RigsCounter] = new RigsGPUInfo(20);
                    RgGPUInf[RigsCounter].nameRig = R.name;
                    try
                    {
                        //FullMsgNiceStr = FullMsgNiceStr + "             Rig name: " + R.name.ToString() + ": " + R.minerStatus + "\r\n";
                        if (R.name != null)
                        {
                            FullMsgNiceStr = FullMsgNiceStr + "             Rig name: " + R.name.ToString(); ;
                            FullMsgNiceStr = FullMsgNiceStr + ": " + R.minerStatus + "\r\n";
                        }

                    }
                    catch (System.NullReferenceException)
                    {

                    }
                    if (Task == 4)
                        TemperatureList.Add("             Rig name: " + R.name.ToString() + "\r\n");
                    FullMsgNiceStr = FullMsgNiceStr + "   ID: " + R.rigid.ToString() + "\r\n";
                    if (Task == 4)
                        TemperatureList.Add("   ID: " + R.rigid.ToString() + "\r\n");
                    if (R.name != null)
                    {
                        RgList RgTmp;
                        RgTmp.name = R.name.ToString();
                        RgTmp.id = R.rigid.ToString();

                        RigList.Add(RgTmp);
                    }
                    if ((Task == 1) || (Task == 3))
                        TemperatureList.Add("     Rig name: " + R.name.ToString() + ": " + R.minerStatus + "\r\n");
                    int DeviceCnt = 1;
                    if (R.devices != null)
                    {
                        foreach (Devices D in R.devices)
                        {
                            RgGPUInf[RigsCounter].gpu[DeviceCnt - 1] = new GPUInfo(5);
                            RgGPUInf[RigsCounter].gpu[DeviceCnt - 1].name = D.name;
                            RgGPUInf[RigsCounter].gpu[DeviceCnt - 1].id = D.id;
                            FullMsgNiceStr = FullMsgNiceStr + "      device name: " + D.name.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "      id: " + D.id.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "      temperature: " + D.temperature.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "      load: " + D.load.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "      powerUsage: " + D.powerUsage.ToString() + "\r\n";
                            if (Task == 4)
                                TemperatureList.Add("      device name: " + D.name.ToString() + "\r\n" + "      id: " + D.id.ToString() + "\r\n" + "      temperature: " + D.temperature.ToString() + "\r\n"
                                     + "      powerUsage: " + D.powerUsage.ToString() + "\r\n");

                            if (D.temperature != -1)
                            {
                                if (Task == 1)
                                {
                                    if (D.temperature <= 70)
                                        TemperatureList.Add("   " + DeviceCnt.ToString() + ":  " + D.temperature.ToString() + 'C' + "\r\n");//°С
                                    else
                                        TemperatureList.Add("   " + DeviceCnt.ToString() + ":  " + D.temperature.ToString() + "C     !!!" + "\r\n");//°С
                                }
                                if (Task == 3)
                                {
                                    foreach (Speeds S in D.speeds)
                                    {
                                        TemperatureList.Add("   " + DeviceCnt.ToString() + ":  " + S.title + " - " + "\r\n");
                                        TemperatureList.Add("        " + S.speed + "\r\n");//°С
                                    }
                                }
                                int SpdCnt = 0;
                                foreach (Speeds S in D.speeds)
                                {
                                    RgGPUInf[RigsCounter].gpu[DeviceCnt - 1].sp[SpdCnt].alg = S.algorithm;
                                    RgGPUInf[RigsCounter].gpu[DeviceCnt - 1].sp[SpdCnt].spds = S.speed;
                                    SpdCnt++;

                                    for (int i = 0; i < HM.Length - 1; i++)
                                    {
                                        if ((D.id == HM[i].GPUid) && (S.algorithm == HM[i].algorithm))
                                        {
                                            if (S.speed > HM[i].hashrateThreshold)
                                                HM[i].hasrateNok = false;
                                            else
                                            {
                                                HM[i].hasrateNok = true;
                                                HM[i].lowHashrate = S.speed;
                                            }
                                        }
                                    }
                                }
                                DeviceCnt++;
                            }
                            bool HighTempPrev = false;
                            if (IDHighTemperatureList.Count > 0)
                            {
                                for (int i = 0; i < IDHighTemperatureList.Count; i++)
                                {
                                    if (IDHighTemperatureList[i] == D.id)
                                    {
                                        if (D.temperature <= TemperHiLevel)
                                        {
                                            IDHighTemperatureList.Remove(D.id);
                                        }
                                        i = IDHighTemperatureList.Count;
                                        HighTempPrev = true;
                                    }
                                }
                                if (!HighTempPrev)
                                {
                                    if (D.temperature > TemperHiLevel)
                                    {
                                        IDHighTemperatureList.Add(D.id);
                                        if (Task == 2)
                                        {
                                            TemperatureList.Add("\r\n");
                                            TemperatureList.Add("Rig name: " + R.name.ToString() + "\r\n");
                                            TemperatureList.Add("   The device is too hot!!!" + "\r\n");
                                            TemperatureList.Add("device name: " + D.name.ToString() + "\r\n");
                                            TemperatureList.Add("temperature: " + D.temperature.ToString() + "\r\n");
                                            TemperatureList.Add("load: " + D.load.ToString() + "\r\n");
                                            try
                                            {
                                                TemperatureList.Add("algorithm: " + D.speeds[0].algorithm + "\r\n");
                                                TemperatureList.Add("speed: " + D.speeds[0].speed + "\r\n");
                                            }
                                            catch (System.IndexOutOfRangeException e)
                                            {

                                            }

                                            TooHotToTelegramm = true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (D.temperature > TemperHiLevel)
                                {
                                    IDHighTemperatureList.Add(D.id);
                                    if (Task == 2)
                                    {
                                        TemperatureList.Add("\r\n");
                                        TemperatureList.Add("Rig name: " + R.name.ToString() + "\r\n");
                                        TemperatureList.Add("   The device is too hot!!!" + "\r\n");
                                        TemperatureList.Add("device name: " + D.name.ToString() + "\r\n");
                                        TemperatureList.Add("temperature: " + D.temperature.ToString() + "\r\n");
                                        TemperatureList.Add("load: " + D.load.ToString() + "\r\n");
                                        TemperatureList.Add("algorithm: " + D.speeds[0].algorithm + "\r\n");
                                        TemperatureList.Add("speed: " + D.speeds[0].speed + "\r\n");
                                        TooHotToTelegramm = true;
                                    }
                                }
                            }



                            foreach (Speeds S in D.speeds)
                            {
                                FullMsgNiceStr = FullMsgNiceStr + "         algorithm: " + S.algorithm.ToString() + "\r\n";
                                FullMsgNiceStr = FullMsgNiceStr + "         speed: " + S.speed.ToString() + "\r\n";
                                if (Task == 4)
                                    TemperatureList.Add("         algorithm: " + S.algorithm.ToString() + "\r\n" + "         speed: " + S.speed.ToString() + "\r\n");

                            }

                            FullMsgNiceStr = FullMsgNiceStr + "-------------------------------------------------" + "\r\n";
                            if (Task == 4)
                                TemperatureList.Add("-------------------------------------------------" + "\r\n");
                        }
                    }
                    RigsCounter++;

                    FullMsgNiceStr = FullMsgNiceStr + "\r\n";


                    FullMsgNiceStr = FullMsgNiceStr + "              STATs;" + "\r\n";
                    if (Task == 4)
                        TemperatureList.Add("\r\n" + "              STATs;" + "\r\n");


                    if (R.stats != null)
                    {
                        foreach (Stats S in R.stats)
                        {

                            FullMsgNiceStr = FullMsgNiceStr + "      algorithm: " + S.algorithm.enumName.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "      unpaidAmount: " + S.unpaidAmount.ToString() + "\r\n";
                            if (Task == 4)
                                TemperatureList.Add("      algorithm: " + S.algorithm.enumName.ToString() + "\r\n" + "      unpaidAmount: " + S.unpaidAmount.ToString() + "\r\n");
                            FullMsgNiceStr = FullMsgNiceStr + "      speedAccepted: " + S.speedAccepted.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                            if (Task == 4)
                                TemperatureList.Add("      speedAccepted: " + S.speedAccepted.ToString() + "\r\n" + "\r\n");


                        }
                    }

                    TemperatureList.Add("\r\n");


                    FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                    FullMsgNiceStr = FullMsgNiceStr + "******************************************" + "\r\n";
                    if (Task == 4)
                        TemperatureList.Add("\r\n" + "******************************************" + "\r\n");
                }
            }
            backgroundWorker2.ReportProgress((byte)4);
        }

        void GetNiceInfo_old(int Task)
        {
            ORG_ID = OrgID;
            API_KEY = Ky;
            API_SECRET = KySecr;
            string time;
            Api api = new Api(URL_ROOT, ORG_ID, API_KEY, API_SECRET);

            string timeResponse = api.get("/api/v2/time");
            TooHotToTelegramm = false;

            if (timeResponse == "")
            {
                FullMsgNiceStr = "No connection with server ( errr 1 )" + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();

                return;
            }

            ServerTime serverTimeObject;
            try
            {
                serverTimeObject = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerTime>(timeResponse);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                FullMsgNiceStr = e.Message + " ( error 8 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();
                return;
            }
            try
            {
                time = serverTimeObject.serverTime;
            }
            catch (System.NullReferenceException)
            {
                FullMsgNiceStr = "No connection with server ( error 1 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();
                return;
            }
            var time1 = TimeSpan.FromMilliseconds(Convert.ToInt64(time));
            DateTime ServerDateTime = new DateTime(1970, 1, 1) + time1;
            FullMsgNiceStr = "Server time: " + ServerDateTime.ToString() + "\r\n";



            //            string accountsResponse1 = api.get("/main/api/v2/mining/rig2/0--wQwYO82VlWVZA4sn67oGg", true, time);
            string AllRigsInfo = "";
            try
            {
                AllRigsInfo = api.get("/main/api/v2/mining/rigs2", true, time);
            }
            catch (System.ArgumentNullException)
            {
                FullMsgNiceStr = "Keys error ( error 5 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();

                return;
            }
            if (AllRigsInfo == "")
            {
                FullMsgNiceStr = "No connection with server ( error 2 )" + "\r\n";
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();

                return;
            }
            ErrorMessage ErrorMsg;
            try
            {
                ErrorMsg = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorMessage>(AllRigsInfo);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                richTextBox1.Text = richTextBox1.Text + e.Message;
                removeLines();
                return;
            }
            if (ErrorMsg.error_id != null)
            {
                FullMsgNiceStr = FullMsgNiceStr + "error_id: " + ErrorMsg.error_id + "\r\n";

                foreach (Err E in ErrorMsg.errors)
                {
                    FullMsgNiceStr = FullMsgNiceStr + "error code: " + E.code.ToString() + "\r\n";
                    FullMsgNiceStr = FullMsgNiceStr + "error message: " + E.message + "\r\n";
                }
                FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();

                return;
            }

            AllRigsInfo AllRgsInfo;
            try
            {
                AllRgsInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<AllRigsInfo>(AllRigsInfo);
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                FullMsgNiceStr = "Server error (error 7)" + "\r\n";
                richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
                removeLines();
                return;
            }



            RigList.Clear();

            FullMsgNiceStr = FullMsgNiceStr + "Mining: " + AllRgsInfo.minerStatuses.MINING.ToString() + "\r\n";

            FullMsgNiceStr = FullMsgNiceStr + "Total rigs: " + AllRgsInfo.totalRigs.ToString() + "\r\n";
            TemperatureList.Clear();
            if (Task == 1)
            {
                TemperatureList.Add("Total rigs: " + AllRgsInfo.totalRigs.ToString() + "\r\n");
                TemperatureList.Add("Mining rigs: " + AllRgsInfo.minerStatuses.MINING.ToString() + "\r\n" + "\r\n");
            }
            FullMsgNiceStr = FullMsgNiceStr + "Total devices: " + AllRgsInfo.totalDevices.ToString() + "\r\n";

            FullMsgNiceStr = FullMsgNiceStr + "Devices statuses: " + "\r\n";

            FullMsgNiceStr = FullMsgNiceStr + "    disabled: " + AllRgsInfo.devicesStatuses.DISABLED.ToString() + "\r\n";

            FullMsgNiceStr = FullMsgNiceStr + "    inactive: " + AllRgsInfo.devicesStatuses.INACTIVE.ToString() + "\r\n";

            FullMsgNiceStr = FullMsgNiceStr + "    mining: " + AllRgsInfo.devicesStatuses.MINING.ToString() + "\r\n";
            FullMsgNiceStr = FullMsgNiceStr + "    unpaid amount: " + AllRgsInfo.unpaidAmount.ToString() + " BTC" + "\r\n";


            FullMsgNiceStr = FullMsgNiceStr + "\r\n";
            FullMsgNiceStr = FullMsgNiceStr + "\r\n";

            FullMsgNiceStr = FullMsgNiceStr + "Rigs details: " + "\r\n";
            FullMsgNiceStr = FullMsgNiceStr + "\r\n";

            if (AllRgsInfo.miningRigs != null)
            {
                foreach (MiningRigs R in AllRgsInfo.miningRigs)
                {
                    FullMsgNiceStr = FullMsgNiceStr + "             Rig name: " + R.name.ToString() + "\r\n";
                    FullMsgNiceStr = FullMsgNiceStr + "   ID: " + R.rigid.ToString() + "\r\n";
                    RgList RgTmp;
                    RgTmp.name = R.name.ToString();
                    RgTmp.id = R.rigid.ToString();

                    RigList.Add(RgTmp);

                    if (Task == 1)
                        TemperatureList.Add("     Rig name: " + R.name.ToString() + "\r\n");
                    int DeviceCnt = 1;
                    if (R.devices != null)
                    {
                        foreach (Devices D in R.devices)
                        {

                            FullMsgNiceStr = FullMsgNiceStr + "      device name: " + D.name.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "      id: " + D.id.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "      temperature: " + D.temperature.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "      load: " + D.load.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "      powerUsage: " + D.powerUsage.ToString() + "\r\n";

                            if (D.temperature != -1)
                            {
                                if (Task == 1)
                                {
                                    //TemperatureList.Add(D.name.ToString() + ':' + "\r\n");
                                    if (D.temperature <= 70)
                                        TemperatureList.Add("   " + DeviceCnt.ToString() + ":  " + D.temperature.ToString() + 'C' + "\r\n");//°С
                                    else
                                        TemperatureList.Add("   " + DeviceCnt.ToString() + ":  " + D.temperature.ToString() + "C     !!!" + "\r\n");//°С
                                    DeviceCnt++;
                                }
                            }
                            bool HighTempPrev = false;
                            if (IDHighTemperatureList.Count > 0)
                            {
                                for (int i = 0; i < IDHighTemperatureList.Count; i++)
                                {
                                    if (IDHighTemperatureList[i] == D.id)
                                    {
                                        if (D.temperature <= TemperHiLevel)
                                        {
                                            IDHighTemperatureList.Remove(D.id);
                                        }
                                        i = IDHighTemperatureList.Count;
                                        HighTempPrev = true;
                                    }
                                }
                                if (!HighTempPrev)
                                {
                                    if (D.temperature > TemperHiLevel)
                                    {
                                        IDHighTemperatureList.Add(D.id);
                                        if (Task == 2)
                                        {
                                            TemperatureList.Add("\r\n");
                                            TemperatureList.Add("Rig name: " + R.name.ToString() + "\r\n");
                                            TemperatureList.Add("   The device is too hot!!!" + "\r\n");
                                            TemperatureList.Add("device name: " + D.name.ToString() + "\r\n");
                                            TemperatureList.Add("temperature: " + D.temperature.ToString() + "\r\n");
                                            TemperatureList.Add("load: " + D.load.ToString() + "\r\n");
                                            try
                                            {
                                                TemperatureList.Add("algorithm: " + D.speeds[0].algorithm + "\r\n");
                                                TemperatureList.Add("speed: " + D.speeds[0].speed + "\r\n");
                                            }
                                            catch (System.IndexOutOfRangeException e)
                                            {

                                            }

                                            TooHotToTelegramm = true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (D.temperature > TemperHiLevel)
                                {
                                    IDHighTemperatureList.Add(D.id);
                                    if (Task == 2)
                                    {
                                        TemperatureList.Add("\r\n");
                                        TemperatureList.Add("Rig name: " + R.name.ToString() + "\r\n");
                                        TemperatureList.Add("   The device is too hot!!!" + "\r\n");
                                        TemperatureList.Add("device name: " + D.name.ToString() + "\r\n");
                                        TemperatureList.Add("temperature: " + D.temperature.ToString() + "\r\n");
                                        TemperatureList.Add("load: " + D.load.ToString() + "\r\n");
                                        TemperatureList.Add("algorithm: " + D.speeds[0].algorithm + "\r\n");
                                        TemperatureList.Add("speed: " + D.speeds[0].speed + "\r\n");
                                        TooHotToTelegramm = true;
                                    }
                                }
                            }



                            foreach (Speeds S in D.speeds)
                            {
                                FullMsgNiceStr = FullMsgNiceStr + "         algorithm: " + S.algorithm.ToString() + "\r\n";
                                FullMsgNiceStr = FullMsgNiceStr + "         speed: " + S.speed.ToString() + "\r\n";

                            }

                            FullMsgNiceStr = FullMsgNiceStr + "-------------------------------------------------" + "\r\n";

                        }
                    }

                    FullMsgNiceStr = FullMsgNiceStr + "\r\n";


                    FullMsgNiceStr = FullMsgNiceStr + "              STATs;" + "\r\n";


                    if (R.stats != null)
                    {
                        foreach (Stats S in R.stats)
                        {

                            FullMsgNiceStr = FullMsgNiceStr + "      algorithm: " + S.algorithm.enumName.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "      unpaidAmount: " + S.unpaidAmount.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "      speedAccepted: " + S.speedAccepted.ToString() + "\r\n";
                            FullMsgNiceStr = FullMsgNiceStr + "\r\n";

                        }
                    }

                    TemperatureList.Add("\r\n");


                    FullMsgNiceStr = FullMsgNiceStr + "\r\n";
                    FullMsgNiceStr = FullMsgNiceStr + "******************************************" + "\r\n";
                }
            }

            comboBox1.Items.Clear();

            for (int i = 0; i < RigList.Count; i++)
            {
                RgList RgL = RigList[i];
                comboBox1.Items.Add(RgL.name);
                comboBox1.SelectedIndex = 0;
            }


            richTextBox1.Text = richTextBox1.Text + FullMsgNiceStr;
            removeLines();
            TemperatureList.Add("\r\n");
            for (int j = 0; j < TemperatureList.Count; j++)
                richTextBox1.Text = richTextBox1.Text + TemperatureList[j];
            removeLines();


        }
        void removeLines()
        {
            int k = richTextBox1.Lines.Count();
            if (k > MaxLines)
            {

                k = richTextBox1.GetFirstCharIndexFromLine(k - MaxLines);
                richTextBox1.Text = richTextBox1.Text.Remove(0, k);
                k = richTextBox1.Lines.Count();
            }
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();

        }

        void SendListToTelegramm()
        {
            int t;
            String LogStr = TemperatureList.Count.ToString() + "\r\n";


            if (!SendMsgList)
                return;

            if ((TemperatureList.Count == 0) && (DelayCnt == 0))
            {
                SendMsgList = false;
                MessageCnt = 1;
                return;
            }

            if (DelayCnt > 0)
            {
                DelayCnt--;
                if (DelayCnt > 0)
                    return;
            }

            richTextBox1.Text = richTextBox1.Text + LogStr;
            try
            {
                if (TemperatureList.Count > 0)
                {
                    while (TemperatureList[0] == "\r\n")
                        TemperatureList.RemoveAt(0);
                }
            }
            catch
            { }

            StrToTelegramm = RigNameToWatch + ": message " + MessageCnt.ToString() + "\r\n\r\n";


            while (((StrToTelegramm.Length + TemperatureList[0].Length) < 1400) && (TemperatureList.Count > 0))
            {
                StrToTelegramm = StrToTelegramm + TemperatureList[0];
                TemperatureList.RemoveAt(0);
                if (TemperatureList.Count == 0)
                    break;
            }

            richTextBox1.Text = richTextBox1.Text + StrToTelegramm +/* "\r\n\r\n" + */"Lenght: " + StrToTelegramm.Length.ToString() + "\r\n";


            ReadyToRecMsg = 0;
            MessageCnt++;
            DelayCnt = 0;
            SendMsgToTelegramm();
        }




        void SendStartingOffFunc()
        {
            CntTrmArray = 4;
            TrmArray[CntTrmArray++] = (byte)UART1_CMD.STARTING_OFF;
            CntTrmArray -= 2;
            TrmArray[2] = (byte)CntTrmArray;//N
            TrmArray[3] = (byte)(CntTrmArray >> 8);
            TrmArray[CntTrmArray + 2] = CalcCheckSumm(TrmArray, TrmArray[2] + (((int)TrmArray[3]) << 8), 2);
            Trm();
        }


        void DefaultAll()
        {
            TotalDevices = 0;
            MiningDevices = 0;
            RigNameToWatch = "No rig name";
            RigIDToWatch = "";
            MonitorAllRigs = false;
            MaxLines = 1000;
            TemperLo = 37;
            TemperHiLevel = 77;
            RestartDelay = 300;
            MiningStatusMonitoring = false;
            TemperatureMonitoring = false;
            HashrateMonitoring = false;
            GPUsMonitoring = false;
            InternetMonitoring = false;
            Monitoring = false;
            button9.Text = "Start monitoring";
            button9.ForeColor = Color.Red;




            Properties.Settings.Default.RigName_Options = RigNameToWatch;
            Properties.Settings.Default.RigID_Options = RigIDToWatch;
            Properties.Settings.Default.MiningDevices = MiningDevices;
            Properties.Settings.Default.TotalDevices = TotalDevices;
            Properties.Settings.Default.MaxLines = MaxLines;
            Properties.Settings.Default.Monitoring = Monitoring;
            Properties.Settings.Default.CntMonitoring = CntMonitoring;
            Properties.Settings.Default.TemperLo = TemperLo;
            Properties.Settings.Default.RestartDelay = RestartDelay;
            Properties.Settings.Default.TemperHiLevel = TemperHiLevel;
            Properties.Settings.Default.MonitorAllRigs = MonitorAllRigs;
            Properties.Settings.Default.MiningStatusMonitoring = MiningStatusMonitoring;
            Properties.Settings.Default.TemperatureMonitoring = TemperatureMonitoring;
            Properties.Settings.Default.HashrateMonitoring = HashrateMonitoring;
            Properties.Settings.Default.GPUsMonitoring = GPUsMonitoring;
            Properties.Settings.Default.InternetMonitoring = InternetMonitoring;

            Properties.Settings.Default.Save();




            checkBox3.Checked = false;
            checkBox4.Checked = false;
            checkBox5.Checked = false;
            checkBox6.Checked = false;
            checkBox7.Checked = false;


            checkBox2.Checked = false;
            textBox7.Text = MaxLines.ToString();
            textBox8.Text = TemperLo.ToString();
            textBox9.Text = RestartDelay.ToString();
            textBox11.Text = TemperHiLevel.ToString();

            label9.Text = MiningDevices.ToString();
            label8.Text = RigIDToWatch;
            label11.Text = RigNameToWatch;
            label13.Text = TotalDevices.ToString();

            Properties.Settings.Default.MiningDevices = MiningDevices;
            Properties.Settings.Default.TotalDevices = TotalDevices;

            Properties.Settings.Default.Save();
        }


        void FillCombo3GPUs()
        {
            while (comboBox3.Items.Count > 0)
                comboBox3.Items.RemoveAt(0);


            if (RgGPUInf != null)
                for (int i = 0; i < RgGPUInf.Length; i++)
                {
                    if (RgGPUInf[i].nameRig != null)
                        comboBox3.Items.Add(RgGPUInf[i].nameRig);
                }
            if (comboBox3.Items.Count > 0)
                comboBox3.SelectedIndex = 0;
            FillCombo4GPUs(0);
            FillCombo5GPUs(0, 0);

        }

        void FillCombo4GPUs(int i)
        {
            while (comboBox4.Items.Count > 0)
                comboBox4.Items.RemoveAt(0);

            if (RgGPUInf[i].gpu != null)
                for (int j = 0; j < RgGPUInf[i].gpu.Length; j++)
                {
                    if (RgGPUInf[i].gpu[j].name != null)
                    {
                        comboBox4.Items.Add(RgGPUInf[i].gpu[j].name);
                    }
                }
            if (comboBox4.Items.Count > 0)
                comboBox4.SelectedIndex = 0;
        }

        void FillCombo5GPUs(int i, int j)
        {
            while (comboBox5.Items.Count > 0)
                comboBox5.Items.RemoveAt(0);

            comboBox5.Items.Clear();
            if (RgGPUInf[i].gpu[j].sp[0].alg != null)
            {
                for (int k = 0; k < RgGPUInf[i].gpu[j].sp.Length; k++)
                {
                    if (RgGPUInf[i].gpu[j].sp[k].alg != null)
                    {
                        comboBox5.Items.Add(RgGPUInf[i].gpu[j].sp[k].alg);
                    }
                }
            }
            else
            {
                comboBox5.Items.Add("No agorithm");
            }
            if (comboBox5.Items.Count > 0)
                comboBox5.SelectedIndex = 0;
        }

        void FillLowThresholdcomboBox()
        {
            comboBox6.Items.Clear();
            for (int i = 0; i < HM.Length; i++)
            {
                if ((HM[i].nameGPU != "") && (HM[i].nameGPU != null))
                    comboBox6.Items.Add(HM[i].nameGPU);
            }
            if (comboBox6.Items.Count > 0)
                comboBox6.SelectedIndex = 0;
        }

        void SendToTelegramm(String Str)
        {
            TemperatureList.Add(Str);
            StrToTelegramm = "";
            SendMsgList = true;
            MessageCnt = 1;
            DelayCnt = 3;
        }
        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204"))
                    return false;
            }
            catch
            {
                return true;
            }
        }

    }


    public class ServerTime
    {
        public string serverTime { get; set; }
    }
    //---------------------------------------------------------
    public class MinerStatuses
    {
        public int MINING { get; set; }
    }
    public class DeviceStatuses
    {
        public int MINING { get; set; }
        public int DISABLED { get; set; }
        public int INACTIVE { get; set; }
    }

    public class MiningRigs
    {
        public String rigid { get; set; }
        public String minerStatus { get; set; }
        public String name { get; set; }
        public Devices[] devices { get; set; }
        public Stats[] stats { get; set; }
        public float unpaidAmount { get; set; }
    }

    public class Devices
    {
        public String id { get; set; }
        public float temperature { get; set; }
        public float load { get; set; }
        public float powerUsage { get; set; }
        public String name { get; set; }
        public Speeds[] speeds { get; set; }

    }

    public class Speeds
    {
        public String algorithm { get; set; }
        public float speed { get; set; }
        public String title { get; set; }

    }

    public class AllRigsInfo
    {
        public MinerStatuses minerStatuses { get; set; }
        public int totalRigs { get; set; }
        public int totalDevices { get; set; }
        public DeviceStatuses devicesStatuses { get; set; }
        public MiningRigs[] miningRigs { get; set; }
        public float unpaidAmount { get; set; }
    }

    public class AlgorithmStat
    {
        public String enumName { get; set; }
    }
    public class Stats
    {
        public AlgorithmStat algorithm { get; set; }
        public float unpaidAmount { get; set; }
        public float speedAccepted { get; set; }
    }

    //---------------------------------------------------------

    public class Status
    {
        public String enumName { get; set; }
    }
    public class SpeedsRg
    {
        public String algorithm { get; set; }
        public double speed { get; set; }
    }

    public class Dev
    {
        public String name { get; set; }
        public float temperature { get; set; }
        public float revolutionsPerMinutePercentage { get; set; }
        public SpeedsRg[] speeds { get; set; }
        public String load { get; set; }
        public Status status { get; set; }
        public bool monitoring { get; set; }
    }

    public class Statss
    {
        public float speedRejectedTotal { get; set; }
    }
    public class RigInfo
    {
        public String rigid { get; set; }
        public String name { get; set; }
        public Dev[] devices { get; set; }
        public Statss[] stats { get; set; }

    }
    //-------------------------------------------error message------------------------------------
    public class Err
    {
        public int code { get; set; }
        public string message { get; set; }
    }


    public class ErrorMessage
    {
        public string error_id { get; set; }
        public Err[] errors { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }

    //-------------------------------------------Telegram------------------------------------

    public class Chat
    {
        public String id { get; set; }
        public String title { get; set; }
    }

    public class My_chat_member
    {
        public Chat chat { get; set; }
    }

    public class Channel_post
    {
        public Chat chat { get; set; }
    }

    public class Result
    {
        public My_chat_member my_chat_member { get; set; }
        public Channel_post channel_post { get; set; }
    }

    public class TelegramResponse
    {
        public Result[] result { get; set; }
    }

    //-----------------------------------------------------------------------------------------

    struct Spd
    {
        public String alg;
        public float spds;
    }


    struct GPUInfo
    {
        public Spd[] sp { get; set; }
        public int algoCnt { get; set; }
        public String algorithmToMonitor { get; set; }
        public String name { get; set; }
        public String id { get; set; }
        public bool hashrateNOk { get; set; }
        public bool monitoringhashrate { get; set; }
        public GPUInfo(int i)
        {
            sp = new Spd[i];
            algoCnt = 0;
            algorithmToMonitor = "";
            hashrateNOk = false;
            monitoringhashrate = false;
            name = "";
            id = "";
        }
    }


    struct RigsGPUInfo
    {
        public String nameRig { get; set; }
        public int GPUCnt { get; set; }
        public GPUInfo[] gpu { get; set; }
        public RigsGPUInfo(int i)
        {
            gpu = new GPUInfo[i];
            nameRig = "";
            GPUCnt = 0;
        }
    }
    struct HashratesMonitoring
    {
        public float hashrateThreshold { get; set; }
        public float lowHashrate { get; set; }
        public String algorithm { get; set; }
        public String nameGPU { get; set; }
        public String Rigname { get; set; }
        public String GPUid { get; set; }
        public bool isLow { get; set; }
        public bool hasrateNok { get; set; }
        public int Cnt { get; set; }
        public HashratesMonitoring(bool low)
        {
            isLow = low;
            hashrateThreshold = 0;
            nameGPU = "";
            GPUid = "";
            algorithm = "";
            Rigname = "";
            hasrateNok = false;
            Cnt = 0;
            lowHashrate = 0;
        }
    }

    struct ProcessesMonitoring
    {
        public String nameProcess { get; set; }
        public int Cnt { get; set; }
        public bool Responding { get; set; }
        public ProcessesMonitoring(int cn)
        {
            Responding = true;
            nameProcess = "";
            Cnt = cn;
        }
    }

}












