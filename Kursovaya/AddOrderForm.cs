using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.IO;

namespace Kursovaya
{
    public partial class AddOrderForm : Form
    {
        private string СonnectionString = ConnectionString.GetConnectionString();
        private DateTime maxAllowedDate; // Максимальная дата (сегодня + 2 месяца)
        private int currentUserId; // ID текущего пользователя

        public AddOrderForm()
        {
            InitializeComponent();

            // Получаем ID текущего пользователя из сессии
            if (UserSession.CurrentUser != null)
            {
                currentUserId = UserSession.CurrentUser.UserID;
            }
            else
            {
                MessageBox.Show("Ошибка: не удалось определить текущего пользователя", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            // Устанавливаем максимальную дату - сегодня + 2 месяца
            maxAllowedDate = DateTime.Today.AddMonths(2);

            LoadComboBoxData(); // Загружаем данные в выпадающие списки
            CalculateTotalPrice(); // Считаем начальную стоимость

            // Настройка ограничений для дат
            dtpStart.MinDate = DateTime.Today; // Нельзя выбрать дату в прошлом
            dtpStart.MaxDate = maxAllowedDate; // Нельзя выбрать позже 2 месяцев
            dtpEnd.MaxDate = maxAllowedDate;
            dtpEnd.MinDate = DateTime.Today.AddDays(1); // Окончание не раньше следующего дня

            if (dtpEnd.Value > maxAllowedDate)
            {
                dtpEnd.Value = maxAllowedDate;
            }
        }

        // Загрузка клиентов и лодок в выпадающие списки
        private void LoadComboBoxData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(СonnectionString))
                {
                    connection.Open();

                    // Загружаем список клиентов
                    string clientsQuery = "SELECT ID, ClientName FROM Clients";
                    MySqlCommand clientsCmd = new MySqlCommand(clientsQuery, connection);
                    MySqlDataAdapter clientsAdapter = new MySqlDataAdapter(clientsCmd);
                    DataTable clientsTable = new DataTable();
                    clientsAdapter.Fill(clientsTable);
                    cmbClient.DataSource = clientsTable;
                    cmbClient.DisplayMember = "ClientName";
                    cmbClient.ValueMember = "ID";

                    // Загружаем список лодок
                    string boatsQuery = "SELECT ID, Nam, Price FROM Boat";
                    MySqlCommand boatsCmd = new MySqlCommand(boatsQuery, connection);
                    MySqlDataAdapter boatsAdapter = new MySqlDataAdapter(boatsCmd);
                    DataTable boatsTable = new DataTable();
                    boatsAdapter.Fill(boatsTable);
                    cmbBoat.DataSource = boatsTable;
                    cmbBoat.DisplayMember = "Nam";
                    cmbBoat.ValueMember = "ID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Расчет общей стоимости аренды
        private void CalculateTotalPrice()
        {
            if (cmbBoat.SelectedValue != null && dtpStart.Value.Date < dtpEnd.Value.Date)
            {
                try
                {
                    int boatId = Convert.ToInt32(cmbBoat.SelectedValue);
                    int days = (dtpEnd.Value.Date - dtpStart.Value.Date).Days + 1; // Количество дней

                    using (MySqlConnection connection = new MySqlConnection(СonnectionString))
                    {
                        connection.Open();
                        string query = "SELECT Price FROM Boat WHERE ID = @ID";
                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@ID", boatId);

                        object result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            decimal price = Convert.ToDecimal(result);
                            decimal total = price * days; // Цена * количество дней
                            lblTotalPrice.Text = $"Общая стоимость: {total:N2} руб";
                        }
                    }
                }
                catch (Exception)
                {
                    // Игнорируем ошибки при расчете
                }
            }
            else
            {
                lblTotalPrice.Text = "Общая стоимость: 0 руб";
            }
        }

        // Проверка доступности лодки на выбранные даты
        private bool IsBoatAvailable(int boatId, DateTime startDate, DateTime endDate)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(СonnectionString))
                {
                    connection.Open();

                    // Проверяем пересечение дат с существующими заказами (кроме отмененных)
                    string query = @"
                        SELECT COUNT(*) 
                        FROM Orders 
                        WHERE BoatID = @BoatID 
                          AND StatusID != 3 -- Исключаем отмененные заказы
                          AND (
                              (StartDate <= @EndDate AND EndDate >= @StartDate)
                          )";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@BoatID", boatId);
                    command.Parameters.AddWithValue("@StartDate", startDate.Date);
                    command.Parameters.AddWithValue("@EndDate", endDate.Date);

