using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace VatApp
{
    public partial class Form2 : Form
    {
        public static string path = VatApp.mConfig[1];

        public Form2(string url, string fileName)
        {
            InitializeComponent();
            DownloadFile(url, fileName);
        }

        /// <summary>
        /// Function converting bytes to megabytes
        /// </summary>
        /// <param name="bytes">Bytes</param>
        /// <returns></returns>
        static double bToMb(long bytes)
        {
            return Math.Round((bytes / 1024f) / 1024f, 0);
        }

        /// <summary>
        /// Function downloading file from url
        /// </summary>
        /// <param name="url">Url to file</param>
        /// <param name="fileName">Path where file should be saved</param>
        public void DownloadFile(string url, string fileName)
        {
            if(Directory.Exists(GetDateYesterday().Replace("-", "")))
            {
                Directory.Delete(GetDateYesterday().Replace("-", ""), true);
            }

            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += (s, e) =>
            {
                progressBar1.Value = e.ProgressPercentage;
                label1.Text = $"Pobrano {bToMb(e.BytesReceived)}MB z {bToMb(e.TotalBytesToReceive)}MB";
            };
            webClient.DownloadFileCompleted += (s, e) =>
            {
                webClient.Dispose();
                ExtractFile(path+$@"\{fileName}", path+$@"\{fileName.Remove(fileName.Length-3, 3)}");
                File.Delete(path+$@"\{fileName}");
                this.Hide();
            };
            webClient.DownloadFileAsync(new Uri(url+fileName), path + $@"\{fileName}");
        }

        /// <summary>
        /// Function extracting .zip file to folder
        /// </summary>
        /// <param name="sourceArchive">Path to .zip archive</param>
        /// <param name="destination">Path where .zip archive should be extracted</param>
        public void ExtractFile(string sourceArchive, string destination)
        {
            string zPath = "7za.exe"; //add to proj and set CopyToOuputDir
            try
            {
                ProcessStartInfo pro = new ProcessStartInfo();
                pro.WindowStyle = ProcessWindowStyle.Hidden;
                pro.FileName = zPath;
                pro.Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", sourceArchive, destination);
                Process x = Process.Start(pro);
                x.WaitForExit();
            }
            catch (System.Exception Ex)
            {
                MessageBox.Show(Ex.Message, "ExtractFile - Wystąpił błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Get date from yesterday
        /// </summary>
        /// <returns></returns>
        private string GetDateYesterday()
        {
            DateTime data = VatApp.mDateTime;

            string rok = data.Year.ToString(), miesiac, dzien;

            if (data.Month < 10) { miesiac = "0" + data.Month.ToString(); }
            else { miesiac = data.Month.ToString(); }

            if ((data.Day-1) < 10)
            {
                dzien = "0" + (data.Day-1).ToString();
            }
            else
            {
                dzien = (data.Day-1).ToString();
            }

            return $"{rok}-{miesiac}-{dzien}";
        }
    }
}
