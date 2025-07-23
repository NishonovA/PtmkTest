using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace PtmkTest
{
    class Program
    {
        private static string connectionString = "Server=DEVSMILE\\SQLEXPRESS;Database=EmployeeDirectory;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";
        private static string[] maleFirstNames = new[] { "Petr", "Stepan", "David", "Mihail", "Alexey", "Ignat", "Anatoliy" };
        private static string[] femaleFirstNames = new[] { "Mary", "Anna", "Sofia", "Eva", "Elizabeth", "Ekaterina", "Alice", "Victoria" };
        private static char[] lastNames = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static string[] middleNames = new[] { "A.", "B.", "C.", "D.", "E.", "F.", "G.", "H.", "I.", "J." };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Для работы приложения следует ввести параметр запуска (цифра от 1 до 5).");
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                switch (args[0])
                {
                    case "1":
                        CreateTable(connection);
                        break;
                    case "2":
                        CreateEmployee(connection, args[1], args[2], args[3]);
                        break;
                    case "3":
                        DisplayUniqueEmployees(connection);
                        break;
                    case "4":
                        GenerateRandomEmployees(connection);
                        break;
                    case "5":
                        FindMaleF(connection);
                        break;
                    default:
                        Console.WriteLine("Введён неверный параметр. В следующий раз введите цифру от 1 до 5.");
                        break;
                }
            }
        }

        static void CreateTable(SqlConnection connection)
        {
            SqlCommand command = new SqlCommand(
                "IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Employees') " +
                "CREATE TABLE Employees (" +
                "Id INT IDENTITY(1,1) PRIMARY KEY, " +
                "FullName NVARCHAR(50) NOT NULL, " +
                "BirthDate DATE NOT NULL, " +
                "Gender NVARCHAR(10) NOT NULL)",
                connection);

            command.ExecuteNonQuery();
            Console.WriteLine("Таблица Employees создана успешно");
        }

        static void CreateEmployee(SqlConnection connection, string inFullname, string inDate, string inGender)
        {
            string fullName = inFullname;
            DateTime birthDate = DateTime.Parse(inDate);
            string gender = inGender;

            Employee employee = new Employee(fullName, birthDate, gender);

            employee.SaveToDatabase(connection);
            Console.WriteLine($"Сотрудник {employee.FullName} сохранен в БД. Возраст: {employee.CalculateAge()} лет");
        }

        static void DisplayUniqueEmployees(SqlConnection connection)
        {
            SqlCommand command = new SqlCommand(
                "SELECT FullName, BirthDate, MIN(Gender) " +
                "FROM Employees " +
                "GROUP BY FullName, BirthDate " +
                "ORDER BY FullName",
                connection);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                Console.WriteLine("Список сотрудников:");
                Console.WriteLine("{0,-30} {1,-12} {2,-8} {3}", "ФИО", "Дата рожд.", "Пол", "Возраст");

                while (reader.Read())
                {
                    Employee employee = new Employee(reader.GetString(0), reader.GetDateTime(1), reader.GetString(2));

                    Console.WriteLine("{0,-30} {1:yyyy-MM-dd  } {2,-8} {3}",
                        employee.FullName,
                        employee.BirthDate,
                        employee.Gender,
                        employee.CalculateAge());
                }
            }
        }

        static void GenerateRandomEmployees(SqlConnection connection)
        {
            Console.WriteLine("Начало генерации 1000000 случайных сотрудников...");

            //List<Employee> employees = new List<Employee>();

            DataTable table = new DataTable();
            table.Columns.Add("FullName", typeof(string));
            table.Columns.Add("BirthDate", typeof(DateTime));
            table.Columns.Add("Gender", typeof(string));

            for (int i = 0; i < 1000000; i++)
            {
                Employee employee = GenerateRandomEmployee(isMaleF: false);

                //employee.SaveToDatabase(connection);
                //employees.Add(employee);
                table.Rows.Add(employee.FullName, employee.BirthDate, employee.Gender);
            }

            Console.WriteLine("Дополнительная генерация 100 мужчин на F...");

            for (int i = 0; i < 100; i++)
            {
                Employee employee = GenerateRandomEmployee(isMaleF: true);

                //employee.SaveToDatabase(connection);
                //employees.Add(employee);
                table.Rows.Add(employee.FullName, employee.BirthDate, employee.Gender);
            }

            using (SqlCommand command = new SqlCommand(
                "INSERT INTO Employees (FullName, BirthDate , Gender) " +
                "SELECT FullName, BirthDate, Gender FROM @table",
                connection))
            {

                SqlParameter parameter = command.Parameters.AddWithValue("@table", table);
                parameter.SqlDbType = SqlDbType.Structured;
                parameter.TypeName = "dbo.Employee";

                command.ExecuteNonQuery();
            }

            Console.WriteLine("Генерация завершена. Всего 1000100 записей добавлено.");
        }

        static Employee GenerateRandomEmployee(bool isMaleF)
        {
            Random random = new Random();

            bool isMale = isMaleF ? true : random.Next(2) == 0;

            string lastName;

            if (isMaleF)
            {
                lastName = "F" + (random.Next(2) == 0 ? "evronon" : "edenkov");
            }
            else
            {
                lastName = lastNames[random.Next(lastNames.Length)].ToString() + (random.Next(2) == 0 ? "etr" : "edenk") + (isMale ? "ov" : "ova");
            }

            string firstName = isMale
                    ? maleFirstNames[random.Next(maleFirstNames.Length)]
                    : femaleFirstNames[random.Next(femaleFirstNames.Length)];

            string middleName = middleNames[random.Next(middleNames.Length)];

            string fullName = $"{lastName} {firstName} {middleName}";

            DateTime birthDate = new DateTime(random.Next(1950, 2010), random.Next(1, 13), random.Next(1, 28));

            string gender = isMale ? "Male" : "Female";

            Employee employee = new Employee(fullName, birthDate, gender);

            return employee;
        }

        static void FindMaleF(SqlConnection connection)
        {
            Console.WriteLine("Выполнение выборки: Male с фамилией на F...");

            var stopwatch = Stopwatch.StartNew();

            var command = new SqlCommand(
                "SELECT FullName, BirthDate, Gender " +
                "FROM Employees " +
                "WHERE Gender = 'Male' AND FullName LIKE 'F%'",
                connection);

            int count = 0;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {

                    Console.WriteLine($"{reader.GetString(0)}, " +
                                    $"{reader.GetDateTime(1):yyyy-MM-dd}, {reader.GetString(2)}");
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Найдено {count} записей. Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
        }
    }
}