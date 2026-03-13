using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using MySql.Data.MySqlClient;
using System.Drawing.Imaging;
using System.Threading;
using Timer = System.Windows.Forms.Timer;

namespace Kursovaya
{
    public partial class ManagerServicesForm : Form
    {
        private int currentUserId;
        private DataTable originalData;
        private DataTable filteredData;
        private string connectionString = ConnectionString.GetConnectionString();

        // Путь к папке с изображениями в AppData
        private string imagesFolderPath;
        private string appDataFolderPath;

        public ManagerServicesForm(int currentUserId)
        {
            InitializeComponent();

            // Проверяем userId
            if (currentUserId <= 0)
            {
                MessageBox.Show("Ошибка: неверный идентификатор пользователя", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            this.currentUserId = currentUserId;

            InitializeImagePaths();
            SetupDataGridViewCompletely();

            // Добавляем задержку перед загрузкой данных
            this.Shown += ManagerServicesForm_Shown;
        }

        private void ManagerServicesForm_Shown(object sender, EventArgs e)
        {
            // Показываем сообщение о загрузке
            lblResults.Text = "Загрузка данных...";
            lblResults.ForeColor = Color.Blue;
            Cursor = Cursors.WaitCursor;

            // Создаем таймер для задержки
            Timer delayTimer = new Timer();
            delayTimer.Interval = 500; // Задержка 500 мс
            delayTimer.Tick += (s, ev) =>
            {
                delayTimer.Stop();
                delayTimer.Dispose();

                // Загружаем данные
                LoadData();
                ApplyFilters();

                Cursor = Cursors.Default;
                lblResults.ForeColor = SystemColors.ControlText;
            };
            delayTimer.Start();
        }

        private void SetupDataGridViewCompletely()
        {
            try
            {
                // 1. Настраиваем основные свойства
                dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGridView.ReadOnly = true;
                dataGridView.RowTemplate.Height = 130;
                dataGridView.AllowUserToAddRows = false;
                dataGridView.AllowUserToDeleteRows = false;
                dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                dataGridView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

                // ВАЖНО: Настройка автоматического растягивания колонок
                dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

                // 2. Устанавливаем ДО загрузки данных
                dataGridView.AutoGenerateColumns = false;

                // 3. Очищаем все существующие колонки
                dataGridView.Columns.Clear();

                // 4. Создаем колонки ВРУЧНУЮ
                CreateAllColumns();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка настройки таблицы: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateAllColumns()
        {
            // Колонка 1: ID (скрытая)
            DataGridViewTextBoxColumn colId = new DataGridViewTextBoxColumn();
            colId.Name = "ID";
            colId.DataPropertyName = "ID";
            colId.HeaderText = "ID";
            colId.Visible = false;
            colId.FillWeight = 1; // Минимальное значение 1
            dataGridView.Columns.Add(colId);

            // Колонка 2: Изображение
            DataGridViewImageColumn colImage = new DataGridViewImageColumn();
            colImage.Name = "ImagePreview";
            colImage.HeaderText = "Изображение";

            // ВАЖНО: Исправление для отображения изображений
            colImage.ImageLayout = DataGridViewImageCellLayout.Zoom;
            colImage.Width = 180;
            colImage.FillWeight = 15; // Вес колонки при растягивании (15%)

            // Важно: устанавливаем свойства для правильного отображения
            colImage.DefaultCellStyle.NullValue = null;
            colImage.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colImage.DefaultCellStyle.Padding = new Padding(2);

            dataGridView.Columns.Add(colImage);

            // Колонка 3: Название
            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn();
            colName.Name = "BoatName";
            colName.DataPropertyName = "BoatName";
            colName.HeaderText = "Название";
            colName.FillWeight = 20; // 20%
            colName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colName.DefaultCellStyle.WrapMode = DataGridViewTriState.True; // Перенос текста
            dataGridView.Columns.Add(colName);

            // Колонка 4: Класс
            DataGridViewTextBoxColumn colClass = new DataGridViewTextBoxColumn();
            colClass.Name = "Class";
            colClass.DataPropertyName = "Class";
            colClass.HeaderText = "Класс";
            colClass.FillWeight = 10; // 10%
            colClass.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridView.Columns.Add(colClass);

            // Колонка 5: Цена
            DataGridViewTextBoxColumn colPrice = new DataGridViewTextBoxColumn();
            colPrice.Name = "Price";
            colPrice.DataPropertyName = "Price";
            colPrice.HeaderText = "Стоимость (руб/день)";
            colPrice.FillWeight = 15; // 15%
            colPrice.DefaultCellStyle.Format = "N2";
            colPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView.Columns.Add(colPrice);

            // Колонка 6: Описание
            DataGridViewTextBoxColumn colDesc = new DataGridViewTextBoxColumn();
            colDesc.Name = "Description";
            colDesc.DataPropertyName = "Description";
            colDesc.HeaderText = "Описание";
            colDesc.FillWeight = 39; // 39% (оставшийся процент)
            colDesc.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colDesc.DefaultCellStyle.WrapMode = DataGridViewTriState.True; // Перенос текста
            dataGridView.Columns.Add(colDesc);

            // Колонка 7: Путь к изображению (имя файла) - скрытая
            DataGridViewTextBoxColumn colImagePath = new DataGridViewTextBoxColumn();
            colImagePath.Name = "ImagePath";
            colImagePath.DataPropertyName = "ImagePath";
            colImagePath.HeaderText = "ImagePath";
            colImagePath.Visible = false;
            colImagePath.FillWeight = 1; // Минимальное значение для скрытой колонки
            dataGridView.Columns.Add(colImagePath);

            // Отключаем сортировку для всех колонок
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        private void InitializeImagePaths()
        {
            try
            {
                string appName = "BoatRentalSystem";
                appDataFolderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    appName);

                imagesFolderPath = Path.Combine(appDataFolderPath, "BoatImages");

                if (!Directory.Exists(appDataFolderPath))
                    Directory.CreateDirectory(appDataFolderPath);

                if (!Directory.Exists(imagesFolderPath))
                    Directory.CreateDirectory(imagesFolderPath);

                // Для отладки - показываем путь к папке с изображениями
                System.Diagnostics.Debug.WriteLine($"Путь к изображениям: {imagesFolderPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания папок для изображений: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string GetFullImagePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            // Если fileName уже содержит полный путь (для обратной совместимости)
            if (fileName.Contains(":\\") && File.Exists(fileName))
            {
                return fileName;
            }

            // Иначе формируем путь к папке в AppData
            return Path.Combine(imagesFolderPath, fileName);
        }

        private void LoadData()
        {
            try
            {
                originalData = GetServicesData();

                if (originalData == null || originalData.Rows.Count == 0)
                {
                    MessageBox.Show("Нет данных об услугах", "Информация",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    filteredData = originalData?.Clone() ?? new DataTable();
                    dataGridView.DataSource = null;
                    return;
                }

                filteredData = originalData.Copy();

                // ВАЖНО: Используем BindingSource для лучшей работы с данными
                BindingSource bindingSource = new BindingSource();
                bindingSource.DataSource = filteredData;
                dataGridView.DataSource = bindingSource;

                // Заполняем изображения
                FillImageColumn();

                // Настраиваем внешний вид
                FormatDataGridViewAppearance();

                // Заполняем фильтры и сортировку
                FillFilters();
                FillSortComboBoxes();
                UpdateResultsCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable GetServicesData()
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            b.ID,
                            b.Nam as BoatName,
                            bc.CategoryName as Class,
                            b.Price,
                            b.Description,
                            b.ImagePath
                        FROM Boat b
                        INNER JOIN BoatCategories bc ON b.CategoryID = bc.ID
                        ORDER BY b.Nam";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    adapter.Fill(dataTable);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения данных из базы: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return dataTable;
        }

        private void FillImageColumn()
        {
            if (dataGridView.Rows.Count == 0) return;

            // Добавляем обработчик события для отображения изображений после отрисовки строк
            dataGridView.RowPostPaint += (sender, e) =>
            {
                // Этот обработчик гарантирует, что изображения будут отображаться после отрисовки строк
            };

            int imagesLoaded = 0;
            int imagesFailed = 0;

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                try
                {
                    if (row.Cells["ImagePath"]?.Value != null &&
                        !string.IsNullOrEmpty(row.Cells["ImagePath"].Value.ToString()))
                    {
                        string imageFileName = row.Cells["ImagePath"].Value.ToString();
                        string fullImagePath = GetFullImagePath(imageFileName);

                        System.Diagnostics.Debug.WriteLine($"Пытаемся загрузить: {fullImagePath}");

                        if (File.Exists(fullImagePath))
                        {
                            try
                            {
                                // ВАЖНО: Создаем изображение и сразу присваиваем ячейке
                                using (Image originalImage = Image.FromFile(fullImagePath))
                                {
                                    // Создаем копию изображения для отображения
                                    Image displayImage = new Bitmap(originalImage);
                                    row.Cells["ImagePreview"].Value = displayImage;
                                    imagesLoaded++;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                                row.Cells["ImagePreview"].Value = GetDefaultNoImage();
                                imagesFailed++;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Файл не найден: {fullImagePath}");
                            row.Cells["ImagePreview"].Value = GetDefaultNoImage();
                            imagesFailed++;
                        }
                    }
                    else
                    {
                        row.Cells["ImagePreview"].Value = GetDefaultNoImage();
                    }

                    row.Height = 130; // Устанавливаем высоту строки после добавления изображения
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Общая ошибка: {ex.Message}");
                    row.Cells["ImagePreview"].Value = GetDefaultNoImage();
                    imagesFailed++;
                }

                // Небольшая задержка для предотвращения блокировки UI
                Application.DoEvents();
            }

            System.Diagnostics.Debug.WriteLine($"Загружено: {imagesLoaded}, не загружено: {imagesFailed}");

            // Принудительно обновляем отображение
            dataGridView.Refresh();
        }

        private void FormatDataGridViewAppearance()
        {
            // Основные настройки уже установлены в SetupDataGridViewCompletely

            dataGridView.RowHeadersVisible = false;

            // Настройка выделения
            dataGridView.DefaultCellStyle.SelectionBackColor = Color.LightBlue;
            dataGridView.DefaultCellStyle.SelectionForeColor = Color.Black;

            // Чередование цветов строк
            dataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.WhiteSmoke;

            // Выравнивание заголовков колонок
            dataGridView.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Bold);

            // Настройка границ
            dataGridView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView.GridColor = Color.LightGray;

            // Разрешаем пользователю изменять ширину колонок
            dataGridView.AllowUserToResizeColumns = true;

            // Автоматическая высота строк для текста с переносом
            dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // Дополнительные настройки для колонок
            if (dataGridView.Columns.Contains("Description"))
            {
                dataGridView.Columns["Description"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }

            if (dataGridView.Columns.Contains("BoatName"))
            {
                dataGridView.Columns["BoatName"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }
        }

        private Image GetDefaultNoImage()
        {
            try
            {
                Bitmap bitmap = new Bitmap(180, 120);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.LightGray);

                    // Центрируем текст "Нет изображения"
                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Center;
                    stringFormat.LineAlignment = StringAlignment.Center;

                    g.DrawString("Нет\nизображения",
                        new Font("Arial", 12, FontStyle.Bold),
                        Brushes.Black,
                        new RectangleF(0, 0, 180, 120),
                        stringFormat);
                }
                return bitmap;
            }
            catch
            {
                return new Bitmap(180, 120);
            }
        }

        // Вспомогательный метод для обновления изображений при изменении размера формы
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (dataGridView != null && dataGridView.Rows.Count > 0)
            {
                // Обновляем отображение при изменении размера формы
                dataGridView.Refresh();
            }
        }

        // Остальные методы остаются без изменений
        private void FillFilters()
        {
            if (originalData == null || originalData.Rows.Count == 0) return;

            try
            {
                var classes = originalData.AsEnumerable()
                    .Select(row => row.Field<string>("Class"))
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                cmbClassFilter.Items.Clear();
                cmbClassFilter.Items.Add("Все классы");
                cmbClassFilter.Items.AddRange(classes.ToArray());
                cmbClassFilter.SelectedIndex = 0;

                cmbPriceFilter.Items.Clear();
                cmbPriceFilter.Items.AddRange(new string[] {
                    "Все цены",
                    "До 4000 руб",
                    "4000-8000 руб",
                    "8000-12000 руб",
                    "12000-18000 руб",
                    "Выше 18000 руб"
                });
                cmbPriceFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка заполнения фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void FillSortComboBoxes()
        {
            try
            {
                cmbSortBy.Items.Clear();
                cmbSortBy.Items.AddRange(new string[] {
                    "Название", "Класс", "Стоимость"
                });
                cmbSortBy.SelectedIndex = 0;

                cmbSortOrder.Items.Clear();
                cmbSortOrder.Items.AddRange(new string[] { "По возрастанию", "По убыванию" });
                cmbSortOrder.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка заполнения сортировки: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplyFilters()
        {
            if (originalData == null) return;

            try
            {
                filteredData = originalData.Copy();

                string searchText = txtSearch.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(searchText))
                {
                    var filteredRows = filteredData.AsEnumerable()
                        .Where(row => row.Field<string>("BoatName").ToLower().Contains(searchText));

                    if (filteredRows.Any())
                    {
                        filteredData = filteredRows.CopyToDataTable();
                    }
                    else
                    {
                        filteredData.Clear();
                    }
                }

                if (cmbClassFilter.SelectedIndex > 0)
                {
                    string selectedClass = cmbClassFilter.SelectedItem.ToString();
                    var filteredRows = filteredData.AsEnumerable()
                        .Where(row => row.Field<string>("Class") == selectedClass);

                    if (filteredRows.Any())
                    {
                        filteredData = filteredRows.CopyToDataTable();
                    }
                    else
                    {
                        filteredData.Clear();
                    }
                }

                if (cmbPriceFilter.SelectedIndex > 0)
                {
                    string priceFilter = cmbPriceFilter.SelectedItem.ToString();
                    var filteredRows = filteredData.AsEnumerable();

                    switch (priceFilter)
                    {
                        case "До 4000 руб":
                            filteredRows = filteredRows.Where(row => row.Field<decimal>("Price") < 4000);
                            break;
                        case "4000-8000 руб":
                            filteredRows = filteredRows.Where(row => row.Field<decimal>("Price") >= 4000 && row.Field<decimal>("Price") <= 8000);
                            break;
                        case "8000-12000 руб":
                            filteredRows = filteredRows.Where(row => row.Field<decimal>("Price") > 8000 && row.Field<decimal>("Price") <= 12000);
                            break;
                        case "12000-18000 руб":
                            filteredRows = filteredRows.Where(row => row.Field<decimal>("Price") > 12000 && row.Field<decimal>("Price") <= 18000);
                            break;
                        case "Выше 18000 руб":
                            filteredRows = filteredRows.Where(row => row.Field<decimal>("Price") > 18000);
                            break;
                    }

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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка применения фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplySorting()
        {
            if (filteredData == null || filteredData.Rows.Count == 0)
            {
                dataGridView.DataSource = null;
                return;
            }

            try
            {
                string sortBy = cmbSortBy.SelectedItem?.ToString();
                string sortOrder = cmbSortOrder.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(sortBy)) return;

                string sortExpression = GetSortExpression(sortBy, sortOrder);
                filteredData.DefaultView.Sort = sortExpression;

                // Обновляем DataSource
                if (dataGridView.DataSource is BindingSource bs)
                {
                    bs.DataSource = filteredData;
                }
                else
                {
                    dataGridView.DataSource = filteredData.DefaultView;
                }

                FillImageColumn();
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
                case "Название": return $"BoatName {sortDirection}";
                case "Класс": return $"Class {sortDirection}";
                case "Стоимость": return $"Price {sortDirection}";
                default: return $"BoatName {sortDirection}";
            }
        }

        private void UpdateResultsCount()
        {
            try
            {
                int totalCount = originalData?.Rows.Count ?? 0;
                int filteredCount = filteredData?.Rows.Count ?? 0;

                if (totalCount == filteredCount)
                {
                    lblResults.Text = $"Всего услуг: {totalCount}";
                }
                else
                {
                    lblResults.Text = $"Показано: {filteredCount} из {totalCount}";
                }
            }
            catch
            {
                lblResults.Text = "Ошибка подсчета";
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                AddEditServiceForm addForm = new AddEditServiceForm(null);
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    LoadData();
                    MessageBox.Show("Услуга успешно добавлена", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении услуги: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView.SelectedRows.Count > 0)
                {
                    if (dataGridView.SelectedRows[0].Cells["ID"].Value != null)
                    {
                        int serviceId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["ID"].Value);
                        AddEditServiceForm editForm = new AddEditServiceForm(serviceId);
                        if (editForm.ShowDialog() == DialogResult.OK)
                        {
                            LoadData();
                            MessageBox.Show("Услуга успешно изменена", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Не удалось получить ID услуги", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Выберите услугу для редактирования", "Внимание",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании услуги: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView.SelectedRows.Count > 0)
                {
                    int serviceId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["ID"].Value);
                    string serviceName = dataGridView.SelectedRows[0].Cells["BoatName"].Value?.ToString() ?? "Неизвестная услуга";
                    string imageFileName = dataGridView.SelectedRows[0].Cells["ImagePath"].Value?.ToString();

                    if (!CheckServiceCanBeDeleted(serviceId))
                    {
                        return;
                    }

                    DialogResult result = MessageBox.Show($"Вы уверены, что хотите удалить услугу '{serviceName}'?\n\n" +
                        "Внимание: Это действие нельзя отменить.",
                        "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        using (MySqlConnection connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();
                            string deleteQuery = "DELETE FROM Boat WHERE ID = @ID";
                            MySqlCommand command = new MySqlCommand(deleteQuery, connection);
                            command.Parameters.AddWithValue("@ID", serviceId);

                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                DeleteImageFile(imageFileName);
                                LoadData();
                                MessageBox.Show("Услуга успешно удалена", "Успех",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Выберите услугу для удаления", "Внимание",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении услуги: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            try
            {
                ResetAllFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сбросе фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetAllFilters()
        {
            txtSearch.Text = "";
            cmbClassFilter.SelectedIndex = 0;
            cmbPriceFilter.SelectedIndex = 0;
            cmbSortBy.SelectedIndex = 0;
            cmbSortOrder.SelectedIndex = 0;

            if (originalData != null)
            {
                filteredData = originalData.Copy();
                if (dataGridView.DataSource is BindingSource bs)
                {
                    bs.DataSource = filteredData;
                }
                else
                {
                    dataGridView.DataSource = filteredData.DefaultView;
                }
                FillImageColumn();
                UpdateResultsCount();
            }
        }

        private bool CheckServiceCanBeDeleted(int serviceId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT COUNT(*) as ActiveOrders
                        FROM Orders o
                        INNER JOIN OrderStatuses os ON o.StatusID = os.ID
                        WHERE o.BoatID = @ServiceId 
                        AND os.StatusName = 'Новый'";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ServiceId", serviceId);

                    object result = command.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        int activeOrders = Convert.ToInt32(result);

                        if (activeOrders > 0)
                        {
                            MessageBox.Show($"Невозможно удалить услугу, так как она используется в {activeOrders} активных заказах.\n" +
                                "Сначала завершите или отмените эти заказы.", "Ошибка удаления",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке возможности удаления: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void DeleteImageFile(string imageFileName)
        {
            try
            {
                if (!string.IsNullOrEmpty(imageFileName))
                {
                    string fullPath = GetFullImagePath(imageFileName);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении файла изображения: {ex.Message}");
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            try
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

                ManagerForm newManagerForm = new ManagerForm(currentUserId);
                newManagerForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при возврате: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void cmbClassFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void cmbPriceFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void cmbSortBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySorting();
        }

        private void cmbSortOrder_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySorting();
        }

        private void DataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 &&
                dataGridView.Columns.Contains("ImagePreview") &&
                e.ColumnIndex == dataGridView.Columns["ImagePreview"].Index)
            {
                string imageFileName = dataGridView.Rows[e.RowIndex].Cells["ImagePath"].Value?.ToString();

                if (!string.IsNullOrEmpty(imageFileName))
                {
                    string fullImagePath = GetFullImagePath(imageFileName);

                    if (File.Exists(fullImagePath))
                    {
                        try
                        {
                            Form imageViewForm = new Form
                            {
                                Text = $"Просмотр изображения - {dataGridView.Rows[e.RowIndex].Cells["BoatName"].Value}",
                                Size = new Size(600, 500),
                                StartPosition = FormStartPosition.CenterParent,
                                FormBorderStyle = FormBorderStyle.Sizable
                            };

                            PictureBox pictureBox = new PictureBox
                            {
                                Dock = DockStyle.Fill,
                                SizeMode = PictureBoxSizeMode.Zoom,
                                Image = Image.FromFile(fullImagePath)
                            };

                            imageViewForm.Controls.Add(pictureBox);
                            imageViewForm.ShowDialog();

                            pictureBox.Image?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Не удалось открыть изображение: {ex.Message}", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Изображение не найдено", "Информация",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Изображение не найдено", "Информация",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void DataGridView_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 &&
                dataGridView.Columns.Contains("ImagePreview") &&
                e.ColumnIndex == dataGridView.Columns["ImagePreview"].Index)
            {
                string boatName = dataGridView.Rows[e.RowIndex].Cells["BoatName"].Value?.ToString();
                if (!string.IsNullOrEmpty(boatName))
                {
                    dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText =
                        $"Двойной клик для просмотра\n{boatName}";
                }
            }
        }

        private void ManagerServicesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Освобождаем ресурсы изображений
            if (dataGridView != null && dataGridView.Columns.Contains("ImagePreview"))
            {
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.Cells["ImagePreview"].Value is Image image &&
                        image != null)
                    {
                        // Проверяем, не является ли изображение заглушкой
                        bool isDefaultImage = false;
                        try
                        {
                            // Примерная проверка - можно добавить более точную
                            isDefaultImage = image.Width == 180 && image.Height == 120;
                        }
                        catch { }

                        if (!isDefaultImage)
                        {
                            row.Cells["ImagePreview"].Value = null;
                            image.Dispose();
                        }
                    }
                }
            }
        }
    }
}