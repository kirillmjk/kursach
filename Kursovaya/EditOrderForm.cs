using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Kursovaya
{
    public partial class EditOrderForm : Form
    {
        private int orderId;
        private string connectionString = ConnectionString.GetConnectionString();
        private DateTime maxAllowedDate;
        private DateTime originalStartDate;
        private DateTime originalEndDate;
        private int originalBoatId;
        private int originalStatusId;

        public EditOrderForm(int orderId)
        {
            this.orderId = orderId;
            InitializeComponent();

            // Устанавливаем максимальную дату - сегодня + 2 месяца
            maxAllowedDate = DateTime.Today.AddMonths(2);

            LoadComboBoxData();
            LoadOrderData();

            // Устанавливаем ограничения для календарей после загрузки данных
            SetDateRestrictions();
        }

        private void SetDateRestrictions()
        {
            // Устанавливаем минимальную дату - сегодня
            dtpStart.MinDate = DateTime.Today;
            dtpStart.MaxDate = maxAllowedDate;

            dtpEnd.MaxDate = maxAllowedDate;
        }

        private void LoadComboBoxData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Клиенты
                    string clientsQuery = "SELECT ID, ClientName FROM Clients";
                    MySqlCommand clientsCmd = new MySqlCommand(clientsQuery, connection);
                    MySqlDataAdapter clientsAdapter = new MySqlDataAdapter(clientsCmd);
                    DataTable clientsTable = new DataTable();
                    clientsAdapter.Fill(clientsTable);
                    cmbClient.DataSource = clientsTable;
                    cmbClient.DisplayMember = "ClientName";
                    cmbClient.ValueMember = "ID";

                    // Лодки
                    string boatsQuery = "SELECT ID, Nam, Price FROM Boat";
                    MySqlCommand boatsCmd = new MySqlCommand(boatsQuery, connection);
                    MySqlDataAdapter boatsAdapter = new MySqlDataAdapter(boatsCmd);
                    DataTable boatsTable = new DataTable();
                    boatsAdapter.Fill(boatsTable);
                    cmbBoat.DataSource = boatsTable;
                    cmbBoat.DisplayMember = "Nam";
                    cmbBoat.ValueMember = "ID";

                    // Статусы
                    string statusQuery = "SELECT ID, StatusName FROM OrderStatuses";
                    MySqlCommand statusCmd = new MySqlCommand(statusQuery, connection);
                    MySqlDataAdapter statusAdapter = new MySqlDataAdapter(statusCmd);
                    DataTable statusTable = new DataTable();
                    statusAdapter.Fill(statusTable);
                    cmbStatus.DataSource = statusTable;
                    cmbStatus.DisplayMember = "StatusName";
                    cmbStatus.ValueMember = "ID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadOrderData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT o.OrderDate, o.StartDate, o.EndDate, o.TotalPrice, 
                                   o.ClientID, o.BoatID, o.StatusID, c.ClientName, b.Nam as BoatName, os.StatusName
                                   FROM Orders o 
                                   INNER JOIN Clients c ON o.ClientID = c.ID
                                   INNER JOIN Boat b ON o.BoatID = b.ID
                                   INNER JOIN OrderStatuses os ON o.StatusID = os.ID
                                   WHERE o.ID = @ID";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ID", orderId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Сохраняем оригинальные значения
                            originalStartDate = Convert.ToDateTime(reader["StartDate"]);
                            originalEndDate = Convert.ToDateTime(reader["EndDate"]);
                            originalBoatId = Convert.ToInt32(reader["BoatID"]);
                            originalStatusId = Convert.ToInt32(reader["StatusID"]);

                            // Заполняем поля данными
                            lblOrderDate.Text = Convert.ToDateTime(reader["OrderDate"]).ToString("dd.MM.yyyy HH:mm");
                            dtpStart.Value = originalStartDate;
                            dtpEnd.Value = originalEndDate;
                            lblTotalPrice.Text = $"Общая стоимость: {Convert.ToDecimal(reader["TotalPrice"]):N2} руб";

                            // Устанавливаем выбранные значения в комбобоксы
                            cmbClient.SelectedValue = reader["ClientID"];
                            cmbBoat.SelectedValue = originalBoatId;
                            cmbStatus.SelectedValue = originalStatusId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных заказа: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CalculateTotalPrice()
        {
            if (cmbBoat.SelectedValue != null && dtpStart.Value < dtpEnd.Value)
            {
                try
                {
                    int boatId = Convert.ToInt32(cmbBoat.SelectedValue);
                    int days = (dtpEnd.Value - dtpStart.Value).Days + 1;

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "SELECT Price FROM Boat WHERE ID = @ID";
                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@ID", boatId);

                        decimal price = Convert.ToDecimal(command.ExecuteScalar());
                        decimal total = price * days;
                        lblTotalPrice.Text = $"Общая стоимость: {total:N2} руб";
                    }
                }
                catch (Exception) { }
            }
            else
            {
                lblTotalPrice.Text = "Общая стоимость: 0 руб";
            }
        }

        // Проверка доступности лодки на выбранные даты (исключая текущий заказ)
        private bool IsBoatAvailable(int boatId, DateTime startDate, DateTime endDate)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT COUNT(*) 
                        FROM Orders 
                        WHERE BoatID = @BoatID 
                          AND StatusID != 3 -- Исключаем отмененные заказы
                          AND ID != @OrderId -- Исключаем текущий заказ
                          AND (
                              (StartDate <= @EndDate AND EndDate >= @StartDate)
                          )";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@BoatID", boatId);
                    command.Parameters.AddWithValue("@StartDate", startDate.Date);
                    command.Parameters.AddWithValue("@EndDate", endDate.Date);
                    command.Parameters.AddWithValue("@OrderId", orderId);

                    object result = command.ExecuteScalar();
                    int count = result != null ? Convert.ToInt32(result) : 0;

                    return count == 0;
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
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT StartDate, EndDate 
                        FROM Orders 
                        WHERE BoatID = @BoatID 
                          AND StatusID != 3
                          AND ID != @OrderId
                          AND (
                              (StartDate <= @EndDate AND EndDate >= @StartDate)
                          )
                        ORDER BY StartDate";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@BoatID", boatId);
                    command.Parameters.AddWithValue("@StartDate", startDate.Date);
                    command.Parameters.AddWithValue("@EndDate", endDate.Date);
                    command.Parameters.AddWithValue("@OrderId", orderId);

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

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cmbClient.SelectedIndex == -1 || cmbBoat.SelectedIndex == -1 || cmbStatus.SelectedIndex == -1)
            {
                MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (dtpStart.Value >= dtpEnd.Value)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // Проверяем доступность лодки, только если изменилась лодка или даты
            int newBoatId = Convert.ToInt32(cmbBoat.SelectedValue);
            DateTime newStartDate = dtpStart.Value.Date;
            DateTime newEndDate = dtpEnd.Value.Date;

            if (newBoatId != originalBoatId ||
                newStartDate != originalStartDate ||
                newEndDate != originalEndDate)
            {
                if (!IsBoatAvailable(newBoatId, newStartDate, newEndDate))
                {
                    DialogResult result = MessageBox.Show(
                        "Выбранная лодка занята на указанные даты.\n\nХотите посмотреть расписание занятости?",
                        "Лодка недоступна",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        ShowBoatSchedule(newBoatId, newStartDate, newEndDate);
                    }
                    return;
                }
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"UPDATE Orders SET 
                                   StartDate = @StartDate, 
                                   EndDate = @EndDate, 
                                   TotalPrice = @TotalPrice, 
                                   ClientID = @ClientID, 
                                   BoatID = @BoatID, 
                                   StatusID = @StatusID 
                                   WHERE ID = @ID";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@StartDate", dtpStart.Value);
                    command.Parameters.AddWithValue("@EndDate", dtpEnd.Value);
                    command.Parameters.AddWithValue("@TotalPrice", GetTotalPrice());
                    command.Parameters.AddWithValue("@ClientID", cmbClient.SelectedValue);
                    command.Parameters.AddWithValue("@BoatID", cmbBoat.SelectedValue);
                    command.Parameters.AddWithValue("@StatusID", cmbStatus.SelectedValue);
                    command.Parameters.AddWithValue("@ID", orderId);

                    command.ExecuteNonQuery();

                    MessageBox.Show("Заказ успешно обновлен!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

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

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void cmbBoat_SelectedIndexChanged(object sender, EventArgs e)
        {
            CalculateTotalPrice();
        }

        private void dtpStart_ValueChanged(object sender, EventArgs e)
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

        private void dtpEnd_ValueChanged(object sender, EventArgs e)
        {
            CalculateTotalPrice();
        }

        private decimal GetTotalPrice()
        {
            string priceText = lblTotalPrice.Text.Replace("Общая стоимость: ", "").Replace(" руб", "");
            return decimal.Parse(priceText);
        }

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
    }
}