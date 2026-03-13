using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kursovaya
{
    partial class AddOrderForm
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
            this.lblClient = new System.Windows.Forms.Label();
            this.cmbClient = new System.Windows.Forms.ComboBox();
            this.lblBoat = new System.Windows.Forms.Label();
            this.cmbBoat = new System.Windows.Forms.ComboBox();
            this.lblStart = new System.Windows.Forms.Label();
            this.dtpStart = new System.Windows.Forms.DateTimePicker();
            this.lblEnd = new System.Windows.Forms.Label();
            this.dtpEnd = new System.Windows.Forms.DateTimePicker();
            this.lblTotalPrice = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblClient
            // 
            this.lblClient.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.lblClient.Location = new System.Drawing.Point(20, 31);
            this.lblClient.Name = "lblClient";
            this.lblClient.Size = new System.Drawing.Size(100, 20);
            this.lblClient.TabIndex = 0;
            this.lblClient.Text = "Клиент:";
            // 
            // cmbClient
            // 
            this.cmbClient.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.cmbClient.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbClient.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbClient.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.cmbClient.Location = new System.Drawing.Point(24, 54);
            this.cmbClient.Name = "cmbClient";
            this.cmbClient.Size = new System.Drawing.Size(278, 28);
            this.cmbClient.TabIndex = 1;
            // 
            // lblBoat
            // 
            this.lblBoat.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.lblBoat.Location = new System.Drawing.Point(20, 96);
            this.lblBoat.Name = "lblBoat";
            this.lblBoat.Size = new System.Drawing.Size(100, 20);
            this.lblBoat.TabIndex = 2;
            this.lblBoat.Text = "Лодка:";
            // 
            // cmbBoat
            // 
            this.cmbBoat.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.cmbBoat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBoat.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbBoat.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.cmbBoat.Location = new System.Drawing.Point(24, 119);
            this.cmbBoat.Name = "cmbBoat";
            this.cmbBoat.Size = new System.Drawing.Size(278, 28);
            this.cmbBoat.TabIndex = 3;
            this.cmbBoat.SelectedIndexChanged += new System.EventHandler(this.CmbBoat_SelectedIndexChanged);
            // 
            // lblStart
            // 
            this.lblStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.lblStart.Location = new System.Drawing.Point(20, 161);
            this.lblStart.Name = "lblStart";
            this.lblStart.Size = new System.Drawing.Size(114, 20);
            this.lblStart.TabIndex = 4;
            this.lblStart.Text = "Дата начала:";
            // 
            // dtpStart
            // 
            this.dtpStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.dtpStart.Location = new System.Drawing.Point(24, 184);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(278, 26);
            this.dtpStart.TabIndex = 5;
            this.dtpStart.ValueChanged += new System.EventHandler(this.DtpStart_ValueChanged);
            // 
            // lblEnd
            // 
            this.lblEnd.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.lblEnd.Location = new System.Drawing.Point(20, 231);
            this.lblEnd.Name = "lblEnd";
            this.lblEnd.Size = new System.Drawing.Size(138, 20);
            this.lblEnd.TabIndex = 6;
            this.lblEnd.Text = "Дата окончания:";
            // 
            // dtpEnd
            // 
            this.dtpEnd.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.dtpEnd.Location = new System.Drawing.Point(24, 254);
            this.dtpEnd.Name = "dtpEnd";
            this.dtpEnd.Size = new System.Drawing.Size(278, 26);
            this.dtpEnd.TabIndex = 7;
            this.dtpEnd.ValueChanged += new System.EventHandler(this.DtpEnd_ValueChanged);
            // 
            // lblTotalPrice
            // 
            this.lblTotalPrice.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.lblTotalPrice.Location = new System.Drawing.Point(20, 298);
            this.lblTotalPrice.Name = "lblTotalPrice";
            this.lblTotalPrice.Size = new System.Drawing.Size(282, 20);
            this.lblTotalPrice.TabIndex = 8;
            this.lblTotalPrice.Text = "Общая стоимость: 0 руб";
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.btnSave.Location = new System.Drawing.Point(12, 337);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(108, 50);
            this.btnSave.TabIndex = 9;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(149)))), ((int)(((byte)(165)))), ((int)(((byte)(166)))));
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.btnCancel.Location = new System.Drawing.Point(196, 337);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(106, 50);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // AddOrderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.ClientSize = new System.Drawing.Size(314, 399);
            this.ControlBox = false;
            this.Controls.Add(this.lblClient);
            this.Controls.Add(this.cmbClient);
            this.Controls.Add(this.lblBoat);
            this.Controls.Add(this.cmbBoat);
            this.Controls.Add(this.lblStart);
            this.Controls.Add(this.dtpStart);
            this.Controls.Add(this.lblEnd);
            this.Controls.Add(this.dtpEnd);
            this.Controls.Add(this.lblTotalPrice);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "AddOrderForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Добавление нового заказа";
            this.ResumeLayout(false);

        }

        private string connectionString = ConnectionString.GetConnectionString();
        private ComboBox cmbClient;
        private ComboBox cmbBoat;
        private DateTimePicker dtpStart;
        private DateTimePicker dtpEnd;
        private Label lblTotalPrice;
        private Button btnSave;
        private Button btnCancel;

        #endregion

        private Label lblClient;
        private Label lblBoat;
        private Label lblStart;
        private Label lblEnd;
    }
}