using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor;
using System.IO.Ports;
using System.Runtime;

namespace SensorsMonitor
{
    public partial class MainForm : Form
    {
        SerialPort port = new SerialPort();
        Computer computer = new Computer()
        {
            CPUEnabled = true,
            GPUEnabled = true,
            FanControllerEnabled = true,
            RAMEnabled = true,
            MainboardEnabled = true
        };
        Sensors sensors = new Sensors();
        bool exit = false;

        public MainForm()
        {
            InitializeComponent();
            Init();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            computer.Open();
        }


        private void Init()
        {
            port.Parity = Parity.None;
            port.StopBits = StopBits.One;
            port.DataBits = 8;
            port.Handshake = Handshake.None;
            port.RtsEnable = true;
            port.BaudRate = 9600;

            try
            {
                string[] ports = SerialPort.GetPortNames();

                var i = 0;
                var activeIndex = 0;

                foreach (var p in ports)
                {
                    cmbPorts.Items.Add(p);

                    if (p != "COM1")
                    {
                        activeIndex = i;
                        port.PortName = p;
                        port.Open();
                        syncTimer.Start();
                        btnConnect.Text = "Disconnect";
                    }

                    i++;
                }

                cmbPorts.SelectedIndex = activeIndex;
            } catch (Exception e)
            {
                lblStatus.Text = e.Message;
            }
        }

        private void Sync()
        {
            int coresCount = 0;
            float totalFrequency = 0;

            foreach (var hardware in computer.Hardware)
            {
                System.Diagnostics.Debug.WriteLine(hardware.HardwareType);

                if (hardware.HardwareType == HardwareType.CPU)
                {
                    hardware.Update();

                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            sensors.cpuTemp = sensor.Value.GetValueOrDefault(0);
                        }

                        if (sensor.SensorType == SensorType.Power && sensor.Name == "CPU Package")
                        {
                            sensors.cpuPower = sensor.Value.GetValueOrDefault(0);
                        }

                        if (sensor.SensorType == SensorType.Load)
                        {
                            sensors.cpuLoad = sensor.Value.GetValueOrDefault(0);
                        }

                        if (sensor.SensorType == SensorType.Clock && sensor.Name != "Bus Speed")
                        {
                            coresCount++;
                            totalFrequency += sensor.Value.GetValueOrDefault(0);
                        }
                    }
                }


                if (hardware.HardwareType == HardwareType.GpuNvidia)
                {
                    hardware.Update();

                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            sensors.gpuTemp = sensor.Value.GetValueOrDefault(0);
                        }

                        if (sensor.SensorType == SensorType.Load && sensor.Name == "GPU Core")
                        {
                            sensors.gpuLoad = sensor.Value.GetValueOrDefault(0);
                        }

                        if (sensor.SensorType == SensorType.Clock && sensor.Name == "GPU Core")
                        {
                            sensors.gpuFreq = sensor.Value.GetValueOrDefault(0);
                        }

                        if (sensor.SensorType == SensorType.SmallData && sensor.Name == "GPU Memory Used")
                        {
                            sensors.gpuRamUsed = sensor.Value.GetValueOrDefault(0);
                        }

                        if (sensor.SensorType == SensorType.SmallData && sensor.Name == "GPU Memory Total")
                        {
                            sensors.gpuRamTotal = sensor.Value.GetValueOrDefault(0);
                        }

                        if (sensor.SensorType == SensorType.Power)
                        {
                            sensors.gpuPower = sensor.Value.GetValueOrDefault(0);
                        }
                    }
                }

                if (hardware.HardwareType == HardwareType.RAM)
                {
                    hardware.Update();

                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Data && sensor.Name == "Used Memory")
                        {
                            sensors.ramUsed = sensor.Value.GetValueOrDefault(0);
                        }

                        if (sensor.SensorType == SensorType.Data && sensor.Name == "Available Memory")
                        {
                            sensors.ramTotal = sensors.ramUsed + sensor.Value.GetValueOrDefault(0);
                        }
                    }
                }
            }

            if (coresCount < 1)
            {
                coresCount = 1;
            }

            sensors.cpuFreq = totalFrequency / coresCount;
        }

        private void syncTimer_Tick(object sender, EventArgs e)
        {
            Sync();

            List<string> lines = new List<string>();
            var line = "";
            var item = "";

            try
            {
                lblStatus.Text = "Connected to " + port.PortName;

                lines.Add("CPU " + Math.Round(sensors.cpuTemp).ToString().PadLeft(2) + "*C " + Math.Round(sensors.cpuLoad).ToString().PadLeft(2, ' ') + "%  " +
                     Math.Round(sensors.cpuFreq / 1000, 1).ToString('F1') + "GHz");

                lines.Add("GPU " + Math.Round(sensors.gpuTemp).ToString().PadLeft(2) + "*C " + Math.Round(sensors.gpuLoad).ToString().PadLeft(2, ' ') + "%  " +
                     Math.Round(sensors.gpuFreq / 1000, 1).ToString("F1") + "GHz");

                line = "RAM " + Math.Round(sensors.ramUsed).ToString("F1").PadLeft(4) + "/" + Math.Round(sensors.ramTotal).ToString() + "GB";
                item = Math.Round(sensors.cpuPower).ToString() + "W";
                line += item.PadLeft(20 - line.Length);
                lines.Add(line);

                line = "VRAM " + Math.Round(sensors.gpuRamUsed / 1000, 1).ToString("F1") + "/" + Math.Round(sensors.gpuRamTotal / 1000).ToString() + "GB";
                item = Math.Round(sensors.gpuPower).ToString().PadLeft(3) + "W";
                line += item.PadLeft(20 - line.Length);
                lines.Add(line);

                var message = "";
                var label = "";

                foreach (var str in lines)
                {
                    message += str.PadRight(20).Substring(0, 20) + "|";
                    label += str.PadRight(20).Substring(0, 20) + "\n";
                }

                message += "#";

                lblCpuTemp.Text = label;
                port.Write(message);
            }
            catch (Exception ex)
            {
                btnConnect.Text = "Connect";
                lblStatus.Text = ex.Message;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exit = true;
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!exit)
            {
                e.Cancel = true;
            }

            Hide();
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                showForm();
            }
        }

        private void cmbPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            connect();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (port.IsOpen)
            {
                port.Close();
                btnConnect.Text = "Connect";
            }
            else
            {
                connect();
            }
        }

        private void connect()
        {
            try
            {
                port.Close();
                port.PortName = cmbPorts.Text;
                port.Open();
                btnConnect.Text = "Disconnect";
            }
            catch (Exception err)
            {
                btnConnect.Text = "Connect";
                lblStatus.Text = err.Message;
            }
        }

        private void showForm()
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showForm();
        }
    }
}
