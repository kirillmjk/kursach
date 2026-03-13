using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursovaya
{
    // Главная форма администратора
    public partial class AdminForm : Form
    {
        private int currentUserId; // ID текущего пользователя

        // Конструктор - принимает ID пользователя
        public AdminForm(int userId)
        {
            InitializeComponent();
            currentUserId = userId;
        }

        // Выход из системы
        private void ExitButton_Click(object sender, EventArgs e)
        {
            ReturnToAuth();
        }

        // Возврат на форму авторизации
        private void ReturnToAuth()
        {
            DialogResult result = MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение выхода",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.Close();
                Auth authForm = new Auth();
                authForm.Show();
            }
        }

        // Обработка закрытия формы
        private void ManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение выхода",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    Auth authForm = new Auth();
                    authForm.Show();
                }
            }
        }

        // Переход к заказам
        private void button2_Click(object sender, EventArgs e)
        {
            OrderForm orderForm = new OrderForm("Администратор");
            orderForm.Show();
            this.Hide();
        }

        // Переход к управлению пользователями
        private void button1_Click(object sender, EventArgs e)
        {
            UsersForm usersForm = new UsersForm(currentUserId);
            usersForm.Show();
            this.Hide();
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            DbImportForm dbImportForm = new DbImportForm();
            dbImportForm.Show();
            this.Hide();
        }
    }
}