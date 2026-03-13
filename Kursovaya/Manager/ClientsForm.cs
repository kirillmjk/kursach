using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Kursovaya
{
    public partial class ClientsForm : Form
    {
        private string connectionString = ConnectionString.GetConnectionString();
        private int currentUserId;                      // ID текущего пользователя
        private bool showSensitiveData = false;          // Показывать ли данные полностью

        public ClientsForm(int currentUserId)
        {
            InitializeComponent();
            this.currentUserId = currentUserId;
            LoadData();
            InitializeControls();
            ApplyFilters();
        }

        // Загрузка данных
        private void LoadData()
        {
            try
            {
                originalData = GetClientsData();
                filteredData = originalData.Copy();
                dataGridView.DataSource = filteredData.DefaultView;
                FillSortComboBoxes();
                UpdateResultsCount();
                FormatDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Получение данных из БД
        private DataTable GetClientsData()
        {
            DataTable dataTable = new DataTable();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT 
                        ID,
                        ClientName,
                        Phone,
                        Email,
                        Address
                    FROM Clients
                    ORDER BY ClientName";

                MySqlCommand command = new MySqlCommand(query, connection);
                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                adapter.Fill(dataTable);
            }

            return dataTable;
        }

        // Заполнение комбобоксов сортировки
        private void FillSortComboBoxes()
        {
            cmbSortBy.Items.Clear();
            cmbSortBy.Items.AddRange(new string[] { "ФИО", "Телефон", "Email" });
            cmbSortBy.SelectedIndex = 0;

            cmbSortOrder.Items.Clear();
            cmbSortOrder.Items.AddRange(new string[] { "По возрастанию", "По убыванию" });
            cmbSortOrder.SelectedIndex = 0;
        }

        private void InitializeControls()
        {
            dataGridView.AutoGenerateColumns = true;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.ReadOnly = true;
            dataGridView.CellFormatting += DataGridView_CellFormatting;
        }

        // Форматирование ячеек (маскирование данных)
        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && !showSensitiveData)
            {
                // Маскирование телефона (показываем последние 4 цифры)
                if (dataGridView.Columns[e.ColumnIndex].Name == "Phone" && e.Value != null)
                {
                    string phone = e.Value.ToString();
                    if (!string.IsNullOrEmpty(phone))
                    {
                        char[] phoneChars = phone.ToCharArray();
                        int digitCount = 0;

                        for (int i = phoneChars.Length - 1; i >= 0; i--)
                        {
                            if (char.IsDigit(phoneChars[i]))
                            {
                                digitCount++;
                                if (digitCount <= 4)
                                    phoneChars[i] = '*';
                            }
                        }
                        e.Value = new string(phoneChars);
                        e.FormattingApplied = true;
                    }
                }
                // Маскирование email
                else if (dataGridView.Columns[e.ColumnIndex].Name == "Email" && e.Value != null)
                {
                    string email = e.Value.ToString();
                    if (!string.IsNullOrEmpty(email))
                    {
                        int atIndex = email.IndexOf('@');
                        if (atIndex > 0)
                        {
                            string localPart = email.Substring(0, atIndex);
                            string domain = email.Substring(atIndex);

                            if (localPart.Length > 1)
                            {
                                e.Value = localPart[0] + new string('*', localPart.Length - 1) + domain;
                            }
                            else
                            {
                                e.Value = new string('*', localPart.Length) + domain;
                            }
                            e.FormattingApplied = true;
                        }
                    }
                }
            }
        }

        // Применение фильтров
        private void ApplyFilters()
        {
            if (originalData == null) return;

            filteredData = originalData.Copy();

            string searchText = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                var filteredRows = filteredData.AsEnumerable()
                    .Where(row =>
                        row.Field<string>("ClientName").ToLower().Contains(searchText) ||
                        row.Field<string>("Phone").ToLower().Contains(searchText) ||
                        (row.Field<string>("Email") != null && row.Field<string>("Email").ToLower().Contains(searchText)) ||
                        (row.Field<string>("Address") != null && row.Field<string>("Address").ToLower().Contains(searchText)));

                filteredData = filteredRows.Any() ? filteredRows.CopyToDataTable() : filteredData.Clone();
            }

            ApplySorting();
            UpdateResultsCount();
        }

        // Применение сортировки
        private void ApplySorting()
        {
            if (filteredData == null || filteredData.Rows.Count == 0)
            {
                dataGridView.DataSource = null;
                return;
            }

            string sortBy = cmbSortBy.SelectedItem?.ToString();
            string sortOrder = cmbSortOrder.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(sortBy)) return;

            string sortDirection = sortOrder == "По убыванию" ? "DESC" : "ASC";
            string sortExpression = "";

            switch (sortBy)
            {
                case "ФИО": sortExpression = $"ClientName {sortDirection}"; break;
                case "Телефон": sortExpression = $"Phone {sortDirection}"; break;
                case "Email": sortExpression = $"Email {sortDirection}"; break;
                default: sortExpression = $"ClientName {sortDirection}"; break;
            }

            filteredData.DefaultView.Sort = sortExpression;
            dataGridView.DataSource = filteredData.DefaultView;
            FormatDataGridView();
        }

        // Настройка отображения DataGridView
        private void FormatDataGridView()
        {
            if (dataGridView.Columns.Count > 0)
            {
                if (dataGridView.Columns["ID"] != null)
                {
                    dataGridView.Columns["ID"].HeaderText = "ID";
                    dataGridView.Columns["ID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }

                if (dataGridView.Columns["ClientName"] != null)
                {
                    dataGridView.Columns["ClientName"].HeaderText = "ФИО";
                    dataGridView.Columns["ClientName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }

                if (dataGridView.Columns["Phone"] != null)
                {
                    dataGridView.Columns["Phone"].HeaderText = "Телефон";
                    dataGridView.Columns["Phone"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }

                if (dataGridView.Columns["Email"] != null)
                {
                    dataGridView.Columns["Email"].HeaderText = "Email";
                    dataGridView.Columns["Email"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }

                if (dataGridView.Columns["Address"] != null)
                {
                    dataGridView.Columns["Address"].HeaderText = "Адрес";
                    dataGridView.Columns["Address"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }

                dataGridView.RowHeadersVisible = false;

                foreach (DataGridViewColumn column in dataGridView.Columns)
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        // Обновление счетчика результатов
        private void UpdateResultsCount()
        {
            int totalCount = originalData?.Rows.Count ?? 0;
            int filteredCount = filteredData?.Rows.Count ?? 0;

            lblResults.Text = totalCount == filteredCount
                ? $"Всего клиентов: {totalCount}"
                : $"Показано: {filteredCount} из {totalCount}";
        }

        // Добавление клиента
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            AddEditClientForm addForm = new AddEditClientForm();
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadData();
                MessageBox.Show("Клиент успешно добавлен", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Редактирование клиента
        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                int clientId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["ID"].Value);
                AddEditClientForm editForm = new AddEditClientForm(clientId);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadData();
                    MessageBox.Show("Данные клиента успешно изменены", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите клиента для редактирования", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Удаление клиента
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                DialogResult result = MessageBox.Show("Вы уверены, что хотите удалить выбранного клиента?",
                    "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        int clientId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["ID"].Value);

                        using (MySqlConnection connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = "DELETE FROM Clients WHERE ID = @ID";
                            MySqlCommand command = new MySqlCommand(query, connection);
                            command.Parameters.AddWithValue("@ID", clientId);

                            command.ExecuteNonQuery();
                            LoadData();
                            MessageBox.Show("Клиент успешно удален", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Невозможно удалить клиента: у него есть заказы", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Сброс фильтров
        private void BtnReset_Click(object sender, EventArgs e)
        {
            ResetAllFilters();
        }

        private void ResetAllFilters()
        {
            txtSearch.Text = "";
            cmbSortBy.SelectedIndex = 0;
            cmbSortOrder.SelectedIndex = 0;

            if (originalData != null)
            {
                filteredData = originalData.Copy();
                dataGridView.DataSource = filteredData.DefaultView;
                FormatDataGridView();
                UpdateResultsCount();
            }
        }

        // Поиск при изменении текста
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void OnSortChanged(object sender, EventArgs e)
        {
            ApplySorting();
        }

        // Возврат в меню
        private void BtnBack_Click(object sender, EventArgs e)
        {
            this.Close();
            ManagerForm mf = new ManagerForm(currentUserId);
            mf.Show();
        }
    }
}