using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Kursovaya
{
    public partial class BoatCategoriesForm : Form
    {
        private int currentUserId;                      // ID текущего пользователя
        private DataTable originalData;                  // Исходные данные
        private DataTable filteredData;                  // Отфильтрованные данные
        private string connectionString = ConnectionString.GetConnectionString();

        public BoatCategoriesForm(int currentUserId)
        {
            InitializeComponent();
            this.currentUserId = currentUserId;

            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(800, 600);

            this.Resize += BoatCategoriesForm_Resize;
            this.Load += BoatCategoriesForm_Load;
        }

        private void BoatCategoriesForm_Load(object sender, EventArgs e)
        {
            LoadData();
            InitializeControls();
            ApplyFilters();
        }

        private void InitializeControls()
        {
            dataGridView.AutoGenerateColumns = true;
            dataGridView.RowHeadersVisible = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.MultiSelect = false;
        }

        private void BoatCategoriesForm_Resize(object sender, EventArgs e)
        {
            AdjustDataGridViewColumns();
        }

        // Настройка ширины колонок
        private void AdjustDataGridViewColumns()
        {
            if (dataGridView != null && dataGridView.Columns.Count > 0)
            {
                int availableWidth = dataGridView.Width - 40;

                int totalWidth = 0;
                foreach (DataGridViewColumn column in dataGridView.Columns)
                    totalWidth += column.Width;

                if (totalWidth <= availableWidth)
                {
                    dataGridView.Columns[dataGridView.Columns.Count - 1].AutoSizeMode =
                        DataGridViewAutoSizeColumnMode.Fill;
                }
            }
        }

        // Загрузка данных
        private void LoadData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            bc.ID,
                            bc.CategoryName,
                            COUNT(b.ID) as BoatCount
                        FROM BoatCategories bc
                        LEFT JOIN Boat b ON bc.ID = b.CategoryID
                        GROUP BY bc.ID, bc.CategoryName
                        ORDER BY bc.CategoryName";

                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                    originalData = new DataTable();
                    adapter.Fill(originalData);

                    // Переименование колонок для отображения
                    originalData.Columns["ID"].ColumnName = "Код";
                    originalData.Columns["CategoryName"].ColumnName = "Название класса";
                    originalData.Columns["BoatCount"].ColumnName = "Количество лодок";

                    filteredData = originalData.Copy();
                    dataGridView.DataSource = filteredData.DefaultView;

                    FormatDataGridView();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    .Where(row => row.Field<string>("Название класса").ToLower().Contains(searchText));

                filteredData = filteredRows.Any() ? filteredRows.CopyToDataTable() : filteredData.Clone();
            }

            dataGridView.DataSource = filteredData.DefaultView;
            FormatDataGridView();
        }

        // Настройка отображения DataGridView
        private void FormatDataGridView()
        {
            if (dataGridView.Columns.Count > 0)
            {
                if (dataGridView.Columns["Код"] != null)
                {
                    dataGridView.Columns["Код"].HeaderText = "Код";
                    dataGridView.Columns["Код"].Width = 60;
                    dataGridView.Columns["Код"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                if (dataGridView.Columns["Название класса"] != null)
                {
                    dataGridView.Columns["Название класса"].HeaderText = "Название класса";
                    dataGridView.Columns["Название класса"].Width = 250;
                    dataGridView.Columns["Название класса"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }

                if (dataGridView.Columns["Количество лодок"] != null)
                {
                    dataGridView.Columns["Количество лодок"].HeaderText = "Количество лодок";
                    dataGridView.Columns["Количество лодок"].Width = 120;
                    dataGridView.Columns["Количество лодок"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                foreach (DataGridViewColumn column in dataGridView.Columns)
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            AdjustDataGridViewColumns();
        }

        // Форматирование ячеек
        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView.Columns[e.ColumnIndex].Name == "Количество лодок" && e.Value != null)
            {
                int boatCount = Convert.ToInt32(e.Value);
                if (boatCount == 0)
                {
                    e.CellStyle.ForeColor = Color.Gray;
                    e.CellStyle.Font = new Font(dataGridView.Font, FontStyle.Italic);
                }
                else if (boatCount > 10)
                {
                    e.CellStyle.ForeColor = Color.Green;
                    e.CellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold);
                }
            }
        }

        // Добавление класса
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            AddEditBoatCategoryForm addForm = new AddEditBoatCategoryForm();
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadData();
                MessageBox.Show("Класс транспорта успешно добавлен", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Редактирование класса
        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                int categoryId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["Код"].Value);
                string categoryName = dataGridView.SelectedRows[0].Cells["Название класса"].Value.ToString();

                AddEditBoatCategoryForm editForm = new AddEditBoatCategoryForm(categoryId, categoryName);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadData();
                    MessageBox.Show("Данные успешно изменены", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // Удаление класса
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                int categoryId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["Код"].Value);
                string categoryName = dataGridView.SelectedRows[0].Cells["Название класса"].Value.ToString();
                int boatCount = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["Количество лодок"].Value);

                if (boatCount > 0)
                {
                    MessageBox.Show($"Невозможно удалить класс '{categoryName}', так как он используется в {boatCount} лодках.",
                        "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DialogResult result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить класс '{categoryName}'?",
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
                            string query = "DELETE FROM BoatCategories WHERE ID = @ID";
                            MySqlCommand command = new MySqlCommand(query, connection);
                            command.Parameters.AddWithValue("@ID", categoryId);

                            command.ExecuteNonQuery();
                            LoadData();
                            MessageBox.Show("Класс успешно удален", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
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

            if (originalData != null)
            {
                filteredData = originalData.Copy();
                dataGridView.DataSource = filteredData.DefaultView;
                FormatDataGridView();
            }
        }

        // Поиск при изменении текста
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        // Возврат в меню
        private void BtnBack_Click(object sender, EventArgs e)
        {
            this.Close();

            foreach (Form form in Application.OpenForms)
            {
                if (form is ManagerForm managerForm)
                {
                    managerForm.Show();
                    return;
                }
            }

            ManagerForm mf = new ManagerForm(currentUserId);
            mf.Show();
        }
    }
}