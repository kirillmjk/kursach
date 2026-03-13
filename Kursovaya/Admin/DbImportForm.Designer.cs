using System.Windows.Forms;

namespace Kursovaya
{
    partial class DbImportForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.btnBack = new System.Windows.Forms.Button();
            this.btnExportFullDB = new System.Windows.Forms.Button();
            this.btnImportSQL = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "SQL files (*.sql)|*.sql|CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            this.openFileDialog1.InitialDirectory = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\Common7\\IDE";
            // 
            // btnBack
            // 
            this.btnBack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(149)))), ((int)(((byte)(165)))), ((int)(((byte)(166)))));
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnBack.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold);
            this.btnBack.ForeColor = System.Drawing.Color.Black;
            this.btnBack.Location = new System.Drawing.Point(46, 137);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(134, 50);
            this.btnBack.TabIndex = 3;
            this.btnBack.Text = "Назад";
            this.btnBack.UseVisualStyleBackColor = false;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // btnExportFullDB
            // 
            this.btnExportFullDB.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btnExportFullDB.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnExportFullDB.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.btnExportFullDB.ForeColor = System.Drawing.Color.Black;
            this.btnExportFullDB.Location = new System.Drawing.Point(12, 12);
            this.btnExportFullDB.Name = "btnExportFullDB";
            this.btnExportFullDB.Size = new System.Drawing.Size(200, 50);
            this.btnExportFullDB.TabIndex = 4;
            this.btnExportFullDB.Text = "Экспорт всей БД";
            this.btnExportFullDB.UseVisualStyleBackColor = false;
            this.btnExportFullDB.Click += new System.EventHandler(this.btnExportFullDB_Click);
            // 
            // btnImportSQL
            // 
            this.btnImportSQL.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btnImportSQL.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnImportSQL.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.btnImportSQL.ForeColor = System.Drawing.Color.Black;
            this.btnImportSQL.Location = new System.Drawing.Point(12, 68);
            this.btnImportSQL.Name = "btnImportSQL";
            this.btnImportSQL.Size = new System.Drawing.Size(200, 50);
            this.btnImportSQL.TabIndex = 6;
            this.btnImportSQL.Text = "Импорт SQL (БД)";
            this.btnImportSQL.UseVisualStyleBackColor = false;
            this.btnImportSQL.Click += new System.EventHandler(this.btnImportSQL_Click);
            // 
            // DbImportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.ClientSize = new System.Drawing.Size(227, 210);
            this.ControlBox = false;
            this.Controls.Add(this.btnImportSQL);
            this.Controls.Add(this.btnExportFullDB);
            this.Controls.Add(this.btnBack);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "DbImportForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Импорт/Экспорт базы данных";
            this.ResumeLayout(false);

        }

        // Компоненты
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Button btnExportFullDB;
        private System.Windows.Forms.Button btnImportSQL;
    }
}