                    object result = command.ExecuteScalar();
                    int count = result != null ? Convert.ToInt32(result) : 0;

                    return count == 0; // true - если нет пересечений (лодка свободна)
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке доступности: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Показываем информацию о занятости лодки
        private void ShowBoatSchedule(int boatId, DateTime startDate, DateTime endDate)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(СonnectionString))
                {
                    connection.Open();

                    // Ищем все заказы на эту лодку в указанном периоде
                    string query = @"
                        SELECT StartDate, EndDate 
                        FROM Orders 
                        WHERE BoatID = @BoatID 
                          AND StatusID != 3
                          AND (
                              (StartDate <= @EndDate AND EndDate >= @StartDate)
                          )
                        ORDER BY StartDate";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@BoatID", boatId);
                    command.Parameters.AddWithValue("@StartDate", startDate.Date);
                    command.Parameters.AddWithValue("@EndDate", endDate.Date);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        string busyDates = "Лодка занята в следующие даты:\n\n";
                        int count = 0;

                        while (reader.Read())
                        {
                            DateTime busyStart = reader.GetDateTime("StartDate");
                            DateTime busyEnd = reader.GetDateTime("EndDate");
                            busyDates += $"с {busyStart:dd.MM.yyyy} по {busyEnd:dd.MM.yyyy}\n";
                            count++;
                        }

