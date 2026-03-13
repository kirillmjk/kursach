using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace Kursovaya
{
    // Форма для работы с заказами
    public partial class OrderForm : Form
    {
        // Поля класса
        private int currentUserId;              // ID текущего пользователя
        private DataTable originalData;          // Исходные данные из БД
        private DataTable filteredData;          // Отфильтрованные данные
        private OrderDataHelper dataHelper;      // Помощник для работы с данными
        private string currentUserRole;           // Роль текущего пользователя
        private string connectionString = ConnectionString.GetConnectionString(); // Строка подключения

        // Конструктор формы
        public OrderForm(string userRole = "Менеджер", int currentUserId = 0)
        {
            currentUserRole = userRole;
            this.currentUserId = currentUserId;
            InitializeComponent();
            dataHelper = new OrderDataHelper();
            LoadData();                 // Загрузка данных
            InitializeControls();        // Инициализация элементов
            ApplyFilters();              // Применение фильтров
            SetupManagerControls();      // Настройка прав доступа
        }

        // Настройка видимости кнопок в зависимости от роли
        private void SetupManagerControls()
        {
            bool isManager = currentUserRole == "Менеджер";

            // Менеджер может редактировать, администратор - только просмотр
            btnAdd.Visible = isManager;
            btnEdit.Visible = isManager;
            btnDelete.Visible = isManager;
            btnPrintReceipt.Visible = isManager; // Чек могут создавать только менеджеры
            btnSaveReport.Visible = isManager;

            if (!isManager)
            {
                this.Text = "Просмотр заказов (Администратор)";
            }
        }

        // Загрузка данных из БД
        private void LoadData()
        {
            try
            {
                originalData = dataHelper.GetOrdersData();
                filteredData = originalData.Copy();
                dataGridView.DataSource = filteredData.DefaultView;
                FillFilters();              // Заполнение фильтров
                FillSortComboBoxes();        // Заполнение списков сортировки
                UpdateResultsCount();        // Обновление счетчика записей
                FormatDataGridView();        // Форматирование таблицы
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Заполнение выпадающих списков для фильтрации
        private void FillFilters()
        {
            // Уникальные статусы для фильтра
            var statuses = originalData.AsEnumerable()
                .Select(row => row.Field<string>("StatusName"))
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            cmbStatusFilter.Items.Clear();
            cmbStatusFilter.Items.Add("Все статусы");
            cmbStatusFilter.Items.AddRange(statuses.ToArray());
            cmbStatusFilter.SelectedIndex = 0;

            // Уникальные категории для фильтра
            var categories = originalData.AsEnumerable()
                .Select(row => row.Field<string>("CategoryName"))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            cmbCategoryFilter.Items.Clear();
            cmbCategoryFilter.Items.Add("Все категории");
            cmbCategoryFilter.Items.AddRange(categories.ToArray());
            cmbCategoryFilter.SelectedIndex = 0;
        }

        // Заполнение списков сортировки
        private void FillSortComboBoxes()
        {
            cmbSortBy.Items.Clear();
            cmbSortBy.Items.AddRange(new string[] {
                "Дата заказа", "Дата начала", "Дата окончания", "Стоимость",
                "Клиент", "Лодка", "Категория", "Статус"
            });
            cmbSortBy.SelectedIndex = 0;

            cmbSortOrder.Items.Clear();
            cmbSortOrder.Items.AddRange(new string[] { "По возрастанию", "По убыванию" });
            cmbSortOrder.SelectedIndex = 0;
        }

        private void InitializeControls()
        {
            dataGridView.AutoGenerateColumns = true;
        }

        // Обработчик изменения фильтра
        private void OnFilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        // Обработчик изменения сортировки
        private void OnSortChanged(object sender, EventArgs e)
        {
            ApplySorting();
        }

        // Применение всех фильтров
        private void ApplyFilters()
        {
            if (originalData == null) return;

            filteredData = originalData.Copy();

            // Живой поиск по тексту
            string searchText = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                var filteredRows = filteredData.AsEnumerable()
                    .Where(row =>
                        row.Field<int>("ID").ToString().StartsWith(searchText) ||
                        row.Field<string>("ClientName").ToLower().Contains(searchText) ||
                        row.Field<string>("BoatName").ToLower().Contains(searchText) ||
                        row.Field<string>("CategoryName").ToLower().Contains(searchText) ||
                        row.Field<string>("StatusName").ToLower().Contains(searchText) ||
                        row.Field<decimal>("TotalPrice").ToString().Contains(searchText) ||
                        row.Field<DateTime>("OrderDate").ToString().Contains(searchText));

                if (filteredRows.Any())
                {
                    filteredData = filteredRows.CopyToDataTable();
                }
                else
                {
                    filteredData.Clear();
                }
            }

            // Фильтр по статусу
            if (cmbStatusFilter.SelectedIndex > 0)
            {
                string selectedStatus = cmbStatusFilter.SelectedItem.ToString();
                var filteredRows = filteredData.AsEnumerable()
                    .Where(row => row.Field<string>("StatusName") == selectedStatus);

                if (filteredRows.Any())
                {
                    filteredData = filteredRows.CopyToDataTable();
                }
                else
                {
                    filteredData.Clear();
                }
            }

            // Фильтр по категории
            if (cmbCategoryFilter.SelectedIndex > 0)
            {
                string selectedCategory = cmbCategoryFilter.SelectedItem.ToString();
                var filteredRows = filteredData.AsEnumerable()
                    .Where(row => row.Field<string>("CategoryName") == selectedCategory);

                if (filteredRows.Any())
                {
                    filteredData = filteredRows.CopyToDataTable();
                }
                else
                {
                    filteredData.Clear();
                }
            }

            ApplySorting();              // Применяем сортировку
            UpdateResultsCount();        // Обновляем счетчик
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

            string sortExpression = GetSortExpression(sortBy, sortOrder);

            try
            {
                filteredData.DefaultView.Sort = sortExpression;
                dataGridView.DataSource = filteredData.DefaultView;
                FormatDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сортировки: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Получение выражения для сортировки
        private string GetSortExpression(string sortBy, string sortOrder)
        {
            string sortDirection = sortOrder == "По убыванию" ? "DESC" : "ASC";

            switch (sortBy)
            {
                case "Дата заказа": return $"OrderDate {sortDirection}";
                case "Дата начала": return $"StartDate {sortDirection}";
                case "Дата окончания": return $"EndDate {sortDirection}";
                case "Стоимость": return $"TotalPrice {sortDirection}";
                case "Клиент": return $"ClientName {sortDirection}";
                case "Лодка": return $"BoatName {sortDirection}";
                case "Категория": return $"CategoryName {sortDirection}";
                case "Статус": return $"StatusName {sortDirection}";
                default: return $"OrderDate {sortDirection}";
            }
        }

        // Форматирование отображения таблицы
        private void FormatDataGridView()
        {
            if (dataGridView.Columns.Count > 0)
            {
                // Настройка отображения дат
                if (dataGridView.Columns["OrderDate"] != null)
                {
                    dataGridView.Columns["OrderDate"].HeaderText = "Дата заказа";
                    dataGridView.Columns["OrderDate"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm";
                }

                if (dataGridView.Columns["StartDate"] != null)
                {
                    dataGridView.Columns["StartDate"].HeaderText = "Дата начала";
                    dataGridView.Columns["StartDate"].DefaultCellStyle.Format = "dd.MM.yyyy";
                }

                if (dataGridView.Columns["EndDate"] != null)
                {
                    dataGridView.Columns["EndDate"].HeaderText = "Дата окончания";
                    dataGridView.Columns["EndDate"].DefaultCellStyle.Format = "dd.MM.yyyy";
                }

                // Форматирование стоимости
                if (dataGridView.Columns["TotalPrice"] != null)
                {
                    dataGridView.Columns["TotalPrice"].HeaderText = "Стоимость (руб)";
                    dataGridView.Columns["TotalPrice"].DefaultCellStyle.Format = "N2";
                    dataGridView.Columns["TotalPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                // Русские заголовки
                if (dataGridView.Columns["ClientName"] != null)
                    dataGridView.Columns["ClientName"].HeaderText = "Клиент";

                if (dataGridView.Columns["BoatName"] != null)
                    dataGridView.Columns["BoatName"].HeaderText = "Лодка";

                if (dataGridView.Columns["CategoryName"] != null)
                    dataGridView.Columns["CategoryName"].HeaderText = "Категория";

                if (dataGridView.Columns["StatusName"] != null)
                    dataGridView.Columns["StatusName"].HeaderText = "Статус";

                if (dataGridView.Columns["UserName"] != null)
                    dataGridView.Columns["UserName"].HeaderText = "Менеджер";

                dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dataGridView.RowHeadersVisible = false;
                dataGridView.AllowUserToResizeColumns = false;
                dataGridView.AllowUserToResizeRows = false;

                // Отключаем стандартную сортировку
                foreach (DataGridViewColumn column in dataGridView.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
        }

        // Обновление счетчика записей
        private void UpdateResultsCount()
        {
            int totalCount = originalData?.Rows.Count ?? 0;
            int filteredCount = filteredData?.Rows.Count ?? 0;

            if (totalCount == filteredCount)
            {
                lblResults.Text = $"Всего заказов: {totalCount}";
            }
            else
            {
                lblResults.Text = $"Показано: {filteredCount} из {totalCount}";
            }
        }

        // Сброс всех фильтров
        private void BtnReset_Click(object sender, EventArgs e)
        {
            ResetAllFilters();
        }

        private void ResetAllFilters()
        {
            txtSearch.Text = "";
            cmbStatusFilter.SelectedIndex = 0;
            cmbCategoryFilter.SelectedIndex = 0;
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

        // Добавление заказа
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            AddOrderForm addForm = new AddOrderForm();
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadData(); // Перезагружаем данные после добавления
                MessageBox.Show("Заказ успешно добавлен", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Редактирование заказа
        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                int orderId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["ID"].Value);
                EditOrderForm editForm = new EditOrderForm(orderId);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadData(); // Перезагружаем данные после изменения
                    MessageBox.Show("Заказ успешно изменен", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите заказ для редактирования", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Удаление заказа
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                // Проверка статуса - нельзя удалить "Новый" заказ
                string orderStatus = dataGridView.SelectedRows[0].Cells["StatusName"].Value?.ToString() ?? "";

                if (orderStatus.Equals("Новый", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Нельзя удалить заказ со статусом 'Новый'", "Запрет удаления",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult result = MessageBox.Show("Вы уверены, что хотите удалить выбранный заказ?",
                    "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        int orderId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["ID"].Value);

                        using (MySqlConnection connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = "DELETE FROM Orders WHERE ID = @ID";
                            MySqlCommand command = new MySqlCommand(query, connection);
                            command.Parameters.AddWithValue("@ID", orderId);

                            command.ExecuteNonQuery();
                            LoadData(); // Перезагружаем данные после удаления
                            MessageBox.Show("Заказ успешно удален", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении заказа: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите заказ для удаления", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ИЗМЕНЕНО: Печать чека для любого заказа (без проверки статуса)
        private void BtnPrintReceipt_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                int orderId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["ID"].Value);
                string orderStatus = dataGridView.SelectedRows[0].Cells["StatusName"].Value?.ToString() ?? "Неизвестно";

                // Убираем проверку на статус "Завершен" - разрешаем для всех заказов
                CreateAndShowReceipt(orderId, orderStatus);
            }
            else
            {
                MessageBox.Show("Выберите заказ для формирования чека", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ИЗМЕНЕНО: Создание и показ чека (теперь принимает статус заказа)
        private void CreateAndShowReceipt(int orderId, string orderStatus)
        {
            try
            {
                string receiptText = GenerateReceiptText(orderId);

                // Сохраняем во временный файл с указанием статуса в имени
                string statusForFileName = orderStatus.Replace(" ", "_");
                string tempFile = Path.Combine(Path.GetTempPath(),
                    $"Чек_заказ_{orderId}_{statusForFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                File.WriteAllText(tempFile, receiptText, System.Text.Encoding.UTF8);

                // Открываем в блокноте
                Process.Start("notepad.exe", tempFile);

                MessageBox.Show($"Чек для заказа №{orderId} (статус: {orderStatus}) сформирован и открыт в блокноте",
                    "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании чека: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ИЗМЕНЕНО: Генерация текста чека с пометкой о статусе
        private string GenerateReceiptText(int orderId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    o.ID as OrderId,
                    o.OrderDate,
                    o.StartDate,
                    o.EndDate,
                    o.TotalPrice,
                    c.ClientName,
                    c.Phone,
                    c.Email,
                    b.Nam as BoatName,
                    bc.CategoryName,
                    b.Price as DailyPrice,
                    os.StatusName,
                    u.FullName as ManagerName
                FROM Orders o
                INNER JOIN Clients c ON o.ClientID = c.ID
                INNER JOIN Boat b ON o.BoatID = b.ID
                INNER JOIN BoatCategories bc ON b.CategoryID = bc.ID
                INNER JOIN OrderStatuses os ON o.StatusID = os.ID
                INNER JOIN Users u ON o.UserID = u.ID
                WHERE o.ID = @OrderId";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@OrderId", orderId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int days = (reader.GetDateTime("EndDate") - reader.GetDateTime("StartDate")).Days + 1;
                            string status = reader.GetString("StatusName");

                            // Формирование текста чека
                            string receipt = "========================================\n";
                            receipt += "           ЧЕК НА АРЕНДУ ЛОДКИ\n";
                            receipt += "========================================\n";
                            receipt += $"Номер заказа: {reader.GetInt32("OrderId")}\n";
                            receipt += $"Дата оформления: {reader.GetDateTime("OrderDate"):dd.MM.yyyy HH:mm}\n";
                            receipt += "----------------------------------------\n";
                            receipt += "Клиент:\n";
                            receipt += $"  {reader.GetString("ClientName")}\n";
                            receipt += $"  Телефон: {reader.GetString("Phone")}\n";
                            if (!reader.IsDBNull(reader.GetOrdinal("Email")))
                                receipt += $"  Email: {reader.GetString("Email")}\n";
                            receipt += "----------------------------------------\n";
                            receipt += "Услуга:\n";
                            receipt += $"  {reader.GetString("BoatName")}\n";
                            receipt += $"  Категория: {reader.GetString("CategoryName")}\n";
                            receipt += $"  Стоимость в день: {reader.GetDecimal("DailyPrice"):N2} руб.\n";
                            receipt += "----------------------------------------\n";
                            receipt += "Период аренды:\n";
                            receipt += $"  С: {reader.GetDateTime("StartDate"):dd.MM.yyyy}\n";
                            receipt += $"  По: {reader.GetDateTime("EndDate"):dd.MM.yyyy}\n";
                            receipt += $"  Количество дней: {days}\n";
                            receipt += "----------------------------------------\n";
                            receipt += "ИТОГО:\n";
                            receipt += $"  Общая стоимость: {reader.GetDecimal("TotalPrice"):N2} руб.\n";
                            receipt += "----------------------------------------\n";
                            receipt += $"Менеджер: {reader.GetString("ManagerName")}\n";
                            receipt += "========================================\n";

                            receipt += $"Чек сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}\n";

                            return receipt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании чека: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return "Ошибка при формировании чека";
        }

        // Создание RTF документа (не используется)
        private string CreateRtfDocument(string text)
        {
            string rtfHeader = @"{\rtf1\ansi\deff0 {\fonttbl {\f0 Times New Roman;}} \f0\fs24 ";
            string rtfFooter = "}";
            string rtfContent = text.Replace("\n", "\\par ");
            return rtfHeader + rtfContent + rtfFooter;
        }

        // Сохранение отчета в Excel
        private void BtnSaveReport_Click(object sender, EventArgs e)
        {
            SaveOrdersReport();
        }

        // Создание Excel отчета
        private void SaveOrdersReport()
        {
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;
            Excel.Worksheet worksheet = null;

            try
            {
                // Создаем Excel приложение
                excelApp = new Excel.Application();
                excelApp.Visible = false;
                excelApp.DisplayAlerts = false;

                // Создаем книгу и лист
                workbook = excelApp.Workbooks.Add();
                worksheet = (Excel.Worksheet)workbook.Worksheets[1];
                worksheet.Name = "Заказы";

                // Заголовок отчета
                Excel.Range titleRange = worksheet.Range["A1:J1"];
                titleRange.Merge();
                titleRange.Value = "ОТЧЕТ ПО ЗАКАЗАМ";
                titleRange.Font.Size = 18;
                titleRange.Font.Bold = true;
                titleRange.Font.Name = "Arial";
                titleRange.Font.Color = Color.FromArgb(0, 102, 204);
                titleRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                titleRange.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
                titleRange.RowHeight = 35;

                // Дата формирования
                Excel.Range dateRange = worksheet.Range["A2:J2"];
                dateRange.Merge();
                dateRange.Value = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                dateRange.Font.Size = 11;
                dateRange.Font.Italic = true;
                dateRange.Font.Name = "Arial";
                dateRange.Font.Color = Color.Gray;
                dateRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                dateRange.RowHeight = 20;

                // Заголовки таблицы
                int headerRow = 4;
                string[] headers = {
            "ID", "Дата заказа", "Дата начала", "Дата окончания",
            "Стоимость (руб)", "Клиент", "Лодка", "Категория", "Статус", "Менеджер"
        };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[headerRow, i + 1] = headers[i];
                }

                // Форматирование заголовков
                Excel.Range headerRange = worksheet.Range[$"A{headerRow}:J{headerRow}"];
                headerRange.Font.Bold = true;
                headerRange.Font.Size = 11;
                headerRange.Font.Name = "Arial";
                headerRange.Font.Color = Color.White;
                headerRange.Interior.Color = Color.FromArgb(79, 129, 189);
                headerRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                headerRange.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
                headerRange.RowHeight = 25;
                headerRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                headerRange.Borders.Weight = Excel.XlBorderWeight.xlThin;

                // Переменные для подсчета статистики
                decimal totalNewSum = 0;
                decimal totalCompletedSum = 0;
                decimal totalCancelledSum = 0;
                int newCount = 0;
                int completedCount = 0;
                int cancelledCount = 0;

                // Заполнение данными из таблицы
                int currentRow = headerRow + 1;

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.IsNewRow) continue;

                    // ID
                    worksheet.Cells[currentRow, 1] = Convert.ToInt32(row.Cells["ID"].Value);

                    // Даты
                    object orderDate = row.Cells["OrderDate"].Value;
                    if (orderDate != null && orderDate != DBNull.Value)
                    {
                        if (orderDate is DateTime dt)
                        {
                            worksheet.Cells[currentRow, 2] = dt.ToString("dd.MM.yyyy HH:mm");
                        }
                        else
                        {
                            worksheet.Cells[currentRow, 2] = orderDate.ToString();
                        }
                    }

                    // Дата начала
                    object startDate = row.Cells["StartDate"].Value;
                    if (startDate != null && startDate != DBNull.Value)
                    {
                        if (startDate is DateTime dt)
                        {
                            worksheet.Cells[currentRow, 3] = dt.ToString("dd.MM.yyyy");
                        }
                        else
                        {
                            worksheet.Cells[currentRow, 3] = startDate.ToString();
                        }
                    }

                    // Дата окончания
                    object endDate = row.Cells["EndDate"].Value;
                    if (endDate != null && endDate != DBNull.Value)
                    {
                        if (endDate is DateTime dt)
                        {
                            worksheet.Cells[currentRow, 4] = dt.ToString("dd.MM.yyyy");
                        }
                        else
                        {
                            worksheet.Cells[currentRow, 4] = endDate.ToString();
                        }
                    }

                    // Стоимость
                    object priceValue = row.Cells["TotalPrice"].Value;
                    decimal price = 0;
                    if (priceValue != null && priceValue != DBNull.Value && !string.IsNullOrEmpty(priceValue.ToString()))
                    {
                        if (decimal.TryParse(priceValue.ToString(), out price) && price > 0)
                        {
                            worksheet.Cells[currentRow, 5] = price.ToString("N2");
                        }
                        else
                        {
                            worksheet.Cells[currentRow, 5] = priceValue.ToString();
                        }
                    }
                    else
                    {
                        worksheet.Cells[currentRow, 5] = "";
                    }

                    // Клиент, лодка, категория
                    worksheet.Cells[currentRow, 6] = row.Cells["ClientName"].Value?.ToString() ?? "";
                    worksheet.Cells[currentRow, 7] = row.Cells["BoatName"].Value?.ToString() ?? "";
                    worksheet.Cells[currentRow, 8] = row.Cells["CategoryName"].Value?.ToString() ?? "";

                    // Статус и подсчет статистики
                    string status = row.Cells["StatusName"].Value?.ToString() ?? "";
                    worksheet.Cells[currentRow, 9] = status;

                    if (!string.IsNullOrEmpty(status) && price > 0)
                    {
                        switch (status.ToLower())
                        {
                            case "новый":
                                totalNewSum += price;
                                newCount++;
                                break;
                            case "завершен":
                                totalCompletedSum += price;
                                completedCount++;
                                break;
                            case "отменен":
                                totalCancelledSum += price;
                                cancelledCount++;
                                break;
                        }
                    }

                    // Менеджер
                    worksheet.Cells[currentRow, 10] = row.Cells["UserName"].Value?.ToString() ?? "";

                    // Строки без данных
                    if (string.IsNullOrEmpty(row.Cells["ClientName"].Value?.ToString()) &&
                        string.IsNullOrEmpty(row.Cells["BoatName"].Value?.ToString()))
                    {
                        Excel.Range emptyRowRange = worksheet.Range[$"A{currentRow}:J{currentRow}"];
                        emptyRowRange.Interior.Color = Color.LightGray;
                        emptyRowRange.Font.Color = Color.DarkGray;
                        emptyRowRange.Font.Italic = true;
                        worksheet.Cells[currentRow, 6] = "Нет данных";
                    }

                    currentRow++;
                }

                int lastDataRow = currentRow - 1;

                // Форматирование данных
                for (int row = headerRow + 1; row <= lastDataRow; row++)
                {
                    // Выравнивание
                    worksheet.Range[$"A{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    worksheet.Range[$"B{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    worksheet.Range[$"C{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    worksheet.Range[$"D{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    worksheet.Range[$"E{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                    worksheet.Range[$"I{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                    // Цвет статуса
                    string status = worksheet.Range[$"I{row}"].Text;
                    if (!string.IsNullOrEmpty(status))
                    {
                        Excel.Range statusCell = worksheet.Range[$"I{row}"];
                        switch (status.ToLower())
                        {
                            case "новый":
                                statusCell.Font.Color = Color.Blue;
                                statusCell.Font.Bold = true;
                                break;
                            case "завершен":
                                statusCell.Font.Color = Color.Green;
                                statusCell.Font.Bold = true;
                                break;
                            case "отменен":
                                statusCell.Font.Color = Color.Red;
                                statusCell.Font.Bold = true;
                                break;
                        }
                    }

                    // Чередование цветов строк
                    Excel.Range rowRange = worksheet.Rows[row];
                    if (rowRange.Interior.Color != Color.LightGray.ToArgb())
                    {
                        if (row % 2 == 0)
                        {
                            rowRange.Interior.Color = Color.FromArgb(240, 240, 240);
                        }
                    }
                }

                // Границы таблицы
                if (lastDataRow >= headerRow + 1)
                {
                    Excel.Range dataRange = worksheet.Range[$"A{headerRow + 1}:J{lastDataRow}"];
                    dataRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                    dataRange.Borders.Weight = Excel.XlBorderWeight.xlThin;
                }

                // Автоподбор ширины
                worksheet.Columns.AutoFit();
                if (worksheet.Columns[2].ColumnWidth < 18) worksheet.Columns[2].ColumnWidth = 18;
                if (worksheet.Columns[5].ColumnWidth < 15) worksheet.Columns[5].ColumnWidth = 15;
                if (worksheet.Columns[6].ColumnWidth < 25) worksheet.Columns[6].ColumnWidth = 25;
                if (worksheet.Columns[7].ColumnWidth < 20) worksheet.Columns[7].ColumnWidth = 20;

                // Статистика по статусам
                int statsRow = lastDataRow + 3;
                worksheet.Cells[statsRow, 1] = "СТАТИСТИКА ПО СТАТУСАМ:";
                worksheet.Range[$"A{statsRow}:J{statsRow}"].Merge();
                worksheet.Cells[statsRow, 1].Font.Bold = true;
                worksheet.Cells[statsRow, 1].Font.Size = 14;
                worksheet.Cells[statsRow, 1].Font.Color = Color.FromArgb(0, 102, 204);
                worksheet.Rows[statsRow].RowHeight = 25;

                statsRow++;

                // Заголовки статистики
                string[] statsHeaders = { "Статус", "Количество заказов", "Сумма (руб)", "Процент от общей суммы" };
                for (int i = 0; i < statsHeaders.Length; i++)
                {
                    worksheet.Cells[statsRow, i + 1] = statsHeaders[i];
                }

                Excel.Range statsHeaderRange = worksheet.Range[$"A{statsRow}:D{statsRow}"];
                statsHeaderRange.Font.Bold = true;
                statsHeaderRange.Font.Size = 11;
                statsHeaderRange.Font.Name = "Arial";
                statsHeaderRange.Font.Color = Color.White;
                statsHeaderRange.Interior.Color = Color.FromArgb(79, 129, 189);
                statsHeaderRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                statsHeaderRange.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
                statsHeaderRange.RowHeight = 22;
                statsHeaderRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                statsHeaderRange.Borders.Weight = Excel.XlBorderWeight.xlThin;

                statsRow++;

                // Общая сумма
                decimal totalAllSum = totalNewSum + totalCompletedSum + totalCancelledSum;

                // Добавление строк статистики
                AddStatusRow(worksheet, statsRow, "Новый", newCount, totalNewSum, totalAllSum);
                statsRow++;
                AddStatusRow(worksheet, statsRow, "Завершен", completedCount, totalCompletedSum, totalAllSum);
                statsRow++;
                AddStatusRow(worksheet, statsRow, "Отменен", cancelledCount, totalCancelledSum, totalAllSum);
                statsRow++;

                // Итоговая строка
                worksheet.Cells[statsRow, 1] = "ИТОГО:";
                worksheet.Cells[statsRow, 2] = (newCount + completedCount + cancelledCount).ToString();
                worksheet.Cells[statsRow, 3] = totalAllSum.ToString("N2");
                worksheet.Cells[statsRow, 4] = "100%";

                Excel.Range totalStatsRange = worksheet.Range[$"A{statsRow}:D{statsRow}"];
                totalStatsRange.Font.Bold = true;
                totalStatsRange.Interior.Color = Color.FromArgb(220, 220, 220);
                totalStatsRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                totalStatsRange.Borders.Weight = Excel.XlBorderWeight.xlThin;

                // Границы статистики
                Excel.Range statsDataRange = worksheet.Range[$"A{statsRow - 3}:D{statsRow}"];
                statsDataRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                statsDataRange.Borders.Weight = Excel.XlBorderWeight.xlThin;

                worksheet.Range[$"A{statsRow - 3}:A{statsRow}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
                worksheet.Range[$"B{statsRow - 3}:B{statsRow}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                worksheet.Range[$"C{statsRow - 3}:C{statsRow}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                worksheet.Range[$"D{statsRow - 3}:D{statsRow}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                // Автоподбор колонок статистики
                worksheet.Columns[1].AutoFit();
                worksheet.Columns[2].AutoFit();
                worksheet.Columns[3].AutoFit();
                worksheet.Columns[4].AutoFit();

                // Сохранение файла
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.FileName = $"Отчет_по_заказам_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                saveDialog.Filter = "Excel файлы (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*";
                saveDialog.DefaultExt = "xlsx";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    workbook.SaveAs(saveDialog.FileName);
                    workbook.Close(false);
                    excelApp.Quit();

                    MessageBox.Show($"Отчет успешно сохранен в файл:\n{saveDialog.FileName}",
                        "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Предложение открыть файл
                    DialogResult result = MessageBox.Show("Открыть созданный отчет?", "Вопрос",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            Process.Start(saveDialog.FileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Не удалось открыть файл: {ex.Message}", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении отчета: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Освобождение COM ресурсов
                if (worksheet != null) Marshal.ReleaseComObject(worksheet);
                if (workbook != null)
                {
                    try { workbook.Close(false); } catch { }
                    Marshal.ReleaseComObject(workbook);
                }
                if (excelApp != null)
                {
                    try { excelApp.Quit(); } catch { }
                    Marshal.ReleaseComObject(excelApp);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        // Добавление строки статистики по статусу
        private void AddStatusRow(Excel.Worksheet worksheet, int row, string statusName, int count, decimal sum, decimal totalSum)
        {
            worksheet.Cells[row, 1] = statusName;
            worksheet.Cells[row, 2] = count.ToString();
            worksheet.Cells[row, 3] = sum.ToString("N2");

            if (totalSum > 0)
            {
                double percent = (double)(sum / totalSum) * 100;
                worksheet.Cells[row, 4] = percent.ToString("F1") + "%";
            }
            else
            {
                worksheet.Cells[row, 4] = "0%";
            }

            // Цвет статуса
            Excel.Range statusCell = worksheet.Cells[row, 1];
            switch (statusName.ToLower())
            {
                case "новый":
                    statusCell.Font.Color = Color.Blue;
                    statusCell.Font.Bold = true;
                    break;
                case "завершен":
                    statusCell.Font.Color = Color.Green;
                    statusCell.Font.Bold = true;
                    break;
                case "отменен":
                    statusCell.Font.Color = Color.Red;
                    statusCell.Font.Bold = true;
                    break;
            }

            // Выравнивание
            worksheet.Cells[row, 2].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            worksheet.Cells[row, 3].HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
            worksheet.Cells[row, 4].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

            // Границы
            Excel.Range rowRange = worksheet.Range[$"A{row}:D{row}"];
            rowRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
            rowRange.Borders.Weight = Excel.XlBorderWeight.xlThin;

            // Чередование цветов
            if (row % 2 == 0)
            {
                rowRange.Interior.Color = Color.FromArgb(240, 240, 240);
            }
        }

        // Возврат в предыдущую форму
        private void BtnBack_Click(object sender, EventArgs e)
        {
            this.Close();
            if (currentUserRole == "Менеджер")
            {
                // Поиск существующей формы менеджера
                foreach (Form form in Application.OpenForms)
                {
                    if (form is ManagerForm managerForm)
                    {
                        managerForm.Show();
                        return;
                    }
                }

                // Создание новой формы менеджера
                ManagerForm mf = new ManagerForm(currentUserId);
                mf.Show();
            }
            else
            {
                // Поиск существующей формы администратора
                foreach (Form form in Application.OpenForms)
                {
                    if (form is AdminForm adminForm)
                    {
                        adminForm.Show();
                        return;
                    }
                }

                // Создание новой формы администратора
                AdminForm af = new AdminForm(currentUserId);
                af.Show();
            }
        }

        // Отключение сортировки при добавлении колонок
        private void dataGridView_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            dataGridView.Columns[e.Column.Index].SortMode = DataGridViewColumnSortMode.NotSortable;
        }
    }
}