using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using com.mc2k.MineCity2000;

namespace com.mc2k.gui
{
    public partial class MineCity2000 : Form
    {
        String inputFile = null;
        String outputDir = null;
        Boolean fillUnderground = false;
        String buildingsDir = null;
        SCMapper mapper = null;
        private BackgroundWorker bw = new BackgroundWorker();

        public MineCity2000()
        {
            InitializeComponent();
            this.Text = "MineCity 2000 v0.3";
            openFileDialog1.Filter = "SimCity 2000 cities|*.sc2";
            openFileDialog1.InitialDirectory = @"C:\Program Files\Maxis\SimCity 2000\Cities";
            folderBrowserDialog1.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft";

            bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

            String workingDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (workingDir.Contains("\\MineCity2000-GUI\\bin\\Debug"))
            {
                workingDir = workingDir.Replace("\\MineCity2000-GUI\\bin\\Debug", "");
            }

            buildingsDir = workingDir + "\\buildings";
            mapper = new SCMapper(buildingsDir);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                sCFileLabel.Text = openFileDialog1.FileName;
                inputFile = openFileDialog1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                mCDirLabel.Text = folderBrowserDialog1.SelectedPath;
                String[] tmp = folderBrowserDialog1.SelectedPath.Split(new char[]{'\\'});
                if (tmp[tmp.Length - 1].Equals(".minecraft"))
                {
                    if (!File.Exists(folderBrowserDialog1.SelectedPath + "\\saves"))
                    {
                        Directory.CreateDirectory(folderBrowserDialog1.SelectedPath + "\\saves");
                    }

                    outputDir = folderBrowserDialog1.SelectedPath + "\\saves\\MineCity2000";
                }
                else if (tmp[tmp.Length - 1].Equals("saves"))
                {
                    outputDir = folderBrowserDialog1.SelectedPath + "\\MineCity2000";
                }
                else
                {
                    outputDir = folderBrowserDialog1.SelectedPath + "\\MineCity2000";
                    MessageBox.Show("This is probably not the correct directory");
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (inputFile == null)
            {
                MessageBox.Show("Select a SimCity 2000 city file");
            }
            else if (outputDir == null)
            {
                MessageBox.Show("Select the directory where Minecraft is installed");
            }

            if (bw.IsBusy != true)
            {
                bw.RunWorkerAsync();
            }
            button3.Enabled = false;
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            mapper = new SCMapper(buildingsDir);
            mapper.Worker = worker;
            MapperOptions options = new MapperOptions(fillUnderground);
            mapper.makeMap(inputFile, outputDir, options);
        }
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button3.Enabled = true;
            if ((e.Cancelled == true))
            {
                statusLabel.Text = "Canceled!";
            }

            else if (!(e.Error == null))
            {
                statusLabel.Text = ("Error: " + e.Error.Message);
            }

            else
            {
                statusLabel.Text = "Done!";
            }
        }
        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            statusLabel.Text = (e.ProgressPercentage.ToString() + "%");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            fillUnderground = ((CheckBox)sender).Checked;
        }
    }
}
