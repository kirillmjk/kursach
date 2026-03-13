using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Kursovaya
{
    public partial class AddEditServiceForm : Form
    {
        private int? serviceId = null;                 // ID услуги (null для новой)
        private string connectionString = ConnectionString.GetConnectionString();
        private string imagesFolderPath;                // Папка для изображений
        private Image selectedImage = null;             // Выбранное изображение
        private string currentImageFileName = null;     // Имя текущего файла
        private bool imageChanged = false;              // Флаг изменения изображения

        private const long MAX_IMAGE_SIZE = 5 * 1024 * 1024; // Макс. 5 МБ

        public AddEditServiceForm(int? id = null)
        {
            InitializeComponent();

            InitializeImagePaths();                     // Создаем папки
            serviceId = id;
            LoadCategories();                           // Загружаем категории

            if (serviceId.HasValue)
            {
                LoadServiceData();                      // Загружаем данные услуги
                this.Text = "Редактирование услуги";
            }
            else
            {
                this.Text = "Добавление услуги";
            }

            SetupPictureBox();                          // Настройка просмотра изображения
        }

        // Настройка PictureBox
        private void SetupPictureBox()
        {
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom; // Масштабирование с сохранением пропорций
            pictureBox.BorderStyle = BorderStyle.FixedSingle;
            pictureBox.BackColor = Color.White;
            pictureBox.MinimumSize = new Size(200, 150);

            UpdateImageControls();
        }

        // Создание папок для изображений
        private void InitializeImagePaths()
        {
            string appName = "BoatRentalSystem";
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                appName);

            imagesFolderPath = Path.Combine(appDataPath, "BoatImages");

            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);
            if (!Directory.Exists(imagesFolderPath))
                Directory.CreateDirectory(imagesFolderPath);
        }

        // Полный путь к файлу изображения
        private string GetFullImagePath(string fileName)
        {
            return string.IsNullOrEmpty(fileName) ? null : Path.Combine(imagesFolderPath, fileName);
        }

        // Загрузка категорий в комбобокс
        private void LoadCategories()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT ID, CategoryName FROM BoatCategories ORDER BY CategoryName";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    cmbClass.DataSource = table;
                    cmbClass.DisplayMember = "CategoryName";
                    cmbClass.ValueMember = "ID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке категорий: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Загрузка данных услуги для редактирования
        private void LoadServiceData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT b.Nam, b.CategoryID, b.Price, b.Description, b.ImagePath 
                                  FROM Boat b WHERE b.ID = @ID";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ID", serviceId.Value);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtName.Text = reader["Nam"].ToString();
                            cmbClass.SelectedValue = reader["CategoryID"];
                            txtPrice.Text = reader["Price"].ToString();
                            txtDescription.Text = reader["Description"].ToString();

                            // Загрузка изображения
                            if (!reader.IsDBNull(reader.GetOrdinal("ImagePath")))
                            {
                                currentImageFileName = reader["ImagePath"].ToString();
                                string fullPath = GetFullImagePath(currentImageFileName);

                                if (File.Exists(fullPath))
                                {
                                    try
                                    {
                                        using (Image originalImage = Image.FromFile(fullPath))
                                        {
                                            selectedImage = new Bitmap(originalImage);
                                        }
                                        pictureBox.Image = selectedImage;
                                    }
                                    catch
                                    {
                                        SetDefaultImage();
                                    }
                                }
                                else
                                {
                                    SetDefaultImage();
                                }
                            }
                            else
                            {
                                SetDefaultImage();
                            }
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

        // Установка изображения-заглушки
        private void SetDefaultImage()
        {
            try
            {
                Bitmap defaultImage = new Bitmap(180, 120);
                using (Graphics g = Graphics.FromImage(defaultImage))
                {
                    g.Clear(Color.LightGray);

                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Center;
                    stringFormat.LineAlignment = StringAlignment.Center;

                    g.DrawString("Нет\nизображения",
                        new Font("Arial", 12, FontStyle.Bold),
                        Brushes.Black,
                        new RectangleF(0, 0, 180, 120),
                        stringFormat);
                }

                if (pictureBox.Image != null && pictureBox.Image != selectedImage)
                    pictureBox.Image.Dispose();

                pictureBox.Image = defaultImage;
            }
            catch
            {
                pictureBox.Image = new Bitmap(180, 120);
            }
        }

        // Выбор изображения
        private void BtnSelectImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                openFileDialog.Title = "Выберите изображение (макс. 5 МБ)";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(openFileDialog.FileName);

                        // Проверка размера
                        if (fileInfo.Length > MAX_IMAGE_SIZE)
                        {
                            MessageBox.Show($"Размер изображения слишком большой! Максимум 5 МБ", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Освобождаем предыдущее изображение
                        if (selectedImage != null)
                            selectedImage.Dispose();

                        // Загружаем новое
                        using (Image originalImage = Image.FromFile(openFileDialog.FileName))
                        {
                            selectedImage = new Bitmap(originalImage);
                        }

                        pictureBox.Image = selectedImage;
                        imageChanged = true;
                        UpdateImageControls();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не удалось загрузить изображение: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Удаление изображения
        private void BtnRemoveImage_Click(object sender, EventArgs e)
        {
            if (selectedImage != null)
            {
                selectedImage.Dispose();
                selectedImage = null;
            }

            SetDefaultImage();
            imageChanged = true;
            UpdateImageControls();
        }

        // Обновление состояния кнопок
        private void UpdateImageControls()
        {
            btnRemoveImage.Enabled = (selectedImage != null || !string.IsNullOrEmpty(currentImageFileName));
        }

        // Сохранение изображения на диск
        private string SaveImageToDisk()
        {
            if (selectedImage == null)
            {
                if (imageChanged && !string.IsNullOrEmpty(currentImageFileName))
                {
                    // Удаляем старый файл
                    try
                    {
                        string oldFilePath = GetFullImagePath(currentImageFileName);
                        if (File.Exists(oldFilePath))
                            File.Delete(oldFilePath);
                    }
                    catch { }
                }
                return null;
            }

            try
            {
                // Генерация уникального имени
                string safeName = txtName.Text.Trim();
                foreach (char c in Path.GetInvalidFileNameChars())
                    safeName = safeName.Replace(c, '_');

                string fileName = $"{safeName}_{DateTime.Now:yyyyMMddHHmmssfff}.jpg";
                string fullPath = Path.Combine(imagesFolderPath, fileName);

                // Сохранение с оптимизацией размера
                SaveImageWithSizeCheck(selectedImage, fullPath);

                // Удаление старого файла
                if (!string.IsNullOrEmpty(currentImageFileName))
                {
                    string oldFilePath = GetFullImagePath(currentImageFileName);
                    if (oldFilePath != fullPath && File.Exists(oldFilePath))
                        File.Delete(oldFilePath);
                }

                return fileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении изображения: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        // Сохранение с проверкой размера и оптимизацией
        private void SaveImageWithSizeCheck(Image image, string filePath)
        {
            // Уменьшение размеров при необходимости
            Image imageToSave = image;
            bool imageResized = false;

            if (image.Width > 1000 || image.Height > 1000)
            {
                int newWidth, newHeight;
                if (image.Width > image.Height)
                {
                    newWidth = 1000;
                    newHeight = (int)((double)image.Height / image.Width * 1000);
                }
                else
                {
                    newHeight = 1000;
                    newWidth = (int)((double)image.Width / image.Height * 1000);
                }

                Bitmap resizedImage = new Bitmap(newWidth, newHeight);
                using (Graphics g = Graphics.FromImage(resizedImage))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, 0, 0, newWidth, newHeight);
                }
                imageToSave = resizedImage;
                imageResized = true;
            }

            try
            {
                // Сохранение с качеством 90%
                ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);

                using (MemoryStream ms = new MemoryStream())
                {
                    imageToSave.Save(ms, jpegCodec, encoderParams);

                    if (ms.Length <= MAX_IMAGE_SIZE)
                    {
                        File.WriteAllBytes(filePath, ms.ToArray());
                    }
                    else
                    {
                        // Если все еще большой, сохраняем как есть
                        imageToSave.Save(filePath, ImageFormat.Jpeg);
                    }
                }
            }
            finally
            {
                if (imageResized && imageToSave != image)
                    imageToSave.Dispose();
            }
        }

        private ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
                if (codec.MimeType == mimeType)
                    return codec;
            return null;
        }

        // Сохранение услуги
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                string imageFileName = null;
                if (imageChanged || (serviceId.HasValue && selectedImage != null))
                {
                    imageFileName = SaveImageToDisk();
                }
                else if (serviceId.HasValue && !imageChanged)
                {
                    imageFileName = currentImageFileName;
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query;
                    MySqlCommand command;

                    if (serviceId.HasValue)
                    {
                        query = @"UPDATE Boat SET 
                                Nam = @Name, CategoryID = @CategoryID, Price = @Price, 
                                Description = @Description, ImagePath = @ImagePath 
                                WHERE ID = @ID";
                        command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@ID", serviceId.Value);
                    }
                    else
                    {
                        query = @"INSERT INTO Boat (Nam, CategoryID, Price, Description, ImagePath) 
                                VALUES (@Name, @CategoryID, @Price, @Description, @ImagePath)";
                        command = new MySqlCommand(query, connection);
                    }

                    command.Parameters.AddWithValue("@Name", txtName.Text.Trim());
                    command.Parameters.AddWithValue("@CategoryID", cmbClass.SelectedValue);
                    command.Parameters.AddWithValue("@Price", decimal.Parse(txtPrice.Text));
                    command.Parameters.AddWithValue("@Description", txtDescription.Text.Trim());

                    command.Parameters.AddWithValue("@ImagePath",
                        string.IsNullOrEmpty(imageFileName) ? DBNull.Value : (object)imageFileName);

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

        // Валидация ввода
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название услуги", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtName.Focus();
                return false;
            }

            if (cmbClass.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите класс", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmbClass.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPrice.Text))
            {
                MessageBox.Show("Введите стоимость", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPrice.Focus();
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную стоимость (положительное число)", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPrice.Focus();
                return false;
            }

            return true;
        }

        // Ограничение ввода для цены
        private void TxtPrice_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            if (e.KeyChar == '.')
                e.KeyChar = ',';

            if (e.KeyChar == ',' && txtPrice.Text.IndexOf(',') > -1)
                e.Handled = true;
        }

        // Двойной клик по изображению - просмотр
        private void PictureBox_DoubleClick(object sender, EventArgs e)
        {
            Image imageToShow = null;
            string title = txtName.Text;

            if (selectedImage != null)
            {
                imageToShow = selectedImage;
            }
            else if (!string.IsNullOrEmpty(currentImageFileName))
            {
                string fullPath = GetFullImagePath(currentImageFileName);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        imageToShow = Image.FromFile(fullPath);
                    }
                    catch
                    {
                        MessageBox.Show("Не удалось открыть изображение", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            if (imageToShow != null)
            {
                // Форма просмотра
                Form viewForm = new Form
                {
                    Text = $"Просмотр изображения: {title}",
                    Size = new Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent
                };

                PictureBox viewPictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = imageToShow,
                    BackColor = Color.Black
                };

                viewForm.Controls.Add(viewPictureBox);

                // Кнопка закрытия
                Button closeButton = new Button
                {
                    Text = "Закрыть (Esc)",
                    Size = new Size(120, 30),
                    Location = new Point(10, 10),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(240, 240, 240)
                };
                closeButton.Click += (s, ev) => viewForm.Close();

                viewForm.Controls.Add(closeButton);
                closeButton.BringToFront();

                viewForm.KeyPreview = true;
                viewForm.KeyDown += (s, ev) =>
                {
                    if (ev.KeyCode == Keys.Escape)
                        viewForm.Close();
                };

                viewForm.ShowDialog();

                if (imageToShow != selectedImage)
                    imageToShow.Dispose();
            }
        }

        // Освобождение ресурсов при закрытии
        private void AddEditServiceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (pictureBox.Image != null && pictureBox.Image != selectedImage)
                pictureBox.Image.Dispose();

            if (selectedImage != null)
                selectedImage.Dispose();
        }

        // Отмена
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}