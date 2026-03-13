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
    public partial class DirectorOrdersForm : Form
    {
        private int currentUserId;
        private DataTable originalData;
        private DataTable filteredData;
        private string connectionString = ConnectionString.GetConnectionString();

        public DirectorOrdersForm(int currentUserId)
        {
            InitializeComponent();
            this.currentUserId = currentUserId;
            LoadData();
            InitializeControls();
            ApplyFilters();
        }

        private void LoadData()
        {
            try
            {
                originalData = GetCompletedOrdersData();
                filteredData = originalData.Copy();
                dataGridView.DataSource = filteredData.DefaultView;
                FillFilters();
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

        // Загружаем только завершенные заказы
        private DataTable GetCompletedOrdersData()
        {
            DataTable dataTable = new DataTable();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT 
                        o.ID,
                        o.OrderDate,
                        o.StartDate,
                        o.EndDate,
                        o.TotalPrice,
                        c.ClientName,
                        b.Nam as BoatName,
                        bc.CategoryName,
                        os.StatusName,
                        u.FullName as ManagerName
                    FROM Orders o
                    INNER JOIN Clients c ON o.ClientID = c.ID
                    INNER JOIN Boat b ON o.BoatID = b.ID
                    INNER JOIN BoatCategories bc ON b.CategoryID = bc.ID
                    INNER JOIN OrderStatuses os ON o.StatusID = os.ID
                    INNER JOIN Users u ON o.UserID = u.ID
                    WHERE os.StatusName = 'Завершен' OR os.StatusName = 'Выполнен'
                    ORDER BY o.OrderDate DESC";

                MySqlCommand command = new MySqlCommand(query, connection);
                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                adapter.Fill(dataTable);
            }

            return dataTable;
        }

        private void FillFilters()
        {
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

        private void FillSortComboBoxes()
        {
            cmbSortBy.Items.Clear();
            cmbSortBy.Items.AddRange(new string[] {
                "Дата заказа", "Дата начала", "Дата окончания", "Стоимость",
                "Клиент", "Лодка", "Категория", "Менеджер"
            });
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
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void OnSortChanged(object sender, EventArgs e)
        {
            ApplySorting();
        }

        private void ApplyFilters()
        {
            if (originalData == null) return;

            filteredData = originalData.Copy();

            string searchText = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                var filteredRows = filteredData.AsEnumerable()
                    .Where(row =>
                        // Поиск по ID
                        row.Field<int>("ID").ToString().Contains(searchText) ||

                        // Поиск по дате заказа (в разных форматах)
                        IsDateMatch(row.Field<DateTime?>("OrderDate"), searchText) ||

                        // Поиск по дате начала
                        IsDateMatch(row.Field<DateTime?>("StartDate"), searchText) ||

                        // Поиск по дате окончания
                        IsDateMatch(row.Field<DateTime?>("EndDate"), searchText) ||

                        // Поиск по клиенту
                        row.Field<string>("ClientName").ToLower().Contains(searchText) ||

                        // Поиск по лодке
                        row.Field<string>("BoatName").ToLower().Contains(searchText) ||

                        // Поиск по категории
                        row.Field<string>("CategoryName").ToLower().Contains(searchText) ||

                        // Поиск по менеджеру
                        row.Field<string>("ManagerName").ToLower().Contains(searchText) ||

                        // Поиск по стоимости
                        row.Field<decimal>("TotalPrice").ToString().Contains(searchText) ||

                        // Поиск по статусу
                        row.Field<string>("StatusName").ToLower().Contains(searchText));

                if (filteredRows.Any())
                {
                    filteredData = filteredRows.CopyToDataTable();
                }
                else
                {
                    filteredData.Clear();
                }
            }

            if (cmbCategoryFilter.SelectedIndex > 0 && filteredData.Rows.Count > 0)
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

            ApplySorting();
            UpdateResultsCount();
        }

        private bool IsDateMatch(DateTime? dateValue, string searchText)
        {
            if (dateValue == null) return false;

            DateTime date = dateValue.Value;

            return date.ToString("dd.MM.yyyy").Contains(searchText) ||         
                   date.ToString("dd.MM.yy").Contains(searchText) ||       
                   date.ToString("yyyy-MM-dd").Contains(searchText) ||       
                   date.ToString("dd MMMM yyyy").ToLower().Contains(searchText) || 
                   date.ToString("MMMM yyyy").ToLower().Contains(searchText) ||  
                   date.ToString("yyyy").Contains(searchText) ||                  
                   date.ToString("MM").Contains(searchText) ||                 
                   date.ToString("dd").Contains(searchText) ||                    
                   date.ToString("dd MMMM").ToLower().Contains(searchText) ||    
                   date.ToString("MMMM").ToLower().Contains(searchText);        
        }

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
                case "Менеджер": return $"ManagerName {sortDirection}";
                default: return $"OrderDate {sortDirection}";
            }
        }

        private void FormatDataGridView()
        {
            if (dataGridView.Columns.Count > 0)
            {
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

                if (dataGridView.Columns["TotalPrice"] != null)
                {
                    dataGridView.Columns["TotalPrice"].HeaderText = "Стоимость (руб)";
                    dataGridView.Columns["TotalPrice"].DefaultCellStyle.Format = "N2";
                    dataGridView.Columns["TotalPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dataGridView.Columns["TotalPrice"].DefaultCellStyle.ForeColor = Color.Green;
                }

                if (dataGridView.Columns["ClientName"] != null)
                    dataGridView.Columns["ClientName"].HeaderText = "Клиент";

                if (dataGridView.Columns["BoatName"] != null)
                    dataGridView.Columns["BoatName"].HeaderText = "Лодка";

                if (dataGridView.Columns["CategoryName"] != null)
                    dataGridView.Columns["CategoryName"].HeaderText = "Категория";

                if (dataGridView.Columns["ManagerName"] != null)
                    dataGridView.Columns["ManagerName"].HeaderText = "Менеджер";

                if (dataGridView.Columns["StatusName"] != null)
                {
                    dataGridView.Columns["StatusName"].HeaderText = "Статус";
                    dataGridView.Columns["StatusName"].DefaultCellStyle.ForeColor = Color.Green;
                    dataGridView.Columns["StatusName"].DefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold);
                }

                dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dataGridView.RowHeadersVisible = false;
                dataGridView.AllowUserToResizeColumns = false;
                dataGridView.AllowUserToResizeRows = false;

                foreach (DataGridViewColumn column in dataGridView.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
        }

        private void UpdateResultsCount()
        {
            int totalCount = originalData?.Rows.Count ?? 0;
            int filteredCount = filteredData?.Rows.Count ?? 0;

            decimal totalRevenue = 0;
            if (filteredData != null && filteredData.Rows.Count > 0)
            {
                totalRevenue = filteredData.AsEnumerable()
                    .Sum(row => row.Field<decimal>("TotalPrice"));
            }

            if (totalCount == filteredCount)
            {
                lblResults.Text = $"Всего завершенных заказов: {totalCount} | Общая выручка: {totalRevenue:N2} руб.";
            }
            else
            {
                lblResults.Text = $"Показано: {filteredCount} из {totalCount} | Общая выручка: {totalRevenue:N2} руб.";
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            ResetAllFilters();
        }

        private void ResetAllFilters()
        {
            txtSearch.Text = "";
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

        private void BtnExportToExcel_Click(object sender, EventArgs e)
        {
            ExportCompletedOrdersReport();
        }

        private void ExportCompletedOrdersReport()
        {
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;
            Excel.Worksheet worksheet = null;

            try
            {
                // Создаем приложение Excel
                excelApp = new Excel.Application();
                excelApp.Visible = false;
                excelApp.DisplayAlerts = false;

                // Создаем новую книгу
                workbook = excelApp.Workbooks.Add();
                worksheet = (Excel.Worksheet)workbook.Worksheets[1];
                worksheet.Name = "Завершенные заказы";

                // Заголовок отчета
                Excel.Range titleRange = worksheet.Range["A1:J1"];
                titleRange.Merge();
                titleRange.Value = "ОТЧЕТ ПО ЗАВЕРШЕННЫМ ЗАКАЗАМ";
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
                decimal totalRevenue = 0;
                int totalOrders = 0;

                // Заполняем данными из DataGridView
                int currentRow = headerRow + 1;

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.IsNewRow) continue;

                    // ID
                    worksheet.Cells[currentRow, 1] = Convert.ToInt32(row.Cells["ID"].Value);

                    // Дата заказа
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
                    if (priceValue != null && priceValue != DBNull.Value)
                    {
                        if (decimal.TryParse(priceValue.ToString(), out price))
                        {
                            worksheet.Cells[currentRow, 5] = price;
                            totalRevenue += price;
                            totalOrders++;
                        }
                    }

                    // Клиент
                    worksheet.Cells[currentRow, 6] = row.Cells["ClientName"].Value?.ToString() ?? "";

                    // Лодка
                    worksheet.Cells[currentRow, 7] = row.Cells["BoatName"].Value?.ToString() ?? "";

                    // Категория
                    worksheet.Cells[currentRow, 8] = row.Cells["CategoryName"].Value?.ToString() ?? "";

                    // Статус (всегда завершен)
                    worksheet.Cells[currentRow, 9] = "Завершен";

                    // Менеджер
                    worksheet.Cells[currentRow, 10] = row.Cells["ManagerName"].Value?.ToString() ?? "";

                    currentRow++;
                }

                int lastDataRow = currentRow - 1;

                // ПРИМЕНЯЕМ ФОРМАТИРОВАНИЕ
                for (int row = headerRow + 1; row <= lastDataRow; row++)
                {
                    // Центрирование ID
                    worksheet.Range[$"A{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                    // Центрирование дат
                    worksheet.Range[$"B{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    worksheet.Range[$"C{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    worksheet.Range[$"D{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                    // Выравнивание стоимости вправо
                    worksheet.Range[$"E{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                    worksheet.Range[$"E{row}"].NumberFormat = "#,##0.00 ₽";

                    // Центрирование статуса
                    worksheet.Range[$"I{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    worksheet.Range[$"I{row}"].Font.Color = Color.Green;
                    worksheet.Range[$"I{row}"].Font.Bold = true;

                    // Центрирование менеджера
                    worksheet.Range[$"J{row}"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                    // Чередование цветов строк
                    if (row % 2 == 0)
                    {
                        Excel.Range rowRange = worksheet.Rows[row];
                        rowRange.Interior.Color = Color.FromArgb(240, 240, 240);
                    }
                }

                // Границы для всей таблицы
                if (lastDataRow >= headerRow + 1)
                {
                    Excel.Range dataRange = worksheet.Range[$"A{headerRow + 1}:J{lastDataRow}"];
                    dataRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                    dataRange.Borders.Weight = Excel.XlBorderWeight.xlThin;
                }

                // Автоподбор ширины колонок
                worksheet.Columns.AutoFit();

                // Устанавливаем минимальную ширину
                if (worksheet.Columns[2].ColumnWidth < 18) worksheet.Columns[2].ColumnWidth = 18;
                if (worksheet.Columns[5].ColumnWidth < 15) worksheet.Columns[5].ColumnWidth = 15;
                if (worksheet.Columns[6].ColumnWidth < 25) worksheet.Columns[6].ColumnWidth = 25;
                if (worksheet.Columns[7].ColumnWidth < 20) worksheet.Columns[7].ColumnWidth = 20;

                // ДОБАВЛЯЕМ ИТОГИ
                int summaryRow = lastDataRow + 3;

                // Заголовок итогов
                worksheet.Cells[summaryRow, 1] = "ИТОГОВАЯ СТАТИСТИКА:";
                worksheet.Range[$"A{summaryRow}:J{summaryRow}"].Merge();
                worksheet.Cells[summaryRow, 1].Font.Bold = true;
                worksheet.Cells[summaryRow, 1].Font.Size = 14;
                worksheet.Cells[summaryRow, 1].Font.Color = Color.FromArgb(0, 102, 204);
                worksheet.Rows[summaryRow].RowHeight = 25;

                summaryRow++;

                // Заголовки таблицы итогов
                string[] summaryHeaders = { "Показатель", "Значение" };
                for (int i = 0; i < summaryHeaders.Length; i++)
                {
                    worksheet.Cells[summaryRow, i + 1] = summaryHeaders[i];
                }

                // Форматирование заголовков итогов
                Excel.Range summaryHeaderRange = worksheet.Range[$"A{summaryRow}:B{summaryRow}"];
                summaryHeaderRange.Font.Bold = true;
                summaryHeaderRange.Font.Size = 11;
                summaryHeaderRange.Font.Name = "Arial";
                summaryHeaderRange.Font.Color = Color.White;
                summaryHeaderRange.Interior.Color = Color.FromArgb(79, 129, 189);
                summaryHeaderRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                summaryHeaderRange.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
                summaryHeaderRange.RowHeight = 22;
                summaryHeaderRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                summaryHeaderRange.Borders.Weight = Excel.XlBorderWeight.xlThin;

                summaryRow++;

                // Данные итогов
                AddSummaryRow(worksheet, summaryRow, "Количество завершенных заказов", totalOrders.ToString());
                summaryRow++;

                AddSummaryRow(worksheet, summaryRow, "Общая выручка", $"{totalRevenue:N2} руб.");
                summaryRow++;

                // Форматирование таблицы итогов
                Excel.Range summaryDataRange = worksheet.Range[$"A{summaryRow - (totalOrders > 0 ? 4 : 3)}:B{summaryRow - 1}"];
                summaryDataRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                summaryDataRange.Borders.Weight = Excel.XlBorderWeight.xlThin;

                worksheet.Columns[1].AutoFit();
                worksheet.Columns[2].AutoFit();

                // Сохраняем файл
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.FileName = $"Отчет_завершенные_заказы_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                saveDialog.Filter = "Excel файлы (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*";
                saveDialog.DefaultExt = "xlsx";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    workbook.SaveAs(saveDialog.FileName);

                    // Закрываем Excel
                    workbook.Close(false);
                    excelApp.Quit();

                    MessageBox.Show($"Отчет успешно сохранен в файл:\n{saveDialog.FileName}",
                        "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Спрашиваем, открыть ли файл
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
                // Освобождаем ресурсы COM
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

        private void AddSummaryRow(Excel.Worksheet worksheet, int row, string label, string value)
        {
            // Название показателя
            worksheet.Cells[row, 1] = label;

            // Значение
            worksheet.Cells[row, 2] = value;

            // Выравнивание
            worksheet.Cells[row, 1].HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
            worksheet.Cells[row, 2].HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
            worksheet.Cells[row, 2].Font.Bold = true;

            // Границы
            Excel.Range rowRange = worksheet.Range[$"A{row}:B{row}"];
            rowRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
            rowRange.Borders.Weight = Excel.XlBorderWeight.xlThin;

            // Чередование цветов
            if (row % 2 == 0)
            {
                rowRange.Interior.Color = Color.FromArgb(240, 240, 240);
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            this.Close();
            DirectorForm df = new DirectorForm(currentUserId);
            df.Show();
        }

        private void dataGridView_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            dataGridView.Columns[e.Column.Index].SortMode = DataGridViewColumnSortMode.NotSortable;
        }
    }
}