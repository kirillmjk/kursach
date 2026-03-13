using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Kursovaya
{
    public partial class UsersForm : Form
    {
        private string connectionString = ConnectionString.GetConnectionString();
        private DataTable usersData;
        private int currentUserId; 

        // Элементы управления
        private DataGridView dataGridView;
        private TextBox txtSearch;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnBack;

        // Конструктор с ID текущего пользователя
        public UsersForm(int currentUserId)
        {
            this.currentUserId = currentUserId;
            InitializeComponent();
            LoadUsersData();                // Загружаем список пользователей
            SetupDataGridView();             // Настраиваем таблицу
        }

        // Запасной конструктор (на всякий случай)
        public UsersForm() : this(-1) { }

        // Фокус на поле поиска при клике на подсказку
        private void LblSearchHint_Click(object sender, EventArgs e)
        {
            txtSearch.Focus();
        }

        // Живой поиск при вводе текста
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            if (usersData != null)
            {
                string searchText = txtSearch.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(searchText))
                {
                    // Фильтруем по ФИО, логину или роли
                    var filteredRows = usersData.AsEnumerable()
                        .Where(row =>
                            row.Field<string>("FullName").ToLower().Contains(searchText) ||
                            row.Field<string>("Login").ToLower().Contains(searchText) ||
                            row.Field<string>("RoleName").ToLower().Contains(searchText));

                    dataGridView.DataSource = filteredRows.Any() ? filteredRows.CopyToDataTable() : null;
                }
                else
                {
                    dataGridView.DataSource = usersData;
                }
            }
        }

        // Загрузка данных пользователей из БД
        private void LoadUsersData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            u.ID,
                            u.FullName,
                            u.Login,
                            u.Pass as Password,
                            r.RoleName
                        FROM Users u
                        INNER JOIN Roles r ON u.RoleID = r.ID
                        ORDER BY u.FullName";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    usersData = new DataTable();
                    adapter.Fill(usersData);
                    dataGridView.DataSource = usersData;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Настройка отображения DataGridView
        private void SetupDataGridView()
        {
            if (dataGridView.Columns.Count > 0)
            {
                // Переименовываем заголовки
                if (dataGridView.Columns["ID"] != null)
                    dataGridView.Columns["ID"].HeaderText = "ID";

                if (dataGridView.Columns["FullName"] != null)
                    dataGridView.Columns["FullName"].HeaderText = "ФИО";

                if (dataGridView.Columns["Login"] != null)
                    dataGridView.Columns["Login"].HeaderText = "Логин";

                if (dataGridView.Columns["Password"] != null)
                {
                    dataGridView.Columns["Password"].HeaderText = "Пароль";
                    dataGridView.Columns["Password"].Visible = false; // Скрываем пароль
                }

                if (dataGridView.Columns["RoleName"] != null)
                    dataGridView.Columns["RoleName"].HeaderText = "Роль";

                // Общие настройки
                dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dataGridView.RowHeadersVisible = false;
                dataGridView.AllowUserToResizeColumns = false;
                dataGridView.AllowUserToResizeRows = false;

                // Отключаем сортировку по заголовкам
                foreach (DataGridViewColumn column in dataGridView.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
        }

        // Добавление нового пользователя
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            AddEditUserForm addForm = new AddEditUserForm();
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadUsersData(); // Обновляем данные после добавления
                MessageBox.Show("Пользователь успешно добавлен", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Редактирование выбранного пользователя
        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                // Получаем данные выбранного пользователя
                int userId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["ID"].Value);
                string fullName = dataGridView.SelectedRows[0].Cells["FullName"].Value.ToString();
                string login = dataGridView.SelectedRows[0].Cells["Login"].Value.ToString();
                string roleName = dataGridView.SelectedRows[0].Cells["RoleName"].Value.ToString();

                // Открываем форму редактирования
                AddEditUserForm editForm = new AddEditUserForm(userId, fullName, login, roleName);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadUsersData(); // Обновляем данные
                    MessageBox.Show("Данные пользователя успешно изменены", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для редактирования", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Удаление пользователя
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                int userId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["ID"].Value);
                string fullName = dataGridView.SelectedRows[0].Cells["FullName"].Value.ToString();

                // Запрещаем удаление самого себя
                if (IsCurrentUser(userId))
                {
                    MessageBox.Show("Нельзя удалить текущего пользователя", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DialogResult result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить пользователя '{fullName}'?",
                    "Подтверждение удаления",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        using (MySqlConnection connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = "DELETE FROM Users WHERE ID = @ID";
                            MySqlCommand command = new MySqlCommand(query, connection);
                            command.Parameters.AddWithValue("@ID", userId);
                            command.ExecuteNonQuery();

                            LoadUsersData(); // Обновляем данные
                            MessageBox.Show("Пользователь успешно удален", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для удаления", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Проверка, является ли выбранный пользователь текущим
        private bool IsCurrentUser(int userId)
        {
            return userId == currentUserId;
        }

        // Возврат в меню администратора
        private void BtnBack_Click(object sender, EventArgs e)
        {
            this.Close();
            AdminForm adminForm = new AdminForm(currentUserId);
            adminForm.Show();
        }
    }
}