                        if (count > 0)
                        {
                            MessageBox.Show(busyDates, "Информация о занятости",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке расписания: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Создание чека для только что созданного заказа
        private void CreateReceiptForOrder(int orderId)
        {
            try
            {
                string receiptText = GenerateReceiptText(orderId);

                // Сохраняем во временный файл
                string tempFile = Path.Combine(Path.GetTempPath(),
                    $"Чек_заказ_{orderId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                File.WriteAllText(tempFile, receiptText, System.Text.Encoding.UTF8);

                // Открываем в блокноте
                Process.Start("notepad.exe", tempFile);

                MessageBox.Show($"Чек для нового заказа №{orderId} сформирован и открыт в блокноте",
                    "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании чека: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Генерация текста чека
        private string GenerateReceiptText(int orderId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(СonnectionString))
                {
                    connection.Open();
                    // Получаем все данные для чека
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
                    u.FullName as ManagerName
                FROM Orders o
                INNER JOIN Clients c ON o.ClientID = c.ID
                INNER JOIN Boat b ON o.BoatID = b.ID
                INNER JOIN BoatCategories bc ON b.CategoryID = bc.ID
                INNER JOIN Users u ON o.UserID = u.ID
                WHERE o.ID = @OrderId";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@OrderId", orderId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int days = (reader.GetDateTime("EndDate") - reader.GetDateTime("StartDate")).Days + 1;

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

        // Сохранение заказа
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Проверка заполнения полей
            if (cmbClient.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите клиента", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmbClient.Focus();
                return;
            }

            if (cmbBoat.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите лодку", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmbBoat.Focus();
                return;
            }

            // Проверка дат
            if (dtpStart.Value.Date >= dtpEnd.Value.Date)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                dtpEnd.Focus();
                return;
            }

            if (dtpStart.Value.Date < DateTime.Today)
            {
                MessageBox.Show("Дата начала не может быть в прошлом", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                dtpStart.Focus();
                return;
            }

            // Проверка на 2-месячный период
            if (dtpStart.Value.Date > maxAllowedDate)
            {
                MessageBox.Show($"Дата начала не может быть позже {maxAllowedDate:dd.MM.yyyy}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                dtpStart.Focus();
                return;
            }

            if (dtpEnd.Value.Date > maxAllowedDate)
            {
                MessageBox.Show($"Дата окончания не может быть позже {maxAllowedDate:dd.MM.yyyy}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                dtpEnd.Focus();
                return;
            }

            // Получаем ID выбранной лодки и даты
            int boatId = Convert.ToInt32(cmbBoat.SelectedValue);
            DateTime startDate = dtpStart.Value.Date;
            DateTime endDate = dtpEnd.Value.Date;

            // Проверяем доступность лодки
            if (!IsBoatAvailable(boatId, startDate, endDate))
            {
                DialogResult result = MessageBox.Show(
                    "Выбранная лодка занята на указанные даты.\n\nХотите посмотреть расписание занятости?",
                    "Лодка недоступна",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    ShowBoatSchedule(boatId, startDate, endDate);
                }
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(СonnectionString))
                {
                    connection.Open();

                    // Получаем цену лодки
                    string priceQuery = "SELECT Price FROM Boat WHERE ID = @BoatID";
                    MySqlCommand priceCmd = new MySqlCommand(priceQuery, connection);
                    priceCmd.Parameters.AddWithValue("@BoatID", boatId);

                    object priceResult = priceCmd.ExecuteScalar();
                    if (priceResult == null || priceResult == DBNull.Value)
                    {
                        MessageBox.Show("Не удалось получить цену лодки", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    decimal price = Convert.ToDecimal(priceResult);
                    int days = (endDate - startDate).Days + 1;
                    decimal totalPrice = price * days;

                    // Сохраняем заказ в БД
                    string insertQuery = @"
                        INSERT INTO Orders (
                            OrderDate, StartDate, EndDate, TotalPrice, 
                            ClientID, UserID, BoatID, StatusID
                        ) VALUES (
                            @OrderDate, @StartDate, @EndDate, @TotalPrice, 
                            @ClientID, @UserID, @BoatID, 1
                        );
                        SELECT LAST_INSERT_ID();"; // Получаем ID созданного заказа

                    MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                    insertCommand.Parameters.AddWithValue("@StartDate", startDate);
                    insertCommand.Parameters.AddWithValue("@EndDate", endDate);
                    insertCommand.Parameters.AddWithValue("@TotalPrice", totalPrice);
                    insertCommand.Parameters.AddWithValue("@ClientID", cmbClient.SelectedValue);
                    insertCommand.Parameters.AddWithValue("@UserID", currentUserId);
                    insertCommand.Parameters.AddWithValue("@BoatID", boatId);

                    // Выполняем запрос и получаем ID нового заказа
                    int newOrderId = Convert.ToInt32(insertCommand.ExecuteScalar());

                    if (newOrderId > 0)
                    {
                        MessageBox.Show($"Заказ №{newOrderId} успешно создан!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Спрашиваем, создать ли чек
                        DialogResult receiptResult = MessageBox.Show(
                            $"Создать чек для заказа №{newOrderId}?",
                            "Создание чека",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (receiptResult == DialogResult.Yes)
                        {
                            CreateReceiptForOrder(newOrderId);
                        }

                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось создать заказ", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении заказа: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Проверка доступности лодки по кнопке
        private void BtnCheckAvailability_Click(object sender, EventArgs e)
        {
            if (cmbBoat.SelectedIndex == -1)
            {
                MessageBox.Show("Сначала выберите лодку", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (dtpStart.Value.Date >= dtpEnd.Value.Date)
            {
                MessageBox.Show("Укажите корректный период дат", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Проверка на 2-месячный период
            if (dtpStart.Value.Date > maxAllowedDate)
            {
                MessageBox.Show($"Дата начала не может быть позже {maxAllowedDate:dd.MM.yyyy}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (dtpEnd.Value.Date > maxAllowedDate)
            {
                MessageBox.Show($"Дата окончания не может быть позже {maxAllowedDate:dd.MM.yyyy}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int boatId = Convert.ToInt32(cmbBoat.SelectedValue);
            DateTime startDate = dtpStart.Value.Date;
            DateTime endDate = dtpEnd.Value.Date;

            if (IsBoatAvailable(boatId, startDate, endDate))
            {
                MessageBox.Show("Лодка доступна на выбранные даты!", "Доступно",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                DialogResult result = MessageBox.Show(
                    "Лодка занята на выбранные даты.\n\nПоказать расписание занятости?",
                    "Недоступно",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    ShowBoatSchedule(boatId, startDate, endDate);
                }
            }
        }

        // Обработчики событий для пересчета стоимости при изменении данных
        private void CmbBoat_SelectedIndexChanged(object sender, EventArgs e)
        {
            CalculateTotalPrice();
        }

        private void DtpStart_ValueChanged(object sender, EventArgs e)
        {
            // Обновляем минимальную дату для dtpEnd
            dtpEnd.MinDate = dtpStart.Value.AddDays(1);

            // Проверяем, не выходит ли dtpEnd за максимальную дату
            if (dtpEnd.Value > maxAllowedDate)
            {
                dtpEnd.Value = maxAllowedDate;
            }

            CalculateTotalPrice();
        }

        private void DtpEnd_ValueChanged(object sender, EventArgs e)
        {
            CalculateTotalPrice();
        }

        // Отмена - закрытие формы
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}