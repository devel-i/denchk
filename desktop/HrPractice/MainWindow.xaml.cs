using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace HrPractice;

public partial class MainWindow : Window
{
    private readonly HrDb db = new();
    private AppUser? currentUser;
    private List<Employee> allEmployees = [];

    public MainWindow()
    {
        InitializeComponent();
        try { db.Seed(); }
        catch (Exception ex) { ShowError("Не удалось открыть базу данных", ex); }
    }
    private void ShowError(string message, Exception? ex = null) =>
        MessageBox.Show(message + (ex == null ? ". Проверьте введённые данные." : $": {ex.GetBaseException().Message}"), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    private bool Confirm(string what) => MessageBox.Show($"Удалить {what}? Действие нельзя отменить.", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    private bool Admin()
    {
        if (currentUser?.IsAdmin == true) return true;
        ShowError("Редактирование доступно только администратору");
        return false;
    }
    private void Login_Click(object sender, RoutedEventArgs e)
    {
        string login = LoginBox.Text.Trim();
        currentUser = db.Users.AsNoTracking().FirstOrDefault(x => x.Login == login);
        if (currentUser == null) { ShowError("Логин не найден в списке пользователей"); return; }
        LoginPanel.Visibility = Visibility.Collapsed;
        WorkPanel.Visibility = Visibility.Visible;
        EditTab.Visibility = currentUser.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        UserNameText.Text = currentUser.FullName;
        Title = "Отдел кадров — " + (currentUser.IsAdmin ? "администратор" : "пользователь");
        Refresh();
    }
    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        currentUser = null;
        LoginBox.Clear();
        SearchBox.Clear();
        Tabs.SelectedIndex = 0;
        WorkPanel.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;
        Title = "Отдел кадров — вход";
    }
    private void Refresh()
    {
        db.ChangeTracker.Clear();
        allEmployees = db.Employees.AsNoTracking().Include(x => x.Department).Include(x => x.Position).ToList();
        var departments = db.Departments.AsNoTracking().OrderBy(x => x.Name).ToList();
        var positions = db.Positions.AsNoTracking().OrderBy(x => x.Name).ToList();
        DepartmentGrid.ItemsSource = departments;
        PositionGrid.ItemsSource = positions;
        DepartmentChoice.ItemsSource = departments;
        PositionChoice.ItemsSource = positions;
        int selected = (FilterBox.SelectedItem as Department)?.Id ?? 0;
        FilterBox.ItemsSource = new[] { new Department { Id = 0, Name = "Все" } }.Concat(departments).ToList();
        FilterBox.SelectedItem = ((List<Department>)FilterBox.ItemsSource).FirstOrDefault(x => x.Id == selected) ?? ((List<Department>)FilterBox.ItemsSource)[0];
        ApplyFilter();
    }
    private void Filter_Changed(object sender, EventArgs e) => ApplyFilter();
    private void ApplyFilter()
    {
        if (EmployeeGrid == null || CountText == null) return;
        IEnumerable<Employee> result = allEmployees;
        int departmentId = (FilterBox?.SelectedItem as Department)?.Id ?? 0;
        if (departmentId > 0) result = result.Where(x => x.DepartmentId == departmentId);
        string query = SearchBox?.Text.Trim() ?? "";
        if (query.Length > 0) result = result.Where(x => x.LastName.Contains(query, StringComparison.CurrentCultureIgnoreCase) || x.FirstName.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        result = SortBox?.SelectedIndex switch { 1 => result.OrderBy(x => x.Position.Salary), 2 => result.OrderByDescending(x => x.Position.Salary), _ => result.OrderBy(x => x.LastName) };
        var rows = result.ToList();
        EmployeeGrid.ItemsSource = rows;
        CountText.Text = $"Всего: {allEmployees.Count}; показано: {rows.Count}" + (rows.Count == 0 ? ". Ничего не найдено." : "");
    }
    private bool TextOk(params string[] values) => values.All(x => !string.IsNullOrWhiteSpace(x) && x.Trim().Length <= 100);
    private bool SalaryOk(out decimal salary)
    {
        if (decimal.TryParse(PositionSalary.Text.Trim(), out salary) && salary >= 0 && salary <= 10000000) return true;
        ShowError("Оклад должен быть числом от 0 до 10 000 000"); return false;
    }
    private void Execute(Action action)
    {
        if (!Admin()) return;
        try { action(); db.SaveChanges(); Refresh(); }
        catch (Exception ex) { db.ChangeTracker.Clear(); ShowError("Операция не выполнена", ex); }
    }
    private void DepartmentGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DepartmentGrid.SelectedItem is Department d) { DepartmentName.Text = d.Name; DepartmentLocation.Text = d.Location; }
    }
    private void PositionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PositionGrid.SelectedItem is Position p) { PositionName.Text = p.Name; PositionSalary.Text = p.Salary.ToString(); }
    }
    private void EmployeeGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EmployeeGrid.SelectedItem is not Employee x) return;
        LastNameBox.Text = x.LastName; FirstNameBox.Text = x.FirstName; MiddleNameBox.Text = x.MiddleName;
        DepartmentChoice.SelectedItem = ((IEnumerable<Department>)DepartmentChoice.ItemsSource).FirstOrDefault(d => d.Id == x.DepartmentId);
        PositionChoice.SelectedItem = ((IEnumerable<Position>)PositionChoice.ItemsSource).FirstOrDefault(p => p.Id == x.PositionId);
    }
    private void AddDepartment_Click(object s, RoutedEventArgs e) => Execute(() =>
    {
        if (!TextOk(DepartmentName.Text, DepartmentLocation.Text)) throw new ArgumentException("Укажите название и расположение (до 100 символов)");
        db.Departments.Add(new Department { Name = DepartmentName.Text.Trim(), Location = DepartmentLocation.Text.Trim() });
    });
    private void EditDepartment_Click(object s, RoutedEventArgs e) => Execute(() =>
    {
        if (DepartmentGrid.SelectedItem is not Department selected) throw new ArgumentException("Выберите подразделение");
        if (!TextOk(DepartmentName.Text, DepartmentLocation.Text)) throw new ArgumentException("Укажите название и расположение (до 100 символов)");
        var item = db.Departments.Find(selected.Id)!;
        item.Name = DepartmentName.Text.Trim(); item.Location = DepartmentLocation.Text.Trim();
    });
    private void DeleteDepartment_Click(object s, RoutedEventArgs e) => Execute(() =>
    {
        if (DepartmentGrid.SelectedItem is not Department selected) throw new ArgumentException("Выберите подразделение");
        if (db.Employees.Any(x => x.DepartmentId == selected.Id)) throw new InvalidOperationException("В подразделении есть сотрудники. Сначала переведите или удалите их");
        if (Confirm($"подразделение «{selected.Name}»")) db.Departments.Remove(db.Departments.Find(selected.Id)!);
    });
    private void AddPosition_Click(object s, RoutedEventArgs e) => Execute(() =>
    {
        if (!TextOk(PositionName.Text)) throw new ArgumentException("Укажите название должности (до 100 символов)");
        if (!SalaryOk(out decimal salary)) return;
        db.Positions.Add(new Position { Name = PositionName.Text.Trim(), Salary = salary });
    });
    private void EditPosition_Click(object s, RoutedEventArgs e) => Execute(() =>
    {
        if (PositionGrid.SelectedItem is not Position selected) throw new ArgumentException("Выберите должность");
        if (!TextOk(PositionName.Text)) throw new ArgumentException("Укажите название должности (до 100 символов)");
        if (!SalaryOk(out decimal salary)) return;
        var item = db.Positions.Find(selected.Id)!; item.Name = PositionName.Text.Trim(); item.Salary = salary;
    });
    private void DeletePosition_Click(object s, RoutedEventArgs e) => Execute(() =>
    {
        if (PositionGrid.SelectedItem is not Position selected) throw new ArgumentException("Выберите должность");
        if (db.Employees.Any(x => x.PositionId == selected.Id)) throw new InvalidOperationException("Должность назначена сотрудникам. Сначала измените их должности");
        if (Confirm($"должность «{selected.Name}»")) db.Positions.Remove(db.Positions.Find(selected.Id)!);
    });
    private Employee ReadEmployee()
    {
        if (!TextOk(LastNameBox.Text, FirstNameBox.Text, MiddleNameBox.Text)) throw new ArgumentException("Заполните ФИО, каждое поле до 100 символов");
        if (DepartmentChoice.SelectedItem is not Department d || PositionChoice.SelectedItem is not Position p) throw new ArgumentException("Выберите подразделение и должность");
        return new Employee { LastName = LastNameBox.Text.Trim(), FirstName = FirstNameBox.Text.Trim(), MiddleName = MiddleNameBox.Text.Trim(), DepartmentId = d.Id, PositionId = p.Id };
    }
    private void AddEmployee_Click(object s, RoutedEventArgs e) => Execute(() => db.Employees.Add(ReadEmployee()));
    private void EditEmployee_Click(object s, RoutedEventArgs e) => Execute(() =>
    {
        if (EmployeeGrid.SelectedItem is not Employee selected) throw new ArgumentException("Выберите сотрудника на первой вкладке");
        var input = ReadEmployee(); var item = db.Employees.Find(selected.Id)!;
        item.LastName = input.LastName; item.FirstName = input.FirstName; item.MiddleName = input.MiddleName;
        item.DepartmentId = input.DepartmentId; item.PositionId = input.PositionId;
    });
    private void DeleteEmployee_Click(object s, RoutedEventArgs e) => Execute(() =>
    {
        if (EmployeeGrid.SelectedItem is not Employee selected) throw new ArgumentException("Выберите сотрудника на первой вкладке");
        if (Confirm($"сотрудника {selected.FullName}")) db.Employees.Remove(db.Employees.Find(selected.Id)!);
    });
}
