using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendSmart.Models;
using System.Globalization;

namespace SpendSmart.Controllers
{
    public class DashboardController : Controller
    {
        private readonly SpendSmartDbContext _context;

        public DashboardController(SpendSmartDbContext context)
        {
            _context = context;
        }
        
        public async Task<ActionResult> Index()
        {
            //last 7 days
            DateTime startDate = DateTime.Today.AddDays(-6);
            DateTime endDate = DateTime.Today;

            List<Transaction> selectedTransactions = await _context.Transactions.Include(x => x.Category).Where(y => y.Date >= startDate && y.Date <= endDate).ToListAsync();

            //total income
            int totalIncome = selectedTransactions.Where(i => i.Category.Type == "Income").Sum(j => j.Amount);
            ViewBag.TotalIncome = totalIncome.ToString("$0");

            //total expense
            int totalExpense = selectedTransactions.Where(i => i.Category.Type == "Expense").Sum(j => j.Amount);
            ViewBag.TotalExpense = totalExpense.ToString("$0");

            //balance
            int balance = totalIncome - totalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:$0}", balance);

            //expense by category
            ViewBag.DoughnutChartData = selectedTransactions.Where(i => i.Category.Type == "Expense").GroupBy(j => j.Category.CategoryId).Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("$0"),
                }).OrderByDescending(l => l.amount).ToList();

            //income vs expense

            //income
            List<SplineChartData> incomeSummary = selectedTransactions.Where(i => i.Category.Type == "Income").GroupBy(j => j.Date).Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                }).ToList();

            //expense
            List<SplineChartData> ExpenseSummary = selectedTransactions.Where(i => i.Category.Type == "Expense").GroupBy(j => j.Date).Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                }).ToList();

            //income & expense
            string[] Last7Days = Enumerable.Range(0, 7).Select(i => startDate.AddDays(i).ToString("dd-MMM")).ToArray();

            ViewBag.SplineChartData = from day in Last7Days
                                      join income in incomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };
            //recent transactions
            ViewBag.RecentTransactions = await _context.Transactions.Include(i => i.Category).OrderByDescending(j => j.Date).Take(5).ToListAsync();
           
            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;

    }
}
