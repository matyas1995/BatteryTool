using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Xml;

namespace AkkuMonitoring_v2._0
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", EntryPoint = "MessageBox")]
        public extern static int MessageBoxAPI(uint HWnd, string text, string caption, uint type);

        private const uint MB_SYSTEMMODAL = 0x1000;
        private const uint MB_APPLMODAL = 0x0;
        private const uint MB_TASKMODAL = 0x2000;
        private const uint MB_ICONASTERISK = 0x40;
        private const uint MB_ICONEXCLAMATION = 0x30;
        private const uint MB_ICONHAND = 0x10;
        private const uint MB_ICONMASK = 0xF0;
        private const uint MB_ICONQUESTION = 0x20;

        public static float RemainingPercent = 0;
        public TimeSpan RemainingTime;
        public static int Akku = 60;
        public static string ChargeStatus = "";
        bool WarningAlreadyGiven = false;
        DateTime DateOfStart = DateTime.Now;
        DateTime DateOfEnd;
        long Runtime = 0;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadXmlData();
            label1.Text = "";
            BackgroundWorker Worker = new BackgroundWorker();
            Worker.WorkerReportsProgress = true;
            Worker.WorkerSupportsCancellation = true;
            Worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            Worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_RunWorkerCompleted);
            if (Worker.IsBusy != true)
            {
                Worker.RunWorkerAsync();
            }
            Form2 form = new Form2();
            form.Show();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            while (true)
            {
                if (bw.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    try
                    {
                        RemainingPercent = Convert.ToInt32(System.Windows.Forms.SystemInformation.PowerStatus.BatteryLifePercent * 100);
                        ChargeStatus = SystemInformation.PowerStatus.BatteryChargeStatus.ToString();
                        RemainingTime = TimeSpan.FromSeconds(SystemInformation.PowerStatus.BatteryLifeRemaining);
                        if (RemainingPercent < Akku)
                        {
                            if (ChargeStatus.IndexOf("Charging") == -1 && WarningAlreadyGiven == false)
                            {
                                DateOfEnd = DateTime.Now;
                                //TimeSpan Laufzeit = DateOfStart - DateOfEnd;
                                //MessageBox.Show("Der Akkustand Beträgt " + Convert.ToString(RemainingPercent) + "\nLaufzeit: " + Convert.ToString(Laufzeit), "Warning!");
                                TimeSpan Laufzeit = TimeSpan.FromMilliseconds(Runtime);
                                MessageBoxAPI(0, "Der Akkustand Beträgt " + Convert.ToString(RemainingPercent) + "\nLaufzeit: " + Convert.ToString(Laufzeit), "Warning", MB_SYSTEMMODAL | MB_ICONEXCLAMATION);
                                WarningAlreadyGiven = true;
                            }
                        }
                        else
                        {
                            if (RemainingPercent == 100)
                            {
                                if (ChargeStatus.IndexOf("NoSystemBattery") != -1 && WarningAlreadyGiven == false)
                                {
                                    //do nothing
                                    WarningAlreadyGiven = true;
                                    Runtime = 0;
                                }
                                else
                                {
                                    if (ChargeStatus.IndexOf("Charging") == -1 && WarningAlreadyGiven == false)
                                    {
                                        //MessageBox.Show("Akku aufgeladen!");
                                        MessageBoxAPI(0, "Akku aufgeladen!", "Warning", MB_SYSTEMMODAL | MB_ICONEXCLAMATION);
                                        //DateOfStart = DateTime.Now;
                                        Runtime = 0;
                                        WarningAlreadyGiven = true;
                                    }
                                }
                            }
                            else
                            {
                                WarningAlreadyGiven = false;
                            }
                        }
                        bw.ReportProgress(Convert.ToInt32(RemainingPercent));
                        Runtime += 5000;
                        Thread.Sleep(5000);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "ERROR");
                    }
                }
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = Convert.ToInt32(e.ProgressPercentage);
            if (RemainingTime.Milliseconds != -1)
            {
                label1.Text = Convert.ToString(e.ProgressPercentage) + "%\n" + ChargeStatus + "\n" + RemainingTime + " left\nSystem Uptime: " + TimeSpan.FromMilliseconds(Runtime);
            }
            else
            {
                label1.Text = Convert.ToString(e.ProgressPercentage) + "%\n" + ChargeStatus + "\nCharging\nSystem Uptime: " + TimeSpan.FromMilliseconds(Runtime);
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Cancelled == true))
            {
                label1.Text = "Cancelled!";
            }

            else if (!(e.Error == null))
            {
                label1.Text = ("Error: " + e.Error.Message);
            }

            else
            {
                label1.Text = "Done!";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Akku = Convert.ToInt32(numericUpDown1.Value);
            //write value to xml to remember
            XmlDocument doc = new XmlDocument();
            doc.Load("./Settings.xml");
            XmlNode root = doc.DocumentElement;
            XmlNode nodeBattery = root.SelectSingleNode("/BatteryLeft");
            nodeBattery.InnerText = Akku.ToString();
            doc.Save("./Settings.xml");
            WarningAlreadyGiven = false;
        }

        private void LoadXmlData()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("./Settings.xml");
            XmlNode root = doc.DocumentElement;
            XmlNode nodeBattery = root.SelectSingleNode("/BatteryLeft");
            Akku = Convert.ToInt32(nodeBattery.InnerText);
            numericUpDown1.Value = Akku;
        }
    }
}
