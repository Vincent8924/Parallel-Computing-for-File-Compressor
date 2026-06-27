namespace compressgui
{
    partial class Form1
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
            rbCompress = new RadioButton();
            rbDecompress = new RadioButton();
            grpCompress = new GroupBox();
            lblTime = new Label();
            label9 = new Label();
            lblFiles = new Label();
            label7 = new Label();
            lblSpeed = new Label();
            label1 = new Label();
            txtSource = new TextBox();
            btnStart = new Button();
            browsebtn = new Button();
            lblStatus = new Label();
            label2 = new Label();
            txtOutput = new TextBox();
            btnsave = new Button();
            label4 = new Label();
            progressBar1 = new ProgressBar();
            label6 = new Label();
            label3 = new Label();
            grpDecompress = new GroupBox();
            lblTimeDe = new Label();
            lblFilesDe = new Label();
            label11 = new Label();
            lblSpeedDe = new Label();
            lblStatusDe = new Label();
            label19 = new Label();
            progressBarDe = new ProgressBar();
            btnDecompress = new Button();
            btnBrowseExtractFolder = new Button();
            btnBrowseZip = new Button();
            txtExtractFolder = new TextBox();
            txtZipSource = new TextBox();
            label20 = new Label();
            label21 = new Label();
            label22 = new Label();
            label23 = new Label();
            label24 = new Label();
            grpCompress.SuspendLayout();
            grpDecompress.SuspendLayout();
            SuspendLayout();
            // 
            // rbCompress
            // 
            rbCompress.AutoSize = true;
            rbCompress.Location = new Point(50, 34);
            rbCompress.Name = "rbCompress";
            rbCompress.Size = new Size(78, 19);
            rbCompress.TabIndex = 12;
            rbCompress.TabStop = true;
            rbCompress.Text = "Compress";
            rbCompress.UseVisualStyleBackColor = true;
            rbCompress.CheckedChanged += radioButton1_CheckedChanged;
            // 
            // rbDecompress
            // 
            rbDecompress.AutoSize = true;
            rbDecompress.Location = new Point(278, 34);
            rbDecompress.Name = "rbDecompress";
            rbDecompress.Size = new Size(90, 19);
            rbDecompress.TabIndex = 13;
            rbDecompress.TabStop = true;
            rbDecompress.Text = "Decompress";
            rbDecompress.UseVisualStyleBackColor = true;
            rbDecompress.CheckedChanged += rbDecompress_CheckedChanged_1;
            // 
            // grpCompress
            // 
            grpCompress.Controls.Add(lblTime);
            grpCompress.Controls.Add(label9);
            grpCompress.Controls.Add(lblFiles);
            grpCompress.Controls.Add(label7);
            grpCompress.Controls.Add(lblSpeed);
            grpCompress.Controls.Add(label1);
            grpCompress.Controls.Add(txtSource);
            grpCompress.Controls.Add(btnStart);
            grpCompress.Controls.Add(browsebtn);
            grpCompress.Controls.Add(lblStatus);
            grpCompress.Controls.Add(label2);
            grpCompress.Controls.Add(txtOutput);
            grpCompress.Controls.Add(btnsave);
            grpCompress.Controls.Add(label4);
            grpCompress.Controls.Add(progressBar1);
            grpCompress.Controls.Add(label6);
            grpCompress.Controls.Add(label3);
            grpCompress.Location = new Point(35, 75);
            grpCompress.Name = "grpCompress";
            grpCompress.Size = new Size(360, 514);
            grpCompress.TabIndex = 17;
            grpCompress.TabStop = false;
            // 
            // lblTime
            // 
            lblTime.AutoSize = true;
            lblTime.Location = new Point(111, 325);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(49, 15);
            lblTime.TabIndex = 19;
            lblTime.Text = "00:00:00";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(15, 325);
            label9.Name = "label9";
            label9.Size = new Size(50, 15);
            label9.TabIndex = 33;
            label9.Text = "Elapsed:";
            // 
            // lblFiles
            // 
            lblFiles.AutoSize = true;
            lblFiles.Location = new Point(111, 395);
            lblFiles.Name = "lblFiles";
            lblFiles.Size = new Size(30, 15);
            lblFiles.TabIndex = 32;
            lblFiles.Text = "0 / 0";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(15, 395);
            label7.Name = "label7";
            label7.Size = new Size(33, 15);
            label7.TabIndex = 22;
            label7.Text = "Files:";
            // 
            // lblSpeed
            // 
            lblSpeed.AutoSize = true;
            lblSpeed.Location = new Point(111, 360);
            lblSpeed.Name = "lblSpeed";
            lblSpeed.Size = new Size(44, 15);
            lblSpeed.TabIndex = 31;
            lblSpeed.Text = "0 MB/s";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(15, 19);
            label1.Name = "label1";
            label1.Size = new Size(108, 15);
            label1.TabIndex = 0;
            label1.Text = "Source Folder / File";
            label1.Click += label1_Click;
            // 
            // txtSource
            // 
            txtSource.Location = new Point(15, 59);
            txtSource.Name = "txtSource";
            txtSource.Size = new Size(218, 23);
            txtSource.TabIndex = 5;
            txtSource.TextChanged += txtSource_TextChanged;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(90, 439);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(200, 50);
            btnStart.TabIndex = 9;
            btnStart.Text = "Start Compression";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // browsebtn
            // 
            browsebtn.Location = new Point(262, 59);
            browsebtn.Name = "browsebtn";
            browsebtn.Size = new Size(75, 23);
            browsebtn.TabIndex = 7;
            browsebtn.Text = "Browse";
            browsebtn.UseVisualStyleBackColor = true;
            browsebtn.Click += browsebtn_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(111, 290);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(39, 15);
            lblStatus.TabIndex = 14;
            lblStatus.Text = "Ready";
            lblStatus.Click += label7_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(15, 109);
            label2.Name = "label2";
            label2.Size = new Size(65, 15);
            label2.TabIndex = 1;
            label2.Text = "Output Zip";
            // 
            // txtOutput
            // 
            txtOutput.Location = new Point(15, 149);
            txtOutput.Name = "txtOutput";
            txtOutput.Size = new Size(218, 23);
            txtOutput.TabIndex = 6;
            txtOutput.TextChanged += txtOutput_TextChanged;
            // 
            // btnsave
            // 
            btnsave.Location = new Point(262, 149);
            btnsave.Name = "btnsave";
            btnsave.Size = new Size(75, 23);
            btnsave.TabIndex = 8;
            btnsave.Text = "Save As";
            btnsave.UseVisualStyleBackColor = true;
            btnsave.Click += btnsave_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(15, 360);
            label4.Name = "label4";
            label4.Size = new Size(42, 15);
            label4.TabIndex = 3;
            label4.Text = "Speed:";
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(15, 239);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(322, 23);
            progressBar1.TabIndex = 10;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(15, 199);
            label6.Name = "label6";
            label6.Size = new Size(52, 15);
            label6.TabIndex = 11;
            label6.Text = "Progress";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(15, 290);
            label3.Name = "label3";
            label3.Size = new Size(42, 15);
            label3.TabIndex = 2;
            label3.Text = "Status:";
            label3.Click += label3_Click;
            // 
            // grpDecompress
            // 
            grpDecompress.Controls.Add(lblTimeDe);
            grpDecompress.Controls.Add(lblFilesDe);
            grpDecompress.Controls.Add(label11);
            grpDecompress.Controls.Add(lblSpeedDe);
            grpDecompress.Controls.Add(lblStatusDe);
            grpDecompress.Controls.Add(label19);
            grpDecompress.Controls.Add(progressBarDe);
            grpDecompress.Controls.Add(btnDecompress);
            grpDecompress.Controls.Add(btnBrowseExtractFolder);
            grpDecompress.Controls.Add(btnBrowseZip);
            grpDecompress.Controls.Add(txtExtractFolder);
            grpDecompress.Controls.Add(txtZipSource);
            grpDecompress.Controls.Add(label20);
            grpDecompress.Controls.Add(label21);
            grpDecompress.Controls.Add(label22);
            grpDecompress.Controls.Add(label23);
            grpDecompress.Controls.Add(label24);
            grpDecompress.Location = new Point(495, 73);
            grpDecompress.Name = "grpDecompress";
            grpDecompress.Size = new Size(360, 514);
            grpDecompress.TabIndex = 18;
            grpDecompress.TabStop = false;
            // 
            // lblTimeDe
            // 
            lblTimeDe.AutoSize = true;
            lblTimeDe.Location = new Point(111, 325);
            lblTimeDe.Name = "lblTimeDe";
            lblTimeDe.Size = new Size(49, 15);
            lblTimeDe.TabIndex = 34;
            lblTimeDe.Text = "00:00:00";
            // 
            // lblFilesDe
            // 
            lblFilesDe.AutoSize = true;
            lblFilesDe.Location = new Point(111, 395);
            lblFilesDe.Name = "lblFilesDe";
            lblFilesDe.Size = new Size(30, 15);
            lblFilesDe.TabIndex = 31;
            lblFilesDe.Text = "0 / 0";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(15, 325);
            label11.Name = "label11";
            label11.Size = new Size(50, 15);
            label11.TabIndex = 35;
            label11.Text = "Elapsed:";
            // 
            // lblSpeedDe
            // 
            lblSpeedDe.AutoSize = true;
            lblSpeedDe.Location = new Point(111, 360);
            lblSpeedDe.Name = "lblSpeedDe";
            lblSpeedDe.Size = new Size(44, 15);
            lblSpeedDe.TabIndex = 30;
            lblSpeedDe.Text = "0 MB/s";
            // 
            // lblStatusDe
            // 
            lblStatusDe.AutoSize = true;
            lblStatusDe.Location = new Point(111, 290);
            lblStatusDe.Name = "lblStatusDe";
            lblStatusDe.Size = new Size(39, 15);
            lblStatusDe.TabIndex = 29;
            lblStatusDe.Text = "Ready";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(15, 199);
            label19.Name = "label19";
            label19.Size = new Size(52, 15);
            label19.TabIndex = 28;
            label19.Text = "Progress";
            // 
            // progressBarDe
            // 
            progressBarDe.Location = new Point(15, 239);
            progressBarDe.Name = "progressBarDe";
            progressBarDe.Size = new Size(322, 23);
            progressBarDe.TabIndex = 27;
            // 
            // btnDecompress
            // 
            btnDecompress.Location = new Point(90, 439);
            btnDecompress.Name = "btnDecompress";
            btnDecompress.Size = new Size(200, 50);
            btnDecompress.TabIndex = 26;
            btnDecompress.Text = "Start Decompression";
            btnDecompress.UseVisualStyleBackColor = true;
            btnDecompress.Click += btnDecompress_Click;
            // 
            // btnBrowseExtractFolder
            // 
            btnBrowseExtractFolder.Location = new Point(262, 149);
            btnBrowseExtractFolder.Name = "btnBrowseExtractFolder";
            btnBrowseExtractFolder.Size = new Size(75, 23);
            btnBrowseExtractFolder.TabIndex = 25;
            btnBrowseExtractFolder.Text = "Save As";
            btnBrowseExtractFolder.UseVisualStyleBackColor = true;
            btnBrowseExtractFolder.Click += btnBrowseExtractFolder_Click;
            // 
            // btnBrowseZip
            // 
            btnBrowseZip.Location = new Point(262, 59);
            btnBrowseZip.Name = "btnBrowseZip";
            btnBrowseZip.Size = new Size(75, 23);
            btnBrowseZip.TabIndex = 24;
            btnBrowseZip.Text = "Browse";
            btnBrowseZip.UseVisualStyleBackColor = true;
            btnBrowseZip.Click += btnBrowseZip_Click;
            // 
            // txtExtractFolder
            // 
            txtExtractFolder.Location = new Point(15, 149);
            txtExtractFolder.Name = "txtExtractFolder";
            txtExtractFolder.Size = new Size(218, 23);
            txtExtractFolder.TabIndex = 23;
            // 
            // txtZipSource
            // 
            txtZipSource.Location = new Point(15, 59);
            txtZipSource.Name = "txtZipSource";
            txtZipSource.Size = new Size(218, 23);
            txtZipSource.TabIndex = 22;
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(15, 395);
            label20.Name = "label20";
            label20.Size = new Size(33, 15);
            label20.TabIndex = 21;
            label20.Text = "Files:";
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(15, 360);
            label21.Name = "label21";
            label21.Size = new Size(42, 15);
            label21.TabIndex = 20;
            label21.Text = "Speed:";
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new Point(15, 290);
            label22.Name = "label22";
            label22.Size = new Size(42, 15);
            label22.TabIndex = 19;
            label22.Text = "Status:";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new Point(15, 109);
            label23.Name = "label23";
            label23.Size = new Size(81, 15);
            label23.TabIndex = 18;
            label23.Text = "Output Folder";
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(15, 19);
            label24.Name = "label24";
            label24.Size = new Size(63, 15);
            label24.TabIndex = 17;
            label24.Text = "Source Zip";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(430, 617);
            Controls.Add(grpDecompress);
            Controls.Add(rbDecompress);
            Controls.Add(rbCompress);
            Controls.Add(grpCompress);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CPU Parallel + Cache";
            grpCompress.ResumeLayout(false);
            grpCompress.PerformLayout();
            grpDecompress.ResumeLayout(false);
            grpDecompress.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private RadioButton rbCompress;
        private RadioButton rbDecompress;
        private GroupBox grpCompress;
        private GroupBox grpDecompress;
        private Label lblFilesDe;
        private Label lblSpeedDe;
        private Label lblStatusDe;
        private Label label19;
        private ProgressBar progressBarDe;
        private Button btnDecompress;
        private Button btnBrowseExtractFolder;
        private Button btnBrowseZip;
        private TextBox txtExtractFolder;
        private TextBox txtZipSource;
        private Label label20;
        private Label label21;
        private Label label22;
        private Label label23;
        private Label label24;
        private Button btnStart;
        private Label label4;
        private Label lblStatus;
        private Label label3;
        private ProgressBar progressBar1;
        private Label label6;
        private Button btnsave;
        private TextBox txtOutput;
        private Label label2;
        private Button browsebtn;
        private TextBox txtSource;
        private Label label1;
        private Label label8;
        private Label label7;
        private Label label5;
        private Label lblSpeed;
        private Label lblFiles;
        private Label lblTime;
        private Label label9;
        private Label lblTimeDe;
        private Label label11;
    }
}
