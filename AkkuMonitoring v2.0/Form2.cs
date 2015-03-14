using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Xml;

namespace AkkuMonitoring_v2._0
{
    public partial class Form2 : Form
    {
        public int[] BatteryState = new int[360];
        public int[] ProcessorState = new int[360];
        public int RemainingPercent = 0;
        public int OverallAverageCPU = 0; //Average overall CPU usage (saved in XML)
        public int OverallAverageCPUCounter = 0;
        int CPUReadSum = 0;      //Average CPU Usage
        int CPUReadCount = 0;
        BackgroundWorker Worker = new BackgroundWorker();
        static PerformanceCounter cpuCounter;

        public Form2()
        {
            InitializeComponent();
            InitialisierePerformanceCounter();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            LoadAverageCPU();
            Worker.WorkerReportsProgress = true;
            Worker.WorkerSupportsCancellation = true;
            Worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            Worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_RunWorkerCompleted);
            if (Worker.IsBusy != true)
            {
                Worker.RunWorkerAsync();
            }
        }

        private void Form2_FormClosing(Object sender, FormClosingEventArgs e)
        {
            MessageBox.Show(OverallAverageCPU.ToString() + " " + OverallAverageCPUCounter.ToString());
            XmlDocument doc = new XmlDocument();
            doc.Load("./Settings.xml");
            XmlNode root = doc.DocumentElement;
            XmlNode nodeBattery = root.SelectSingleNode("/Battery");
            XmlNodeList InnerNodes = nodeBattery.ChildNodes;
            nodeBattery = InnerNodes.Item(1);
            nodeBattery.InnerText = OverallAverageCPU.ToString();
            XmlNode Counter = InnerNodes.Item(2);
            Counter.InnerText = OverallAverageCPUCounter.ToString();

            doc.Save("./Settings.xml");
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
                        bw.ReportProgress(RemainingPercent);
                        Thread.Sleep(10000);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Backgroundworker");
                    }
                }
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                int[] MemoryBatteryState = new int[360];
                int[] MemoryProcessorState = new int[360];
                for (int i = 0; i < 360; ++i)
                {
                    MemoryBatteryState[i] = BatteryState[i];
                    MemoryProcessorState[i] = ProcessorState[i];
                }
                for (int i = 0; i < 359; ++i)
                {
                    BatteryState[i + 1] = MemoryBatteryState[i];
                    ProcessorState[i + 1] = MemoryProcessorState[i];
                }
                BatteryState[0] = 100 - e.ProgressPercentage;
                ProcessorState[0] = 100 - Convert.ToInt32(cpuCounter.NextValue());
                Thread.Sleep(1000);
                DrawHistory();

            }
            catch (Exception ex)
            {
                Worker.CancelAsync();
                //MessageBox.Show(ex.Message, "Worker_ProgressChanged");
            }
        }

        private void DrawHistory()
        {
            try
            {
                int x = 0;
                int y = 0;
                pictureBox1.Refresh();
                Point[] points = new Point[360];
                Point point;
                Point[] cpuPoints = new Point[360];
                Point cpuPoint;
                for (int i = 0; i < 360; ++i)
                {
                    x = BatteryState[359 - i];
                    point = new Point(i, x);
                    points[i] = point;
                    y = ProcessorState[359 - i];
                    cpuPoint = new Point(i, y);
                    cpuPoints[i] = cpuPoint;
                }
                Graphics e = pictureBox1.CreateGraphics();
                e.DrawLines(new Pen(Brushes.Black), cpuPoints);
                if (x <= 25)
                {
                    e.DrawLines(new Pen(Brushes.Green), points);
                }
                else if (x <= 40)
                {
                    e.DrawLines(new Pen(Brushes.Blue), points);
                }
                else if (x <= 50)
                {
                    e.DrawLines(new Pen(Brushes.Orange), points);
                }
                else
                {
                    e.DrawLines(new Pen(Brushes.Red), points);
                }
                double[] averages = new double[2];
                averages = AverageCPU();
                label1.Text = "Average CPU usage (current session): " + averages[1].ToString() + "%\nAverage CPU usage (overall): " + averages[0].ToString() + "%";
            }
            catch (Exception ex)
            {
                throw ex;
                //MessageBox.Show(ex.Message, "DrawHistory");
            }
        }

        private void LoadAverageCPU()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("./Settings.xml");
            XmlNode root = doc.DocumentElement;
            XmlNode nodeBattery = root.SelectSingleNode("/Battery");
            XmlNodeList InnerNodes = nodeBattery.ChildNodes;
            nodeBattery = InnerNodes.Item(2);
            OverallAverageCPUCounter = Convert.ToInt32(nodeBattery.InnerText);

            nodeBattery = InnerNodes.Item(1);

            OverallAverageCPU = Convert.ToInt32(nodeBattery.InnerText);
        }
        
        private double[] AverageCPU()
        {
            int currentCPU = Convert.ToInt32(cpuCounter.NextValue());
            OverallAverageCPU += currentCPU;
            CPUReadSum += currentCPU;
            CPUReadCount += 1;
            OverallAverageCPUCounter += 1;

            double[] Averages = new double[2];
            Averages[0] = OverallAverageCPU / OverallAverageCPUCounter;
            Averages[1] = CPUReadSum / CPUReadCount;

            return Averages;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }

        public void InitialisierePerformanceCounter() // Initialisieren
        {
            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total"; // "_Total" entspricht der gesamten CPU Auslastung, Bei Computern mit mehr als 1 logischem Prozessor: "0" dem ersten Core, "1" dem zweiten...
            //cpuCounter.BeginInit();
        }
    }
}
