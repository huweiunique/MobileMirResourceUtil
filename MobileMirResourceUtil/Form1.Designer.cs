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
            this.SuspendLayout();
            // 
            // btnDePackage
            // 
            this.btnDePackage.Location = new System.Drawing.Point(12, 12);
            this.btnDePackage.Name = "btnDePackage";
            this.btnDePackage.Size = new System.Drawing.Size(75, 23);
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
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(651, 409);
            this.Controls.Add(this.btnDePackage);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.ShowIcon = false;
            this.Text = "资源打开解包";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnDePackage;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}

