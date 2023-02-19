namespace com.mc2k.gui
{
    partial class MineCity2000
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.button1 = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.button2 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            this.sCFileLabel = new System.Windows.Forms.Label();
            this.mCDirLabel = new System.Windows.Forms.Label();
            this.fillUndergroundCB = new System.Windows.Forms.CheckBox();
            this.generateTerrainCB = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(229, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Choose a SimCity 2000 city file:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(236, 20);
            this.label2.TabIndex = 1;
            this.label2.Text = "Choose your Minecraft directory:";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(251, 15);
            this.button1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(84, 29);
            this.button1.TabIndex = 2;
            this.button1.Text = "Browse";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(251, 56);
            this.button2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(84, 29);
            this.button2.TabIndex = 3;
            this.button2.Text = "Browse";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(102, 471);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 20);
            this.label3.TabIndex = 4;
            this.label3.Text = "Status:";
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(162, 471);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(0, 20);
            this.statusLabel.TabIndex = 5;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(12, 467);
            this.button3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(84, 29);
            this.button3.TabIndex = 6;
            this.button3.Text = "Convert!";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // sCFileLabel
            // 
            this.sCFileLabel.AutoSize = true;
            this.sCFileLabel.Location = new System.Drawing.Point(343, 21);
            this.sCFileLabel.Name = "sCFileLabel";
            this.sCFileLabel.Size = new System.Drawing.Size(0, 20);
            this.sCFileLabel.TabIndex = 7;
            // 
            // mCDirLabel
            // 
            this.mCDirLabel.AutoSize = true;
            this.mCDirLabel.Location = new System.Drawing.Point(342, 64);
            this.mCDirLabel.Name = "mCDirLabel";
            this.mCDirLabel.Size = new System.Drawing.Size(0, 20);
            this.mCDirLabel.TabIndex = 8;
            // 
            // fillUndergroundCB
            // 
            this.fillUndergroundCB.AutoSize = true;
            this.fillUndergroundCB.Location = new System.Drawing.Point(18, 95);
            this.fillUndergroundCB.Name = "fillUndergroundCB";
            this.fillUndergroundCB.Size = new System.Drawing.Size(185, 24);
            this.fillUndergroundCB.TabIndex = 9;
            this.fillUndergroundCB.Text = "Fill underground area";
            this.fillUndergroundCB.UseVisualStyleBackColor = true;
            this.fillUndergroundCB.CheckedChanged += new System.EventHandler(this.fillUndergroundCB_CheckedChanged);
            // 
            // generateTerrainCB
            // 
            this.generateTerrainCB.AutoSize = true;
            this.generateTerrainCB.Location = new System.Drawing.Point(18, 132);
            this.generateTerrainCB.Name = "generateTerrainCB";
            this.generateTerrainCB.Size = new System.Drawing.Size(260, 24);
            this.generateTerrainCB.TabIndex = 10;
            this.generateTerrainCB.Text = "Generate terrain around the city";
            this.generateTerrainCB.UseVisualStyleBackColor = true;
            this.generateTerrainCB.CheckedChanged += new System.EventHandler(this.generateTerrainCB_CheckedChanged);
            // 
            // MineCity2000
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(717, 509);
            this.Controls.Add(this.generateTerrainCB);
            this.Controls.Add(this.fillUndergroundCB);
            this.Controls.Add(this.mCDirLabel);
            this.Controls.Add(this.sCFileLabel);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MineCity2000";
            this.Text = "MineCity 2000";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label sCFileLabel;
        private System.Windows.Forms.Label mCDirLabel;
        private System.Windows.Forms.CheckBox fillUndergroundCB;
        private System.Windows.Forms.CheckBox generateTerrainCB;
    }
}

