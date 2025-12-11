using System.IO.Ports;

namespace TenzoEmulatorWin
{
    public partial class MainForm : Form
    {
        private SerialPort port;
        private Thread workerThread;
        private volatile bool isRunning = false;

        // Настройки эмулятора
        private byte myAddr = 0x01;
        private uint mySerial = 0x000001;
        private double currentWeight = 123.45;
        private int decimalPlaces = 2;
        private int baudRate = 9600;
        private bool isStable = true;
        private bool isOverload = false;
        private bool isNegative = false;
        private string portName = "Com1";

        private NotifyIcon trayIcon;

        public MainForm()
        {
            InitializeComponent();
            InitializeTray();
            LoadSettings();
        }
        protected override void WndProc(ref Message m)
        {
            const int WM_SHOWME = 0x8001;

            if (m.Msg == WM_SHOWME)
            {
                ShowMe();
            }

            base.WndProc(ref m);
        }
         
        private void ShowMe()
        {
            this.ShowInTaskbar = true;

            // показать окно
            if (!this.Visible)
                this.Show();

            // восстановить
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;

            // поднять вверх
            this.Activate();
            this.BringToFront();
        }
        private void InitializeTray()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Text = "Тензо-М Эмулятор",
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };
            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("Открыть", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            trayIcon.ContextMenuStrip.Items.Add("Выход", null, (s, e) => Application.Exit());
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            cmbPort.Items.AddRange(SerialPort.GetPortNames());
            if (cmbPort.Items.Count > 0) cmbPort.SelectedIndex = 0;

            cmbBaud.SelectedIndex = cmbBaud.Items.IndexOf("9600");
            numAddr.Value = myAddr;
            numSerial.Value = mySerial;
            numWeight.Value = (decimal)currentWeight;
            numDecimals.Value = decimalPlaces;

            chkStable.Checked = isStable;
            chkOverload.Checked = isOverload;
            chkNegative.Checked = isNegative;
        }




