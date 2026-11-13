using Microsoft.EntityFrameworkCore;

namespace HrPractice;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Location { get; set; } = "";
    public List<Employee> Employees { get; set; } = [];
    public override string ToString() => Name;
}
public class Position
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Salary { get; set; }
    public List<Employee> Employees { get; set; } = [];
    public override string ToString() => Name;
}
public class Employee
{
    public int Id { get; set; }
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public DateTime HireDate { get; set; } = DateTime.Today;
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
}
public class AppUser
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool IsAdmin { get; set; }
}
public class HrDb : DbContext
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AppUser> Users => Set<AppUser>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite($"Data Source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "hr-practice-v3.sqlite")}");
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Department>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Position>().HasIndex(x => x.Name).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.Login).IsUnique();
        b.Entity<Employee>().HasOne(x => x.Department).WithMany(x => x.Employees).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Employee>().HasOne(x => x.Position).WithMany(x => x.Employees).HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Seed()
    {
        Database.EnsureCreated();
        if (!Users.Any()) Users.AddRange(new (string login, string name, bool admin)[] {
            ("isivanov", "Иванов Иван Сергеевич", true),
            ("papetrov", "Петров Пётр Алексеевич", false),
            ("asidorova", "Сидорова Анна Дмитриевна", false),
            ("ekuznetsova", "Кузнецова Екатерина Андреевна", false),
            ("dsmirnov", "Смирнов Дмитрий Владимирович", false),
            ("apopov", "Попов Алексей Николаевич", true),
            ("ovasileva", "Васильева Ольга Игоревна", false),
            ("psokolov", "Соколов Павел Викторович", false),
            ("tmikhailova", "Михайлова Татьяна Юрьевна", false),
            ("nnovikova", "Новикова Наталья Сергеевна", false),
            ("afedorov", "Фёдоров Андрей Максимович", true),
            ("smorozov", "Морозов Сергей Павлович", false),
            ("jvolkova", "Волкова Юлия Александровна", false),
            ("aalekseev", "Алексеев Артём Евгеньевич", false),
            ("ilebedeva", "Лебедева Ирина Владимировна", false),
            ("negorov", "Егоров Никита Денисович", false),
            ("epavlova", "Павлова Елена Петровна", false),
            ("rkozlov", "Козлов Роман Олегович", true),
            ("vstepanova", "Степанова Виктория Романовна", false),
            ("vnikolaev", "Николаев Владимир Иванович", false)
        }.Select(x => new AppUser { Login = x.login, FullName = x.name, IsAdmin = x.admin }));
        if (!Departments.Any()) Departments.AddRange(
            new Department { Name = "Бухгалтерия", Location = "Кабинет 201" },
            new Department { Name = "Отдел кадров", Location = "Кабинет 102" });
        if (!Positions.Any()) Positions.AddRange(
            new Position { Name = "Бухгалтер", Salary = 65000 },
            new Position { Name = "Специалист по кадрам", Salary = 60000 });
        SaveChanges();
        if (!Employees.Any())
        {
            Employees.Add(new Employee { LastName = "Сидорова", FirstName = "Анна", MiddleName = "Дмитриевна", DepartmentId = Departments.First(x => x.Name == "Отдел кадров").Id, PositionId = Positions.First(x => x.Name == "Специалист по кадрам").Id });
            SaveChanges();
        }
    }
}
