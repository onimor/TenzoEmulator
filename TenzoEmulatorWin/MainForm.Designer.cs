namespace TenzoEmulatorWin
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            cmbPort = new ComboBox();
            cmbBaud = new ComboBox();
            numAddr = new NumericUpDown();
            numSerial = new NumericUpDown();
            numDecimals = new NumericUpDown();
            numWeight = new NumericUpDown();
            chkStable = new CheckBox();
            chkNegative = new CheckBox();
            chkOverload = new CheckBox();
            btnStart = new Button();
            btnStop = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            txtLog = new RichTextBox();
            ((System.ComponentModel.ISupportInitialize)numAddr).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numSerial).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDecimals).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numWeight).BeginInit();
            SuspendLayout();
            // 
            // cmbPort
            // 
            cmbPort.FormattingEnabled = true;
            cmbPort.Items.AddRange(new object[] { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10", "COM11", "COM12", "COM13", "COM14" });
            cmbPort.Location = new Point(12, 30);
            cmbPort.Name = "cmbPort";
            cmbPort.Size = new Size(151, 28);
            cmbPort.TabIndex = 0;
            // 
            // cmbBaud
            // 
            cmbBaud.FormattingEnabled = true;
            cmbBaud.Items.AddRange(new object[] { "110", "300", "600", "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" });
            cmbBaud.Location = new Point(12, 101);
            cmbBaud.Name = "cmbBaud";
            cmbBaud.Size = new Size(151, 28);
            cmbBaud.TabIndex = 1;
            // 
            // numAddr
            // 
            numAddr.Location = new Point(12, 177);
            numAddr.Name = "numAddr";
            numAddr.Size = new Size(150, 27);
            numAddr.TabIndex = 2;
            numAddr.ValueChanged += myAddr_ValueChanged;
            // 
            // numSerial
            // 
            numSerial.Location = new Point(210, 31);
            numSerial.Name = "numSerial";
            numSerial.Size = new Size(230, 27);
            numSerial.TabIndex = 3;
            numSerial.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numSerial.ValueChanged += numSerial_ValueChanged;
            // 
            // numDecimals
            // 
            numDecimals.Location = new Point(210, 177);
            numDecimals.Maximum = new decimal(new int[] { 3, 0, 0, 0 });
            numDecimals.Name = "numDecimals";
            numDecimals.Size = new Size(230, 27);
            numDecimals.TabIndex = 4;
            numDecimals.Value = new decimal(new int[] { 2, 0, 0, 0 });
            numDecimals.ValueChanged += numDecimals_ValueChanged;
            // 
            // numWeight
            // 
            numWeight.DecimalPlaces = 3;
            numWeight.Location = new Point(210, 101);
            numWeight.Maximum = new decimal(new int[] { 32000, 0, 0, 0 });
            numWeight.Name = "numWeight";
            numWeight.Size = new Size(230, 27);
            numWeight.TabIndex = 5;
            numWeight.Value = new decimal(new int[] { 12345, 0, 0, 131072 });
            numWeight.ValueChanged += numWeight_ValueChanged;
            // 
            // chkStable
            // 
            chkStable.AutoSize = true;
            chkStable.Checked = true;
            chkStable.CheckState = CheckState.Checked;
            chkStable.Location = new Point(489, 34);
            chkStable.Name = "chkStable";
            chkStable.Size = new Size(127, 24);
            chkStable.TabIndex = 6;
            chkStable.Text = "Стабильность";
            chkStable.UseVisualStyleBackColor = true;
            chkStable.CheckedChanged += chkStable_CheckedChanged;
            // 
            // chkNegative
            // 
            chkNegative.AutoSize = true;
            chkNegative.Location = new Point(489, 128);
            chkNegative.Name = "chkNegative";
            chkNegative.Size = new Size(169, 24);
            chkNegative.TabIndex = 7;
            chkNegative.Text = "Отрицательный вес";
            chkNegative.UseVisualStyleBackColor = true;
            chkNegative.CheckedChanged += chkNegative_CheckedChanged;
            // 
            // chkOverload
            // 
            chkOverload.AutoSize = true;
            chkOverload.Location = new Point(489, 78);
            chkOverload.Name = "chkOverload";
            chkOverload.Size = new Size(111, 24);
            chkOverload.TabIndex = 8;
            chkOverload.Text = "Перегрузка";
            chkOverload.UseVisualStyleBackColor = true;
            chkOverload.CheckedChanged += chkOverload_CheckedChanged;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(464, 409);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(94, 29);
            btnStart.TabIndex = 9;
            btnStart.Text = "Старт";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click_1;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(564, 409);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(94, 29);
            btnStop.TabIndex = 10;
            btnStop.Text = "Стоп";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click_1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 7);
            label1.Name = "label1";
            label1.Size = new Size(79, 20);
            label1.TabIndex = 11;
            label1.Text = "COM порт";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 78);
            label2.Name = "label2";
            label2.Size = new Size(73, 20);
            label2.TabIndex = 12;
            label2.Text = "Скорость";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 154);
            label3.Name = "label3";
            label3.Size = new Size(137, 20);
            label3.TabIndex = 13;
            label3.Text = "Номер терминала";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(210, 8);
            label4.Name = "label4";
            label4.Size = new Size(132, 20);
            label4.TabIndex = 14;
            label4.Text = "Серийный номер";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(210, 78);
            label5.Name = "label5";
            label5.Size = new Size(33, 20);
            label5.TabIndex = 15;
            label5.Text = "Вес";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(210, 154);
            label6.Name = "label6";
            label6.Size = new Size(230, 20);
            label6.TabIndex = 16;
            label6.Text = "Количество знаков после точки";
            // 
            // txtLog
            // 
            txtLog.Location = new Point(12, 238);
            txtLog.Name = "txtLog";
            txtLog.Size = new Size(645, 143);
            txtLog.TabIndex = 17;
            txtLog.Text = "";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(669, 450);
            Controls.Add(txtLog);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Controls.Add(chkOverload);
            Controls.Add(chkNegative);
            Controls.Add(chkStable);
            Controls.Add(numWeight);
            Controls.Add(numDecimals);
            Controls.Add(numSerial);
            Controls.Add(numAddr);
            Controls.Add(cmbBaud);
            Controls.Add(cmbPort);
            Name = "MainForm";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)numAddr).EndInit();
            ((System.ComponentModel.ISupportInitialize)numSerial).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDecimals).EndInit();
            ((System.ComponentModel.ISupportInitialize)numWeight).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox cmbPort;
        private ComboBox cmbBaud;
        private NumericUpDown numAddr;
        private NumericUpDown numSerial;
        private NumericUpDown numDecimals;
        private NumericUpDown numWeight;
        private CheckBox chkStable;
        private CheckBox chkNegative;
        private CheckBox chkOverload;
        private Button btnStart;
        private Button btnStop;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private RichTextBox txtLog;
    }
}
