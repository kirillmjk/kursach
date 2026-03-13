using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Kursovaya
{
    public partial class DbImportForm : Form
    {
        private int currentUserId;
        private string connectionString = ConnectionString.GetConnectionString();

        public DbImportForm()
        {
            InitializeComponent();
            if (UserSession.CurrentUser != null)
            {
                currentUserId = UserSession.CurrentUser.UserID;
            }
            LoadTableList();
        }

        // Загрузка списка таблиц из базы
        private void LoadTableList()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable schema = connection.GetSchema("Tables");
                    foreach (DataRow row in schema.Rows)
                    {
                        string tableName = row["TABLE_NAME"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка таблиц: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportFullDatabase()
        {
            try
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Выберите папку для сохранения структуры и данных базы данных";

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        var builder = new MySqlConnectionStringBuilder(connectionString);
                        string databaseName = builder.Database;

                        if (string.IsNullOrEmpty(databaseName))
                        {
                            MessageBox.Show("Не удалось определить имя базы данных из строки подключения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        string fileName = $"{databaseName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
                        string fullPath = Path.Combine(folderDialog.SelectedPath, fileName);

                        using (var connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();

                            using (var writer = new StreamWriter(fullPath, false, Encoding.UTF8))
                            {
                                writer.WriteLine($"-- MySQL Backup: {databaseName} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                                writer.WriteLine($"-- Host: {builder.Server} Database: {builder.Database}");
                                writer.WriteLine("/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;");
                                writer.WriteLine("/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;\n");

                                // Получаем список таблиц
                                var tables = new List<string>();
                                using (var cmd = new MySqlCommand($"SELECT table_name FROM information_schema.tables WHERE table_schema = '{databaseName}' AND table_type = 'BASE TABLE'", connection))
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read()) tables.Add(reader.GetString(0));
                                }

                                foreach (var table in tables)
                                {
                                    writer.WriteLine($"--");
                                    writer.WriteLine($"-- Table structure for table `{table}`");
                                    writer.WriteLine($"--");
                                    writer.WriteLine($"DROP TABLE IF EXISTS `{table}`;");

                                    // Структура таблицы
                                    using (var cmd = new MySqlCommand($"SHOW CREATE TABLE `{table}`", connection))
                                    using (var reader = cmd.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            string createTable = reader.GetString(1);
                                            writer.WriteLine(createTable + ";");
                                        }
                                    }

                                    writer.WriteLine();
                                    writer.WriteLine($"--");
                                    writer.WriteLine($"-- Dumping data for table `{table}`");
                                    writer.WriteLine($"--");
                                    writer.WriteLine($"LOCK TABLES `{table}` WRITE;");
                                    writer.WriteLine($"/*!40000 ALTER TABLE `{table}` DISABLE KEYS */;");

                                    // Данные таблицы
                                    using (var cmd = new MySqlCommand($"SELECT * FROM `{table}`", connection))
                                    using (var reader = cmd.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            var values = new List<string>();
                                            for (int i = 0; i < reader.FieldCount; i++)
                                            {
                                                if (reader.IsDBNull(i))
                                                    values.Add("NULL");
                                                else
                                                {
                                                    var val = reader.GetValue(i);
                                                    var fieldType = reader.GetFieldType(i);

                                                    if (fieldType == typeof(string))
                                                        values.Add($"'{MySqlHelper.EscapeString(val.ToString())}'");
                                                    else if (fieldType == typeof(DateTime))
                                                    {
                                                        // Форматируем дату в правильный формат для MySQL: YYYY-MM-DD HH:MM:SS
                                                        DateTime dt = (DateTime)val;
                                                        values.Add($"'{dt:yyyy-MM-dd HH:mm:ss}'");
                                                    }
                                                    else if (fieldType == typeof(bool))
                                                        values.Add((bool)val ? "1" : "0");
                                                    else if (fieldType == typeof(byte[]))
                                                        values.Add($"0x{BitConverter.ToString((byte[])val).Replace("-", "")}");
                                                    else
                                                        values.Add(val.ToString().Replace(',', '.'));
                                                }
                                            }
                                            writer.WriteLine($"INSERT INTO `{table}` VALUES ({string.Join(", ", values)});");
                                        }
                                    }

                                    writer.WriteLine($"/*!40000 ALTER TABLE `{table}` ENABLE KEYS */;");
                                    writer.WriteLine($"UNLOCK TABLES;");
                                    writer.WriteLine();
                                }

                                writer.WriteLine("/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;");
                                writer.WriteLine("/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;");
                                writer.WriteLine($"-- Backup completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                            }
                        }

                        MessageBox.Show($"База данных экспортирована:\n{fullPath}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportSQLFile()
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Выберите SQL файл для импорта";
                    openFileDialog.Filter = "SQL files (*.sql)|*.sql|All files (*.*)|*.*";
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = openFileDialog.FileName;

                        DialogResult q = MessageBox.Show(
                            "ВНИМАНИЕ!\nИмпорт базы данных может перезаписать существующие данные.\n" +
                            "Рекомендуется сделать резервную копию перед импортом.\n\n" +
                            "Вы действительно хотите выполнить импорт?",
                            "Подтверждение",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (q == DialogResult.Yes)
                        {
                            // Читаем файл и удаляем BOM если есть
                            string sql = ReadFileAndRemoveBOM(filePath);

                            using (var conn = new MySqlConnection(connectionString))
                            {
                                conn.Open();

                                // Временно отключаем проверку внешних ключей
                                using (var cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", conn))
                                {
                                    cmd.ExecuteNonQuery();
                                }

                                // Также отключаем уникальные проверки для ускорения
                                using (var cmd = new MySqlCommand("SET UNIQUE_CHECKS = 0;", conn))
                                {
                                    cmd.ExecuteNonQuery();
                                }

                                int commandCount = 0;
                                int errorCount = 0;
                                List<string> errorMessages = new List<string>();

                                // Разделяем SQL на команды
                                string[] sqlCommands = sql.Split(new[] { ";\r\n", ";\n", ";" }, StringSplitOptions.RemoveEmptyEntries);

                                foreach (string command in sqlCommands)
                                {
                                    string trimmedCommand = command.Trim();

                                    // Пропускаем пустые команды и комментарии
                                    if (string.IsNullOrWhiteSpace(trimmedCommand) ||
                                        trimmedCommand.StartsWith("--") ||
                                        trimmedCommand.StartsWith("/*") ||
                                        trimmedCommand.StartsWith("#"))
                                        continue;

                                    try
                                    {
                                        using (var cmd = new MySqlCommand(trimmedCommand, conn))
                                        {
                                            cmd.CommandTimeout = 300; // 5 минут
                                            cmd.ExecuteNonQuery();
                                            commandCount++;
                                        }
                                    }
                                    catch (MySqlException ex)
                                    {
                                        errorCount++;

                                        // Игнорируем ошибки дублирования ключей и существования таблиц
                                        if (ex.Number == 1050 || ex.Number == 1062) // 1050: Table already exists, 1062: Duplicate entry
                                        {
                                            // Просто логируем, но не показываем как ошибку
                                            Console.WriteLine($"Предупреждение (код {ex.Number}): {ex.Message}");
                                        }
                                        else
                                        {
                                            errorMessages.Add($"Ошибка {ex.Number} в команде: {trimmedCommand.Substring(0, Math.Min(50, trimmedCommand.Length))}... - {ex.Message}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        errorMessages.Add($"Ошибка в команде: {trimmedCommand.Substring(0, Math.Min(50, trimmedCommand.Length))}... - {ex.Message}");
                                    }
                                }

                                // Включаем обратно проверки
                                using (var cmd = new MySqlCommand("SET UNIQUE_CHECKS = 1;", conn))
                                {
                                    cmd.ExecuteNonQuery();
                                }

                                using (var cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", conn))
                                {
                                    cmd.ExecuteNonQuery();
                                }

                                if (errorMessages.Count == 0)
                                {
                                    MessageBox.Show($"Импорт базы данных успешно выполнен!\nВыполнено команд: {commandCount}",
                                        "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                else
                                {
                                    string errorText = string.Join("\n", errorMessages.Take(5));
                                    MessageBox.Show($"Импорт завершен с ошибками.\nВыполнено команд: {commandCount}\nПропущено предупреждений: {errorCount - errorMessages.Count}\nОшибок: {errorMessages.Count}\n\nПервые 5 ошибок:\n{errorText}",
                                        "Завершено с ошибками", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Новая функция для чтения файла и удаления BOM
        private string ReadFileAndRemoveBOM(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);

            // Проверяем наличие BOM для UTF-8 (EF BB BF)
            if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
            {
                // Пропускаем первые 3 байта (BOM)
                return Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3);
            }
            // Проверяем наличие BOM для UTF-16 LE (FF FE)
            else if (fileBytes.Length >= 2 && fileBytes[0] == 0xFF && fileBytes[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(fileBytes, 2, fileBytes.Length - 2);
            }
            // Проверяем наличие BOM для UTF-16 BE (FE FF)
            else if (fileBytes.Length >= 2 && fileBytes[0] == 0xFE && fileBytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode.GetString(fileBytes, 2, fileBytes.Length - 2);
            }
            else
            {
                // Если BOM нет, просто читаем как UTF-8
                return Encoding.UTF8.GetString(fileBytes);
            }
        }

        private void btnExportFullDB_Click(object sender, EventArgs e)
        {
            ExportFullDatabase();
        }

        private void btnImportSQL_Click(object sender, EventArgs e)
        {
            ImportSQLFile();
        }

        // Кнопка возврата
        private void btnBack_Click(object sender, EventArgs e)
        {
            if (UserSession.CurrentUser != null)
            {
                AdminForm adminForm = new AdminForm(currentUserId);
                adminForm.Show();
                this.Close();
            }
            else
            {
                Auth authForm = new Auth();
                authForm.Show();
                this.Close();
            }
        }
    }
}