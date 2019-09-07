namespace MobileMirResourceUtil
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnDePackage = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.btnPackage = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.SuspendLayout();
            // 
            // btnDePackage
            // 
            this.btnDePackage.Location = new System.Drawing.Point(30, 12);
            this.btnDePackage.Name = "btnDePackage";
            this.btnDePackage.Size = new System.Drawing.Size(91, 66);
            this.btnDePackage.TabIndex = 0;
            this.btnDePackage.Text = "解包";
            this.btnDePackage.UseVisualStyleBackColor = true;
            this.btnDePackage.Click += new System.EventHandler(this.BtnDePackage_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "资源文件";
            this.openFileDialog1.Filter = "资源文件|*.zip";
            this.openFileDialog1.Title = "选择要解压的手游资源文件";
            // 
            // btnPackage
            // 
            this.btnPackage.Location = new System.Drawing.Point(153, 12);
            this.btnPackage.Name = "btnPackage";
            this.btnPackage.Size = new System.Drawing.Size(85, 66);
            this.btnPackage.TabIndex = 1;
            this.btnPackage.Text = "打包";
            this.btnPackage.UseVisualStyleBackColor = true;
            this.btnPackage.Click += new System.EventHandler(this.BtnPackage_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(275, 92);
            this.Controls.Add(this.btnPackage);
            this.Controls.Add(this.btnDePackage);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "战神引擎手游资源工具";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnDePackage;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnPackage;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
    }
}

