namespace Invoice.Winforms
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
            btnStart = new Button();
            btnStop = new Button();
            comboMode = new ComboBox();
            lblStatus = new Label();
            SuspendLayout();
            // 
            // btnStart
            // 
            btnStart.Location = new Point(60, 92);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(139, 67);
            btnStart.TabIndex = 0;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(225, 92);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(139, 67);
            btnStop.TabIndex = 1;
            btnStop.Text = "Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // comboMode
            // 
            comboMode.FormattingEnabled = true;
            comboMode.Location = new Point(74, 32);
            comboMode.Name = "comboMode";
            comboMode.Size = new Size(182, 33);
            comboMode.TabIndex = 2;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(285, 217);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(79, 25);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "lblStatus";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(464, 251);
            Controls.Add(lblStatus);
            Controls.Add(comboMode);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnStart;
        private Button btnStop;
        private ComboBox comboMode;
        private Label lblStatus;
    }
}
