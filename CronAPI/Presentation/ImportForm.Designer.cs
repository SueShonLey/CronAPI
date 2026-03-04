namespace CronAPI.Presentation
{
    partial class ImportForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            label3 = new Label();
            button2 = new Button();
            label2 = new Label();
            label1 = new Label();
            panel1 = new Panel();
            label4 = new Label();
            button1 = new Button();
            groupBox1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.BackColor = Color.Transparent;
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(button2);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(panel1);
            groupBox1.Controls.Add(button1);
            groupBox1.Location = new Point(12, 9);
            groupBox1.Margin = new Padding(3, 2, 3, 2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(3, 2, 3, 2);
            groupBox1.Size = new Size(605, 335);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "操作台";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = Color.Blue;
            label3.Location = new Point(9, 67);
            label3.Name = "label3";
            label3.Size = new Size(512, 15);
            label3.TabIndex = 4;
            label3.Text = "提示语:请先下载模板，填写好模板后拖动到下方区域，再点击开始导入";
            // 
            // button2
            // 
            button2.Enabled = false;
            button2.Location = new Point(401, 25);
            button2.Name = "button2";
            button2.Size = new Size(186, 29);
            button2.TabIndex = 3;
            button2.Text = "开始导入";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(336, 32);
            label2.Name = "label2";
            label2.Size = new Size(55, 15);
            label2.TabIndex = 2;
            label2.Text = "操作：";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(9, 32);
            label1.Name = "label1";
            label1.Size = new Size(55, 15);
            label1.TabIndex = 1;
            label1.Text = "数据：";
            // 
            // panel1
            // 
            panel1.Controls.Add(label4);
            panel1.Location = new Point(9, 108);
            panel1.Name = "panel1";
            panel1.Size = new Size(578, 209);
            panel1.TabIndex = 1;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("微软雅黑", 15F, FontStyle.Bold, GraphicsUnit.Point);
            label4.Location = new Point(180, 91);
            label4.Name = "label4";
            label4.Size = new Size(215, 33);
            label4.TabIndex = 5;
            label4.Text = "将文件拖动到此处";
            // 
            // button1
            // 
            button1.Location = new Point(70, 25);
            button1.Name = "button1";
            button1.Size = new Size(186, 29);
            button1.TabIndex = 0;
            button1.Text = "下载模板Excel";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // ImportForm
            // 
            AutoScaleDimensions = new SizeF(9F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.yellow;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(634, 358);
            Controls.Add(groupBox1);
            DoubleBuffered = true;
            Font = new Font("宋体", 9F, FontStyle.Bold, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            Name = "ImportForm";
            Text = "导入操作";
            Load += ImportForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private Panel panel1;
        private Button button1;
        private Label label3;
        private Button button2;
        private Label label2;
        private Label label1;
        private Label label4;
    }
}