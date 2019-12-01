namespace SmartCertificateKeyProviderPlugin
{
    partial class SaveRecoveryKeyForm
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
            this.button1 = new System.Windows.Forms.Button();
            this.recoveryKeyTextField = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(570, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Copy this text and keep it safe, you will need it if you lose your smartcard cert" +
    "ificate to continue accessing the database.";
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(16, 55);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(179, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "Okay, I have saved this text!";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // recoveryKeyTextField
            // 
            this.recoveryKeyTextField.Location = new System.Drawing.Point(16, 29);
            this.recoveryKeyTextField.Name = "recoveryKeyTextField";
            this.recoveryKeyTextField.Size = new System.Drawing.Size(561, 20);
            this.recoveryKeyTextField.TabIndex = 4;
            // 
            // SaveRecoveryKeyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(590, 91);
            this.Controls.Add(this.recoveryKeyTextField);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label1);
            this.Name = "SaveRecoveryKeyForm";
            this.Text = "SaveRecoveryKeyForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox recoveryKeyTextField;
    }
}