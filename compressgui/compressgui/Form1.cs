using MyParallelZipApp;
using System.Data;
namespace compressgui
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Compressor.ProgressChanged += UpdateProgress;
            Compressor.StatusChanged += UpdateStatus;
            Compressor.SpeedChanged += UpdateSpeed;
            Compressor.FilesChanged += UpdateFiles;
            Compressor.TimeChanged += UpdateTime;
            grpCompress.Location = new Point(35, 75);
            grpDecompress.Location = new Point(35, 75);
            grpCompress.Visible = true;
            grpDecompress.Visible = false;

        }

        private void UpdateTime(string text)
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateTime(text));
                return;
            }

            if (rbCompress.Checked)
                lblTime.Text = text;
            else
                lblTimeDe.Text = text;
        }
        private void UpdateProgress(int percent)
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateProgress(percent));
                return;
            }
            if (percent > 100) percent = 100;
            if (percent < 0) percent = 0;
            if (rbCompress.Checked)
                progressBar1.Value = percent;
            else
                progressBarDe.Value = percent;
        }
        private void UpdateStatus(string text)
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateStatus(text));
                return;
            }

            if (rbCompress.Checked)
                lblStatus.Text = text;
            else
                lblStatusDe.Text = text;
        }
        private void UpdateSpeed(string text)
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateSpeed(text));
                return;
            }

            if (rbCompress.Checked)
                lblSpeed.Text = text;
            else
                lblSpeedDe.Text = text;
        }
        private void UpdateFiles(string text)
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateFiles(text));
                return;
            }

            if (rbCompress.Checked)
                lblFiles.Text = text;
            else
                lblFilesDe.Text = text;
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (rbCompress.Checked)
            {
                grpCompress.Visible = true;
                grpDecompress.Visible = false;
            }
        }
        private void rbDecompress_CheckedChanged_1(object sender, EventArgs e)
        {
            if (rbDecompress.Checked)
            {
                grpCompress.Visible = false;
                grpDecompress.Visible = true;
            }
        }
        private void browsebtn_Click(object sender, EventArgs e)
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            menu.Items.Add("Select File", null, (s, ev) =>
            {
                using OpenFileDialog dlg = new OpenFileDialog();

                dlg.Filter = "All Files (*.*)|*.*";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtSource.Text = dlg.FileName;

                    // 自动产生默认 zip 名称
                    txtOutput.Text = Path.Combine(
                        Path.GetDirectoryName(dlg.FileName)!,
                        Path.GetFileNameWithoutExtension(dlg.FileName) + ".zip"
                    );
                }
                    
            });

            menu.Items.Add("Select Folder", null, (s, ev) =>
            {
                using FolderBrowserDialog dlg = new FolderBrowserDialog();

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtSource.Text = dlg.SelectedPath;

                    // 自动产生默认 zip 名称
                    txtOutput.Text = Path.Combine(
                        Path.GetDirectoryName(dlg.SelectedPath)!,
                        Path.GetFileName(dlg.SelectedPath) + ".zip"
                    );
                }
                    
            });

            menu.Show(browsebtn, 0, browsebtn.Height);
        }

        private void btnsave_Click(object sender, EventArgs e)
        {
            using SaveFileDialog dlg = new SaveFileDialog();

            dlg.Filter = "Zip Files (*.zip)|*.zip";

            if (!string.IsNullOrWhiteSpace(txtOutput.Text))
            {
                dlg.FileName = Path.GetFileName(txtOutput.Text);
                dlg.InitialDirectory = Path.GetDirectoryName(txtOutput.Text);
            }

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtOutput.Text = dlg.FileName;
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            LockUI();

            try
            {
                await Task.Run(() =>
                {
                    if (rbCompress.Checked)
                    {
                        Compressor.compress(
                            txtOutput.Text,
                            txtSource.Text
                        );
                    }
                    else
                    {
                        Compressor.de(
                            txtSource.Text,
                            txtOutput.Text
                        );
                    }
                });
                await Task.Delay(1000);
                MessageBox.Show("Completed!");
                ResetUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                UnlockUI();
            }
        }

        private void txtSource_TextChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void lblSpeed_Click(object sender, EventArgs e)
        {

        }

        private void txtOutput_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void lblFiles_Click(object sender, EventArgs e)
        {

        }

        private void lblFiles_Click_1(object sender, EventArgs e)
        {

        }
        private void LockUI()
        {
            grpCompress.Enabled = false;
            grpDecompress.Enabled = false;

            rbCompress.Enabled = false;
            rbDecompress.Enabled = false;
        }
        private void UnlockUI()
        {
            grpCompress.Enabled = true;
            grpDecompress.Enabled = true;

            rbCompress.Enabled = true;
            rbDecompress.Enabled = true;
        }
        private void ResetUI()
        {
            // Progress Bar
            progressBar1.Value = 0;
            progressBarDe.Value = 0;

            // Compress
            txtSource.Text = " ";
            txtOutput.Text = " ";
            lblStatus.Text = "Ready";
            lblSpeed.Text = "0 MB/s";
            lblFiles.Text = "0 / 0";
            lblTime.Text = "00:00:00";

            // Decompress
            txtZipSource.Text = " ";
            txtExtractFolder.Text = " ";
            lblStatusDe.Text = "Ready";
            lblSpeedDe.Text = "0 MB/s";
            lblFilesDe.Text = "0 / 0";
            lblTimeDe.Text = "00:00:00";
        }
        private async void btnDecompress_Click(
    object sender,
    EventArgs e)
        {
            LockUI();

            try
            {
                await Task.Run(() =>
                {
                    Compressor.de(
                        txtZipSource.Text,
                        txtExtractFolder.Text
                    );
                });
                await Task.Delay(1000);
                MessageBox.Show("Decompression Completed!");
                ResetUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                UnlockUI();
            }
        }
        private void btnBrowseZip_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dlg = new OpenFileDialog();

            dlg.Filter = "Zip Files (*.zip)|*.zip";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtZipSource.Text = dlg.FileName;

                txtExtractFolder.Text = Path.Combine(
                    Path.GetDirectoryName(dlg.FileName)!,
                    Path.GetFileNameWithoutExtension(dlg.FileName)
                );
            }
        }
        private void btnBrowseExtractFolder_Click(object sender,EventArgs e)
        {
            using FolderBrowserDialog dlg = new FolderBrowserDialog();

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtExtractFolder.Text =
                    dlg.SelectedPath;
            }
        }
    }
}
