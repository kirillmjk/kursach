using System;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Kursovaya
{
    public partial class AddEditClientForm : Form
    {
        private int? clientId = null;                  // ID клиента (null для нового)
        private string connectionString = ConnectionString.GetConnectionString();

        public AddEditClientForm(int? id = null)
        {
            clientId = id;
            InitializeComponent();

            if (clientId.HasValue)
            {
                LoadClientData();
                this.Text = "Редактирование клиента";
            }
            else
            {
                this.Text = "Добавление клиента";
            }
        }

        // Загрузка данных клиента для редактирования
        private void LoadClientData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT ClientName, Phone, Email, Address 
                                  FROM Clients WHERE ID = @ID";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ID", clientId.Value);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtFullName.Text = reader["ClientName"].ToString();
                            txtPhone.Text = reader["Phone"].ToString();
                            txtEmail.Text = reader["Email"].ToString();
                            txtAddress.Text = reader["Address"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Автоматическое форматирование ФИО (первая буква заглавная)
        private void TxtFullName_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                var culture = new System.Globalization.CultureInfo("ru-RU");
                txtFullName.Text = culture.TextInfo.ToTitleCase(txtFullName.Text.ToLower());
            }
        }

        // Сохранение клиента
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            // Проверка уникальности телефона
            if (!CheckIfPhoneExists(txtPhone.Text.Trim()))
                return;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query;
                    if (clientId.HasValue)
                    {
                        // Обновление
                        query = @"UPDATE Clients SET ClientName = @FullName, Phone = @Phone, 
                                Email = @Email, Address = @Address WHERE ID = @ID";
                    }
                    else
                    {
                        // Добавление
                        query = @"INSERT INTO Clients (ClientName, Phone, Email, Address) 
                                VALUES (@FullName, @Phone, @Email, @Address)";
                    }

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                    command.Parameters.AddWithValue("@Phone", txtPhone.Text.Trim());
                    command.Parameters.AddWithValue("@Email",
                        string.IsNullOrWhiteSpace(txtEmail.Text) ? DBNull.Value : (object)txtEmail.Text.Trim());
                    command.Parameters.AddWithValue("@Address",
                        string.IsNullOrWhiteSpace(txtAddress.Text) ? DBNull.Value : (object)txtAddress.Text.Trim());

                    if (clientId.HasValue)
                        command.Parameters.AddWithValue("@ID", clientId.Value);

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

        // Проверка существования телефона
        private bool CheckIfPhoneExists(string phone)
        {
            try
            {
                string normalizedPhone = Regex.Replace(phone, @"\D", ""); // Только цифры

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Получаем все телефоны
                    string query = clientId.HasValue
                        ? "SELECT ID, Phone FROM Clients WHERE ID != @ID"
                        : "SELECT ID, Phone FROM Clients";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    if (clientId.HasValue)
                        command.Parameters.AddWithValue("@ID", clientId.Value);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dbPhone = reader["Phone"].ToString();
                            string normalizedDbPhone = Regex.Replace(dbPhone, @"\D", "");

                            if (normalizedDbPhone == normalizedPhone)
                            {
                                MessageBox.Show($"Клиент с номером телефона '{dbPhone}' уже существует.",
                                    "Дублирование номера", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                txtPhone.Focus();
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке телефона: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Валидация ввода
        private bool ValidateInput()
        {
            // ФИО
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Введите ФИО клиента", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtFullName.Focus();
                return false;
            }

            // Только русские буквы
            if (!Regex.IsMatch(txtFullName.Text, @"^[а-яА-ЯёЁ\s\-]+$"))
            {
                MessageBox.Show("ФИО должно содержать только русские буквы, пробелы и дефисы", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtFullName.Focus();
                return false;
            }

            // Телефон
            if (string.IsNullOrWhiteSpace(txtPhone.Text) || txtPhone.Text.Contains("_"))
            {
                MessageBox.Show("Введите корректный номер телефона", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPhone.Focus();
                return false;
            }

            string phoneDigits = Regex.Replace(txtPhone.Text, @"\D", "");
            if (phoneDigits.Length < 10)
            {
                MessageBox.Show("Номер телефона должен содержать не менее 10 цифр", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPhone.Focus();
                return false;
            }

            // Email (если заполнен)
            if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !IsValidEmail(txtEmail.Text))
            {
                MessageBox.Show("Введите корректный email адрес", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtEmail.Focus();
                return false;
            }

            return true;
        }

        // Проверка корректности email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Ограничение ввода для ФИО (только русские буквы)
        private void TxtFullName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !Regex.IsMatch(e.KeyChar.ToString(), @"[а-яА-ЯёЁ\s\-]"))
            {
                e.Handled = true;
            }
        }

        // Форматирование телефона при выходе из поля
        private void TxtPhone_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                string digits = Regex.Replace(txtPhone.Text, @"\D", "");

                if (digits.Length >= 11)
                {
                    // +7 (XXX) XXX-XX-XX
                    txtPhone.Text = $"+7 ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-{digits.Substring(7, 2)}-{digits.Substring(9, 2)}";
                }
                else if (digits.Length >= 10)
                {
                    // (XXX) XXX-XX-XX
                    txtPhone.Text = $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 2)}-{digits.Substring(8, 2)}";
                }
            }
        }

        // Ограничение ввода для телефона
        private void TxtPhone_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                e.KeyChar != '+' && e.KeyChar != '(' && e.KeyChar != ')' &&
                e.KeyChar != ' ' && e.KeyChar != '-')
            {
                e.Handled = true;
            }
        }

        // Отмена
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}