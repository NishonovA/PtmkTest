using Microsoft.Data.SqlClient;

namespace PtmkTest
{
    public class Employee
    {
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; }

        public Employee(string fullName, DateTime birthDate, string gender)
        {
            FullName = fullName;
            BirthDate = birthDate;
            Gender = gender;
        }

        public int CalculateAge()
        {
            DateTime today = DateTime.Today;
            int age = today.Year - BirthDate.Year;
            if (BirthDate.Date.AddYears(age) > today) age--;
            return age;
        }

        public void SaveToDatabase(SqlConnection connection)
        {
            SqlCommand command = new SqlCommand(
                "INSERT INTO Employees (FullName, BirthDate, Gender) " +
                "VALUES (@FullName, @BirthDate, @Gender)",
                connection);

            command.Parameters.AddWithValue("@FullName", FullName);
            command.Parameters.AddWithValue("@BirthDate", BirthDate);
            command.Parameters.AddWithValue("@Gender", Gender);

            command.ExecuteNonQuery();
        }
    }
}
