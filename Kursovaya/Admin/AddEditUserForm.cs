using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Kursovaya
{
    // Форма для добавления/редактирования пользователя
    public partial class AddEditUserForm : Form
    {
        private string connectionString = ConnectionString.GetConnectionString();
        private int? userId;                    // ID пользователя (null - новый)
        private bool isUpdatingText = false;    // Флаг для предотвращения рекурсии

        // Конструктор с параметрами
        public AddEditUserForm(int? userId = null, string fullName = "", string login = "", string roleName = "")
        {
            this.userId = userId;
            InitializeComponent();
            LoadRoles(); // Загружаем роли в выпадающий список

            if (userId.HasValue)
            {
                // Режим редактирования
                this.Text = "Редактирование пользователя";
                txtFullName.Text = fullName;
                txtLogin.Text = login;

                // Устанавливаем выбранную роль
                foreach (DataRowView item in cmbRole.Items)
                {
                    if (item["RoleName"].ToString() == roleName)
                    {
                        cmbRole.SelectedItem = item;
                        break;
                    }
                }

                // Подсказка для поля пароля
                lblPasswordHint.Text = "Оставьте пустым, если не меняете пароль";
            }
            else
            {
                // Режим добавления
                this.Text = "Добавление пользователя";
            }
        }

        // Загрузка ролей из БД
        private void LoadRoles()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT ID, RoleName FROM Roles";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataTable rolesTable = new DataTable();
                    adapter.Fill(rolesTable);
                    cmbRole.DataSource = rolesTable;
                    cmbRole.DisplayMember = "RoleName";
                    cmbRole.ValueMember = "ID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ролей: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Хеширование пароля SHA256
        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Автоматическое форматирование ФИО при вводе
        private void TxtFullName_TextChanged(object sender, EventArgs e)
        {
            if (isUpdatingText) return;

            isUpdatingText = true;

            try
            {
                if (!string.IsNullOrWhiteSpace(txtFullName.Text))
                {
                    int selectionStart = txtFullName.SelectionStart;
                    int selectionLength = txtFullName.SelectionLength;

                    // Находим все слова в тексте
                    string pattern = @"[^\s\-]+";
                    MatchCollection matches = Regex.Matches(txtFullName.Text, pattern);
                    string result = txtFullName.Text;

                    // Каждое слово начинаем с заглавной буквы
                    foreach (Match match in matches)
                    {
                        string word = match.Value;
                        if (word.Length > 0)
                        {
                            string formattedWord = char.ToUpper(word[0]) +
                                                  (word.Length > 1 ? word.Substring(1).ToLower() : "");

                            result = result.Remove(match.Index, word.Length);
                            result = result.Insert(match.Index, formattedWord);
                        }
                    }

                    if (!string.IsNullOrEmpty(result) && result != txtFullName.Text)
                    {
                        txtFullName.Text = result;

                        if (selectionStart <= txtFullName.Text.Length)
                        {
                            txtFullName.SelectionStart = selectionStart;
                            txtFullName.SelectionLength = selectionLength;
                        }
                    }
                }
            }
            finally
            {
                isUpdatingText = false;
            }
        }

        // Финальное форматирование ФИО при потере фокуса
        private void TxtFullName_Leave(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                var culture = new System.Globalization.CultureInfo("ru-RU");
                txtFullName.Text = culture.TextInfo.ToTitleCase(txtFullName.Text.ToLower());
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

        // Показ/скрытие пароля
        private void ChkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        }

        // Сохранение пользователя
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверка уникальности логина
                    string checkQuery = "SELECT COUNT(*) FROM Users WHERE Login = @Login";
                    if (userId.HasValue)
                        checkQuery += " AND ID != @ID";

                    MySqlCommand checkCommand = new MySqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@Login", txtLogin.Text.Trim());
                    if (userId.HasValue)
                        checkCommand.Parameters.AddWithValue("@ID", userId.Value);

                    int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());
                    if (userCount > 0)
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (userId.HasValue)
                    {
                        // Редактирование существующего
                        if (string.IsNullOrEmpty(txtPassword.Text))
                        {
                            // Без смены пароля
                            string query = @"UPDATE Users SET 
                                           FullName = @FullName, 
                                           Login = @Login, 
                                           RoleID = @RoleID 
                                           WHERE ID = @ID";

                            MySqlCommand command = new MySqlCommand(query, connection);
                            command.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                            command.Parameters.AddWithValue("@Login", txtLogin.Text.Trim());
                            command.Parameters.AddWithValue("@RoleID", cmbRole.SelectedValue);
                            command.Parameters.AddWithValue("@ID", userId.Value);
                            command.ExecuteNonQuery();
                        }
                        else
                        {
                            // Со сменой пароля
                            string hashedPassword = ComputeSha256Hash(txtPassword.Text);
                            string query = @"UPDATE Users SET 
                                           FullName = @FullName, 
                                           Login = @Login, 
                                           Pass = @Password, 
                                           RoleID = @RoleID 
                                           WHERE ID = @ID";

                            MySqlCommand command = new MySqlCommand(query, connection);
                            command.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                            command.Parameters.AddWithValue("@Login", txtLogin.Text.Trim());
                            command.Parameters.AddWithValue("@Password", hashedPassword);
                            command.Parameters.AddWithValue("@RoleID", cmbRole.SelectedValue);
                            command.Parameters.AddWithValue("@ID", userId.Value);
                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Добавление нового
                        string hashedPassword = ComputeSha256Hash(txtPassword.Text);
                        string query = @"INSERT INTO Users (FullName, Login, Pass, RoleID) 
                                       VALUES (@FullName, @Login, @Password, @RoleID)";

                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                        command.Parameters.AddWithValue("@Login", txtLogin.Text.Trim());
                        command.Parameters.AddWithValue("@Password", hashedPassword);
                        command.Parameters.AddWithValue("@RoleID", cmbRole.SelectedValue);
                        command.ExecuteNonQuery();
                    }

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

        // Валидация формы
        private bool ValidateForm()
        {
            // ФИО
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Введите ФИО пользователя", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtFullName.Focus();
                return false;
            }

            // Только русские буквы в ФИО
            if (!Regex.IsMatch(txtFullName.Text, @"^[а-яА-ЯёЁ\s\-]+$"))
            {
                MessageBox.Show("ФИО должно содержать только русские буквы, пробелы и дефисы", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtFullName.Focus();
                return false;
            }

            // Логин
            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                MessageBox.Show("Введите логин пользователя", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtLogin.Focus();
                return false;
            }

            if (txtLogin.Text.Length > 12)
            {
                MessageBox.Show("Логин не должен превышать 12 символов", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtLogin.Focus();
                return false;
            }

            // Пароль
            if (!userId.HasValue) // Новый пользователь
            {
                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Введите пароль пользователя", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Focus();
                    return false;
                }

                if (txtPassword.Text.Length > 12)
                {
                    MessageBox.Show("Пароль не должен превышать 12 символов", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Focus();
                    return false;
                }
            }
            else // Редактирование
            {
                if (!string.IsNullOrEmpty(txtPassword.Text) && txtPassword.Text.Length > 12)
                {
                    MessageBox.Show("Пароль не должен превышать 12 символов", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Focus();
                    return false;
                }
            }

            // Роль
            if (cmbRole.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите роль пользователя", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmbRole.Focus();
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