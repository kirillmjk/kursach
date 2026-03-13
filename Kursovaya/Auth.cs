using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.IO;

namespace Kursovaya
{
    // Форма авторизации пользователя
    public partial class Auth : Form
    {
        // Счетчик неудачных попыток входа
        int AuthAtt = 0;
        // Строка подключения к БД
        string ConnStr = ConnectionString.GetConnectionString();

        // Новые поля для каптчи
        private string currentCaptchaCode = "";
        private List<string> captchaImages = new List<string>();
        private Random random = new Random();

        //Флаги и таймеры для каптчи
        private bool isCaptchaRequired = false;
        private bool isBlocked = false;
        private DateTime blockEndTime;
        private System.Windows.Forms.Timer blockTimer;
        private int failedCaptchaAttempts = 0;

        // Исходный размер формы
        private Size originalSize;
        // Размер формы с каптчей
        private Size expandedSize = new Size(640, 457);

        public Auth()
        {
            InitializeComponent();

            // Запоминаем исходный размер
            originalSize = this.Size;

            // Устанавливаем исходный размер
            this.Size = originalSize;

            // Изначально скрываем элементы каптчи
            HideCaptchaElements();

            //// Загружаем список изображений при создании формы
            LoadCaptchaImages();

            // Инициализируем таймер для блокировки
            blockTimer = new System.Windows.Forms.Timer();
            blockTimer.Interval = 1000; // 1 секунда для плавного отсчета
            blockTimer.Tick += BlockTimer_Tick;

            // Подписываемся на события клавиш
            this.loginTextBox.KeyPress += LoginTextBox_KeyPress;
            this.pwdTextBox.KeyPress += PwdTextBox_KeyPress;
            this.captchaTextBox.KeyPress += CaptchaTextBox_KeyPress;
        }

        // Скрыть элементы каптчи и уменьшить форму
        private void HideCaptchaElements()
        {
            captchaPictureBox.Visible = false;
            captchaTextBox.Visible = false;
            RefreshCaptchaButton.Visible = false;


            //    // Возвращаем исходный размер
            this.Size = originalSize;
            // Центрируем форму
            CenterToScreen();
        }

        // Показать элементы каптчи и увеличить форму
        private void ShowCaptchaElements()
        {
            // Увеличиваем размер формы
            this.Size = expandedSize;
            // Центрируем форму
            CenterToScreen();

            captchaPictureBox.Visible = true;
            captchaTextBox.Visible = true;
            RefreshCaptchaButton.Visible = true;

            // Генерируем новую каптчу при показе
            GenerateCaptcha();
        }

