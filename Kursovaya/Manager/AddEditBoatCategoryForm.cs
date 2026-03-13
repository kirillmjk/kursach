using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Kursovaya
{
    public partial class AddEditBoatCategoryForm : Form
    {
        private int? categoryId = null;                // ID категории (null для новой)
        private string connectionString = ConnectionString.GetConnectionString();

        public AddEditBoatCategoryForm(int? id = null, string categoryName = "")
        {
            InitializeComponent();
            categoryId = id;

            if (categoryId.HasValue)
            {
                this.Text = "Редактирование класса транспорта";
                txtCategoryName.Text = categoryName;
            }
        }

        // Ограничение ввода - только буквы, цифры и допустимые символы
        private void TxtCategoryName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsLetterOrDigit(e.KeyChar) &&
                e.KeyChar != ' ' && e.KeyChar != '-' && e.KeyChar != '(' && e.KeyChar != ')' &&
                e.KeyChar != '.' && e.KeyChar != ',')
            {
                e.Handled = true;
            }
        }

        // Сохранение категории
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            // Проверка на уникальность
            if (!CheckIfCategoryNameExists(txtCategoryName.Text.Trim()))
                return;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query;
                    MySqlCommand command;

                    if (categoryId.HasValue)
                    {
                        // Обновление существующей
                        query = @"UPDATE BoatCategories SET CategoryName = @CategoryName WHERE ID = @ID";
                        command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@CategoryName", txtCategoryName.Text.Trim());
                        command.Parameters.AddWithValue("@ID", categoryId.Value);
                    }
                    else
                    {
                        // Добавление новой
                        query = @"INSERT INTO BoatCategories (CategoryName) VALUES (@CategoryName)";
                        command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@CategoryName", txtCategoryName.Text.Trim());
                    }

                    command.ExecuteNonQuery();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Проверка уникальности названия категории
        private bool CheckIfCategoryNameExists(string categoryName)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query;
                    MySqlCommand command;

                    if (categoryId.HasValue)
                    {
                        // При редактировании исключаем текущую
                        query = @"SELECT COUNT(*) FROM BoatCategories 
                                WHERE CategoryName = @CategoryName AND ID != @ID";
                        command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@CategoryName", categoryName);
                        command.Parameters.AddWithValue("@ID", categoryId.Value);
                    }
                    else
                    {
                        // При добавлении просто проверяем существование
                        query = @"SELECT COUNT(*) FROM BoatCategories 
                                WHERE CategoryName = @CategoryName";
                        command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@CategoryName", categoryName);
                    }

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    if (count > 0)
                    {
                        MessageBox.Show($"Класс транспорта с названием '{categoryName}' уже существует.",
                            "Дублирование данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtCategoryName.Focus();
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Валидация введенных данных
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtCategoryName.Text))
            {
                MessageBox.Show("Введите название класса транспорта", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtCategoryName.Focus();
                return false;
            }

            if (txtCategoryName.Text.Trim().Length < 2)
            {
                MessageBox.Show("Название класса должно содержать не менее 2 символов", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtCategoryName.Focus();
                return false;
            }

            if (txtCategoryName.Text.Trim().Length > 100)
            {
                MessageBox.Show("Название класса не должно превышать 100 символов", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtCategoryName.Focus();
                return false;
            }

            return true;
        }

        // Отмена
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}