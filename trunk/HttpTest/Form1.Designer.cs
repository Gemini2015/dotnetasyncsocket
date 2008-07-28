namespace HttpTest
{
	partial class Form1
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
			this.fetchButton = new System.Windows.Forms.Button();
			this.sslCheckBox = new System.Windows.Forms.CheckBox();
			this.logRichTextBox = new System.Windows.Forms.RichTextBox();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.SuspendLayout();
			// 
			// fetchButton
			// 
			this.fetchButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.fetchButton.Location = new System.Drawing.Point(13, 13);
			this.fetchButton.Name = "fetchButton";
			this.fetchButton.Size = new System.Drawing.Size(154, 23);
			this.fetchButton.TabIndex = 0;
			this.fetchButton.Text = "Fetch Deusty Homepage";
			this.fetchButton.UseVisualStyleBackColor = true;
			this.fetchButton.Click += new System.EventHandler(this.fetchButton_Click);
			// 
			// sslCheckBox
			// 
			this.sslCheckBox.AutoSize = true;
			this.sslCheckBox.Checked = global::HttpTest.Properties.Settings.Default.UseSSL;
			this.sslCheckBox.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::HttpTest.Properties.Settings.Default, "UseSSL", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.sslCheckBox.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.sslCheckBox.Location = new System.Drawing.Point(173, 16);
			this.sslCheckBox.Name = "sslCheckBox";
			this.sslCheckBox.Size = new System.Drawing.Size(66, 19);
			this.sslCheckBox.TabIndex = 1;
			this.sslCheckBox.Text = "Use SSL";
			this.sslCheckBox.UseVisualStyleBackColor = true;
			// 
			// logRichTextBox
			// 
			this.logRichTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.logRichTextBox.BackColor = System.Drawing.Color.White;
			this.logRichTextBox.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.logRichTextBox.Location = new System.Drawing.Point(12, 42);
			this.logRichTextBox.Name = "logRichTextBox";
			this.logRichTextBox.ReadOnly = true;
			this.logRichTextBox.Size = new System.Drawing.Size(260, 181);
			this.logRichTextBox.TabIndex = 2;
			this.logRichTextBox.Text = "";
			// 
			// progressBar
			// 
			this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar.Location = new System.Drawing.Point(13, 229);
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(259, 23);
			this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressBar.TabIndex = 3;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 264);
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.logRichTextBox);
			this.Controls.Add(this.sslCheckBox);
			this.Controls.Add(this.fetchButton);
			this.MinimumSize = new System.Drawing.Size(265, 150);
			this.Name = "Form1";
			this.Text = "HTTP Test";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button fetchButton;
		private System.Windows.Forms.CheckBox sslCheckBox;
		private System.Windows.Forms.RichTextBox logRichTextBox;
		private System.Windows.Forms.ProgressBar progressBar;
	}
}