        // Загрузка списка изображений из папки
        private void LoadCaptchaImages()
        {
            try
            {
                string captchaFolder = Path.Combine(Application.StartupPath, "captchaimg");

                if (Directory.Exists(captchaFolder))
                {
                    string[] imageFiles = Directory.GetFiles(captchaFolder, "*.*")
                        .Where(f => f.EndsWith(".jpg") || f.EndsWith(".jpeg") ||
                                   f.EndsWith(".png") || f.EndsWith(".bmp") ||
                                   f.EndsWith(".gif"))
                        .ToArray();

                    captchaImages.Clear();
                    captchaImages.AddRange(imageFiles);
                }
                else
                {
                    Directory.CreateDirectory(captchaFolder);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке изображений каптчи: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //// Генерация новой каптчи
        private void GenerateCaptcha()
        {
            if (captchaImages.Count == 0)
            {
                GenerateTextCaptcha();
                return;
            }

            try
            {
                if (captchaPictureBox.Image != null)
                {
                    captchaPictureBox.Image.Dispose();
                }

                int randomIndex = random.Next(captchaImages.Count);
                string selectedImage = captchaImages[randomIndex];

                captchaPictureBox.Image = Image.FromFile(selectedImage);

                string fileName = Path.GetFileNameWithoutExtension(selectedImage);

                if (System.Text.RegularExpressions.Regex.IsMatch(fileName, @"^[a-zA-Z0-9]+$"))
                {
                    currentCaptchaCode = fileName;
                }
                else
                {
                    GenerateTextCaptcha();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке изображения каптчи: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                GenerateTextCaptcha();
            }
        }

        // Генерация текстовой каптчи с буквами
        private void GenerateTextCaptcha()
        {
            if (captchaPictureBox.Image != null)
            {
                captchaPictureBox.Image.Dispose();
            }

            // Генерируем код из букв и цифр
            currentCaptchaCode = GenerateRandomCode(5); // 5 символов

            Bitmap bitmap = new Bitmap(250, 100);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);

                // Добавляем случайный поворот для каждой буквы
                for (int i = 0; i < currentCaptchaCode.Length; i++)
                {
                    using (Font font = new Font("Arial", 28 + random.Next(-5, 5),
                           random.Next(0, 2) == 0 ? FontStyle.Regular : FontStyle.Bold))
                    {
                        // Сохраняем состояние графики
                        var state = g.Save();

                        // Применяем трансформацию для поворота
                        float x = 20 + i * 40;
                        float y = 30;

                        // Поворачиваем каждый символ на случайный угол
                        g.TranslateTransform(x + 15, y + 15);
                        g.RotateTransform(random.Next(-15, 15));
                        g.TranslateTransform(-x - 15, -y - 15);

                        // Рисуем символ
                        g.DrawString(currentCaptchaCode[i].ToString(), font,
                            Brushes.Black, x, y);

                        // Восстанавливаем состояние
                        g.Restore(state);
                    }
                }

                // Добавляем шум (линии)
                using (Pen pen = new Pen(Color.FromArgb(100, Color.Gray), 1))
                {
                    for (int i = 0; i < 15; i++)
                    {
                        g.DrawLine(pen, random.Next(0, 250), random.Next(0, 100),
                            random.Next(0, 250), random.Next(0, 100));
                    }
                }

                // Добавляем шум (точки)
                for (int i = 0; i < 200; i++)
                {
                    bitmap.SetPixel(random.Next(0, 250), random.Next(0, 100),
                        Color.FromArgb(100, Color.Gray));
                }
            }

            captchaPictureBox.Image = bitmap;
        }

        // Генерация случайного кода из букв и цифр
        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Исключаем похожие символы
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }

        //Хеширование пароля
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

        // Обработчик тика таймера блокировки
        private void BlockTimer_Tick(object sender, EventArgs e)
        {
            if (isBlocked)
            {
                TimeSpan timeLeft = blockEndTime - DateTime.Now;

                if (timeLeft.TotalSeconds > 0)
                {
                    // Обновляем отображение времени

                }
                else
                {
                    // Снимаем блокировку
                    isBlocked = false;
                    blockTimer.Stop();

                    EnableInputControls(true);

                    if (isCaptchaRequired)
                    {
                        ShowCaptchaElements();
                        GenerateCaptcha();
                    }

                    MessageBox.Show("Блокировка снята. Вы можете продолжить попытки входа.",
                        "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // Блокировка формы
        private void BlockForm(int seconds)
        {
            isBlocked = true;
            blockEndTime = DateTime.Now.AddSeconds(seconds);

            EnableInputControls(false);

            blockTimer.Start();
        }

        //Включение/отключение элементов ввода
        private void EnableInputControls(bool enable)
        {
            loginTextBox.Enabled = enable;
            pwdTextBox.Enabled = enable;
            LogInButton.Enabled = enable;
            exitButton.Enabled = enable;
            RefreshCaptchaButton.Enabled = enable && isCaptchaRequired;
            captchaTextBox.Enabled = enable && isCaptchaRequired;
        }

        // Обработчик кнопки входа
        private void LogInButton_Click(object sender, EventArgs e)
        {
            if (isBlocked)
            {
                MessageBox.Show($"Форма заблокирована. Пожалуйста, подождите.",
                    "Блокировка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string login = loginTextBox.Text.Trim();
            string password = pwdTextBox.Text;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (isCaptchaRequired)
            {
                string captchaInput = captchaTextBox.Text.Trim();

                if (string.IsNullOrEmpty(captchaInput))
                {
                    MessageBox.Show("Введите код с картинки", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!captchaInput.Equals(currentCaptchaCode, StringComparison.OrdinalIgnoreCase))
                {
                    failedCaptchaAttempts++;
                    MessageBox.Show($"Неверный код с картинки", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    captchaTextBox.Clear();

                    // Блокируем на 10 секунд при неверном вводе каптчи
                    BlockForm(10);

                    return;
                }

                failedCaptchaAttempts = 0;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(ConnStr))
                {
                    connection.Open();
                    string hashedPassword = ComputeSha256Hash(password);

                    string query = @"
                        SELECT u.ID, u.FullName, r.RoleName 
                        FROM Users u 
                        INNER JOIN Roles r ON u.RoleID = r.ID 
                        WHERE u.Login = @Login AND u.Pass = @Password";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Login", login);
                    command.Parameters.AddWithValue("@Password", hashedPassword);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int userId = reader.GetInt32("ID");
                            string fullName = reader.GetString("FullName");
                            string roleName = reader.GetString("RoleName");

                            UserSession.CurrentUser = new UserInfo
                            {
                                UserID = userId,
                                Login = login,
                                FullName = fullName,
                                Role = roleName
                            };

                            AuthAtt = 0;
                            isCaptchaRequired = false;
                            failedCaptchaAttempts = 0;

                            this.Hide();

                            switch (roleName.ToLower())
                            {
                                case "администратор":
                                case "admin":
                                    AdminForm adminForm = new AdminForm(userId);
                                    adminForm.Show();
                                    break;
                                case "менеджер":
                                case "manager":
                                    ManagerForm managerForm = new ManagerForm(userId);
                                    managerForm.Show();
                                    break;
                                case "директор":
                                case "director":
                                    DirectorForm directorForm = new DirectorForm(userId);
                                    directorForm.Show();
                                    break;
                                default:
                                    MessageBox.Show("Неизвестная роль пользователя", "Ошибка",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    this.Show();
                                    break;
                            }
                        }
                        else
                        {
                            AuthAtt++;
                            MessageBox.Show($"Неверный логин или пароль", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);

                            // После первой неудачной попытки показываем каптчу
                            if (!isCaptchaRequired)
                            {
                                isCaptchaRequired = true;
                                ShowCaptchaElements();
                                MessageBox.Show("Для продолжения введите код с картинки",
                                    "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }

                            loginTextBox.Clear();
                            pwdTextBox.Clear();
                            loginTextBox.Focus();

                            if (isCaptchaRequired)
                            {
                                captchaTextBox.Clear();
                                GenerateCaptcha();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при авторизации: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Кнопка обновления каптчи
        private void RefreshCaptchaButton_Click(object sender, EventArgs e)
        {
            if (!isBlocked && isCaptchaRequired)
            {
                GenerateCaptcha();
                captchaTextBox.Clear();
                captchaTextBox.Focus();
            }
        }

        // Обработчик нажатия Enter в поле логина
        private void LoginTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter && !isBlocked)
            {
                pwdTextBox.Focus();
                e.Handled = true;
            }
        }

        // Обработчик нажатия Enter в поле пароля
        private void PwdTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter && !isBlocked)
            {
                if (isCaptchaRequired)
                {
                    captchaTextBox.Focus();
                }
                else
                {
                    LogInButton_Click(sender, e);
                }
                e.Handled = true;
            }
        }

        // Обработчик нажатия Enter в поле каптчи
        private void CaptchaTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter && !isBlocked && isCaptchaRequired)
            {
                LogInButton_Click(sender, e);
                e.Handled = true;
            }
        }

        // Кнопка выхода из приложения
        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Освобождение ресурсов
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (captchaPictureBox.Image != null)
            {
                captchaPictureBox.Image.Dispose();
            }
            if (blockTimer != null)
            {
                blockTimer.Stop();
                blockTimer.Dispose();
            }
            base.OnFormClosing(e);
        }
    }

    // Класс для хранения данных текущего пользователя
    public static class UserSession
    {
        public static UserInfo CurrentUser { get; set; }

        public static void Clear()
        {
            CurrentUser = null;
        }
    }

    // Класс с информацией о пользователе
    public class UserInfo
    {
        public int UserID { get; set; }
        public string Login { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }
}