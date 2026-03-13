using System;
using System.Windows.Forms;

namespace Kursovaya
{
    public partial class ManagerForm : Form
    {
        private int currentUserId;  // ID текущего пользователя

        public ManagerForm(int userId)
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

        // Переход к заказам
        private void button2_Click(object sender, EventArgs e)
        {
            OrderForm orderForm = new OrderForm("Менеджер", currentUserId);
            orderForm.Show();
            this.Hide();
        }

        // Переход к услугам (лодкам)
        private void button1_Click(object sender, EventArgs e)
        {
            ManagerServicesForm managerServicesForm = new ManagerServicesForm(currentUserId);
            managerServicesForm.Show();
            this.Hide();
        }

        // Переход к клиентам
        private void button3_Click(object sender, EventArgs e)
        {
            ClientsForm ClientForm = new ClientsForm(currentUserId);
            ClientForm.Show();
            this.Hide();
        }

        // Переход к категориям лодок
        private void BtnManageCategories_Click(object sender, EventArgs e)
        {
            BoatCategoriesForm categoriesForm = new BoatCategoriesForm(currentUserId);
            categoriesForm.Show();
            this.Hide();
        }
    }
}