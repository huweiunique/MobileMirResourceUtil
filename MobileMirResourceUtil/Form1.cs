using System;
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
            try
            {
                //Test();
                //return; ;
                if (openFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
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
        }

        private void Test()
        {
            this.btnDePackage.Enabled = false;
            Stopwatch stopwatch = Stopwatch.StartNew();
            var result = Directory.GetFiles(@"C:\Users\huwei\Desktop\data资源文件\rs").AsParallel().Select(file =>
            {
                ResourceInfo resource = new ResourceInfo();
                resource.DePackageAsync(file).Wait();
                return resource;
            }).ToList();
            StringBuilder sb = new StringBuilder();
            foreach (var items in result.GroupBy(x => x.FileLengthOffset))
            {
                foreach (var dtlItems in items.GroupBy(x => x.ChildFiles[0].LastNineBytes[8]))
                {
                    sb.AppendLine($"最后一位为:{dtlItems.Key}的偏移量是:{items.Key},对应资源文件:{string.Join(",", dtlItems.Select(x => x.FileName))}");
                }
            }

            stopwatch.Stop();
            MessageBox.Show($"解包完成,耗时:{stopwatch.ElapsedMilliseconds}毫秒,结果:{Environment.NewLine + sb.ToString()}");
            this.btnDePackage.Enabled = true;
        }
    }
}
