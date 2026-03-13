using System;
using System.Windows.Forms;

namespace Kursovaya
{
    public partial class DirectorForm : Form
    {
        private int currentUserId;  // ID текущего пользователя

        public DirectorForm(int userId)
        {
            InitializeComponent();
            currentUserId = userId;
        }

        // Выход из системы
        private void ExitButton_Click(object sender, EventArgs e)
        {
            ReturnToAuth();
        }

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

        // Переход к просмотру заказов
        private void button2_Click(object sender, EventArgs e)
        {
            DirectorOrdersForm directorOrdersForm = new DirectorOrdersForm(currentUserId);
            directorOrdersForm.Show();
            this.Hide();
        }

        // Переход к просмотру услуг (лодок)
        private void btnViewServices_Click(object sender, EventArgs e)
        {
            DirectorServicesForm servicesForm = new DirectorServicesForm(currentUserId);
            servicesForm.Show();
            this.Hide();
        }
    }
}