﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MobileMirResourceUtil
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void BtnDePackage_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                this.btnDePackage.Enabled = false;
                Stopwatch stopwatch = Stopwatch.StartNew();
                ResourceInfo resource = new ResourceInfo();
                await resource.DePackageAsync(openFileDialog1.FileName);
                stopwatch.Stop();
                MessageBox.Show($"解包完成,耗时:{stopwatch.ElapsedMilliseconds}毫秒");
                this.btnDePackage.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.btnDePackage.Enabled = true;
            }
        }

        private async void BtnPackage_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                this.btnPackage.Enabled = false;
                Stopwatch stopwatch = Stopwatch.StartNew();
                ResourceInfo resource = new ResourceInfo();
                await resource.PackageAsync(folderBrowserDialog1.SelectedPath);
                stopwatch.Stop();
                MessageBox.Show($"打包完成,耗时:{stopwatch.ElapsedMilliseconds}毫秒");

            }
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
            finally
            {
                this.btnPackage.Enabled = true;
            }
        }
    }
}