        private void SerialWorker()
        {
            while (isRunning)
            {
                SerialPort sp = null;
                try
                {
                    sp = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                    {
                        ReadTimeout = 500,
                        WriteTimeout = 500,
                        RtsEnable = true,
                        DtrEnable = true
                    };
                    sp.Open();
                    LogInvoke($"[OK] {portName} подключён");

                    while (isRunning && sp.IsOpen)
                    {
                        var frame = ReadFrame(sp);
                        if (frame != null)
                        {
                            var resp = ProcessFrame(frame);
                            if (resp != null)
                                SendFrame(sp, resp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogInvoke($"[ERR] {ex.Message}");
                }
                finally
                {
                    try { sp?.Close(); sp?.Dispose(); } catch { }
                }

                if (isRunning)
                {
                    LogInvoke("Переподключение через 1 сек...");
                    Thread.Sleep(1000);
                }
            }
        }

        private byte[] ProcessFrame(byte[] payload)
        {
            if (payload.Length < 3) return null;

            int pos = 0;
            bool extended = payload[pos] == 0x00;
            uint receivedSerial = 0;
            byte addr = 0;

            if (extended)
            {
                if (payload.Length < 4) return null;
                receivedSerial = (uint)(payload[1] << 16 | payload[2] << 8 | payload[3]);
                pos += 4;
            }
            else
            {
                addr = payload[0];
                pos++;
            }

            byte cop = payload[pos++];

            // Проверка адреса
            if (extended && receivedSerial != mySerial) return null;
            if (!extended && addr != myAddr) return null;

            switch (cop)
            {
                case 0xA0: // Назначение адреса
                    if (payload.Length >= pos + 1 && payload[pos] >= 1 && payload[pos] <= 0x9F)
                    {
                        myAddr = payload[pos];
                        LogInvoke($"Новый адрес: 0x{myAddr:X2}");
                        Invoke((Action)(() => numAddr.Value = myAddr));
                    }
                    return new byte[] { extended ? (byte)0 : myAddr, cop };

                case 0xA1: // Серийный номер
                    return new byte[] { 0x00, cop, (byte)(mySerial >> 16), (byte)(mySerial >> 8), (byte)mySerial };

                case 0xC0: // Тарирование
                    currentWeight = 0;
                    Invoke((Action)(() => numWeight.Value = 0));
                    return new byte[] { extended ? (byte)0 : myAddr, cop };

                case 0xC2: // Нетто
                case 0xC3: // Брутто
                    return BuildWeightResponse(extended, cop);

                default:
                    return null;
            }
        }

        private byte[] BuildWeightResponse(bool extended, byte cop)
        {
            long val = (long)Math.Abs(currentWeight * Math.Pow(10, decimalPlaces));
            string s = val.ToString("D6");

            byte w0 = (byte)((s[5] - '0') | ((s[4] - '0') << 4));
            byte w1 = (byte)((s[3] - '0') | ((s[2] - '0') << 4));
            byte w2 = (byte)((s[1] - '0') | ((s[0] - '0') << 4));

            byte con = (byte)(
                (isNegative ? 0x80 : 0) |
                (isStable ? 0x10 : 0) |
                (isOverload ? 0x08 : 0) |
                (decimalPlaces & 0x03));

            var resp = new List<byte>();
            if (extended) resp.Add(0x00);
            resp.Add(extended ? (byte)0 : myAddr);
            resp.Add(cop);
            resp.Add(w0); resp.Add(w1); resp.Add(w2); resp.Add(con);
            return resp.ToArray();
        }

        private void SendFrame(SerialPort p, byte[] payload)
        {
            p.Write(new byte[] { 0xFF }, 0, 1);
            byte crc = 0;
            foreach (byte b in payload)
            {
                crc = CalcCrc(b, crc);
                p.Write(new byte[] { b }, 0, 1);
                if (b == 0xFF) p.Write(new byte[] { 0xFE }, 0, 1);
            }
            p.Write(new byte[] { crc }, 0, 1);
            if (crc == 0xFF) p.Write(new byte[] { 0xFE }, 0, 1);
            p.Write(new byte[] { 0xFF, 0xFF }, 0, 2);
        }

        private byte[] ReadFrame(SerialPort p)
        {
            try
            {
                while (p.ReadByte() != 0xFF) { }
                var payload = new List<byte>();
                while (true)
                {
                    int b = p.ReadByte();
                    if (b == 0xFF)
                    {
                        int next = p.ReadByte();
                        if (next == 0xFF) return payload.Count >= 3 ? payload.ToArray() : null;
                        if (next == 0xFE) b = 0xFF;
                        else { payload.Add(0xFF); payload.Add((byte)next); continue; }
                    }
                    else if (b == 0xFE) continue;
                    payload.Add((byte)b);
                }
            }
            catch { return null; }
        }

        private byte CalcCrc(byte b, byte crc)
        {
            byte x = (byte)(crc ^ b);
            x = (byte)(x ^ (x << 4));
            return (byte)(x ^ (x >> 3) ^ (x << 5));
        }

        // === Вспомогательные методы ===
        private void LogInvoke(string text)
        {
            if (txtLog.InvokeRequired)
                txtLog.Invoke((Action)(() => txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + text + "\r\n")));
            else
                txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + text + "\r\n");
        }

        private void log(string text) => LogInvoke(text);

        // Сохранение/загрузка настроек
        private void SaveSettings()
        {
            Properties.Settings.Default.Port = cmbPort.SelectedText;
            Properties.Settings.Default.Baud = cmbBaud.Text;
            Properties.Settings.Default.Address = myAddr;
            Properties.Settings.Default.Serial = mySerial;
            Properties.Settings.Default.Save();
        }

        private void LoadSettings()
        {
            cmbPort.SelectedText = Properties.Settings.Default.Port;
            cmbBaud.Text = Properties.Settings.Default.Baud;
            numAddr.Value = Properties.Settings.Default.Address;
            numSerial.Value = Properties.Settings.Default.Serial;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                isRunning = false;
                trayIcon.Visible = false;
            }
            SaveSettings();
        }

        // Обработчики изменения значений
        private void myAddr_ValueChanged(object sender, EventArgs e) => myAddr = (byte)numAddr.Value;
        private void numSerial_ValueChanged(object sender, EventArgs e) => mySerial = (byte)numSerial.Value;

        private void numWeight_ValueChanged(object sender, EventArgs e) => currentWeight = (double)numWeight.Value;
        private void numDecimals_ValueChanged(object sender, EventArgs e) => decimalPlaces = (int)numDecimals.Value;
        private void chkStable_CheckedChanged(object sender, EventArgs e) => isStable = chkStable.Checked;
        private void chkOverload_CheckedChanged(object sender, EventArgs e) => isOverload = chkOverload.Checked;
        private void chkNegative_CheckedChanged(object sender, EventArgs e) => isNegative = chkNegative.Checked;

        private void btnStart_Click_1(object sender, EventArgs e)
        {
            if (isRunning) return;

            portName = cmbPort.Text;
            baudRate = int.Parse(cmbBaud.Text);
            myAddr = (byte)numAddr.Value;
            mySerial = (uint)numSerial.Value;

            isRunning = true;
            workerThread = new Thread(SerialWorker) { IsBackground = true };
            workerThread.Start();

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            log("Эмулятор запущен");
        }

        private void btnStop_Click_1(object sender, EventArgs e)
        {
            isRunning = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            log("Эмулятор остановлен");
        }

        private void combOpen(object sender, EventArgs e)
        {
            cmbPort.Items.Clear();
            cmbPort.Items.AddRange(SerialPort.GetPortNames());
        }
    }

}
