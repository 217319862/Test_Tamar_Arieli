using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

// Library fines report.
// Layers: Program (input/output wiring) -> LoanParser (parsing + validation)
//         -> FineCalculator (rules) -> ReportBuilder (aggregation) -> ReportPrinter (output).
class Program
{
    static void Main(string[] args)
    {
        // Hebrew is printed as UTF-8, also on Windows
        Console.OutputEncoding = new UTF8Encoding(false);
        string path = args.Length > 0 ? args[0] : FindSample();
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        int reportDay = ReadReportDay(lines);

        LoanParser.Parse(lines, out List<Loan> loans, out List<string> errors);
        Console.Error.WriteLine("read " + Path.GetFileName(path) + " - report day " + reportDay + " - " + loans.Count + " valid, " + errors.Count + " errors");

        Report report = ReportBuilder.Build(loans, errors, reportDay);
        ReportPrinter.Print(report);
    }

    static int ReadReportDay(string[] lines)
    {
        return int.Parse(lines[0].Split(':')[1].Trim(), CultureInfo.InvariantCulture);
    }

    // Without a path, look for data/sample.txt next to the project, also when started from Visual Studio
    static string FindSample()
    {
        DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "data", "sample.txt");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return Path.Combine("data", "sample.txt");
    }
}

// ---------------------------------------------------------------- model

enum MemberType { Student, Teacher, Guest }

sealed class Loan
{
    public string ReaderId { get; }
    public string Name { get; }
    public MemberType Type { get; }
    public string Book { get; }
    public int LoanDay { get; }
    public int? ReturnDay { get; }   // null = not returned yet

    public Loan(string readerId, string name, MemberType type, string book, int loanDay, int? returnDay)
    {
        ReaderId = readerId;
        Name = name;
        Type = type;
        Book = book;
        LoanDay = loanDay;
        ReturnDay = returnDay;
    }

    public bool IsReturned { get { return ReturnDay.HasValue; } }

    // Return day minus loan day; for a book not returned: report day minus loan day
    public int DaysOut(int reportDay)
    {
        return (ReturnDay ?? reportDay) - LoanDay;
    }
}

sealed class ReaderSummary
{
    public string ReaderId { get; }
    public string Name { get; }
    public int TotalFine { get; set; }
    public int FinedLoans { get; set; }   // loans whose fine is above 0

    public ReaderSummary(string readerId, string name)
    {
        ReaderId = readerId;
        Name = name;
    }
}

sealed class Report
{
    public List<ReaderSummary> Readers = new List<ReaderSummary>();
    public List<ReaderSummary> Flagged = new List<ReaderSummary>();
    public List<string> Errors = new List<string>();
    public int Total;
    public List<KeyValuePair<string, int>> TopBooks = new List<KeyValuePair<string, int>>();
    public List<ReaderSummary> AtRisk = new List<ReaderSummary>();
}

// ---------------------------------------------------------------- parsing

static class LoanParser
{
    // Lines 0 and 1 are the report day and the header. Blank lines are skipped, bad lines go to 'errors' as they are.
    public static void Parse(string[] lines, out List<Loan> loans, out List<string> errors)
    {
        loans = new List<Loan>();
        errors = new List<string>();
        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Trim() == "") continue;
            if (TryParseLine(line, out Loan loan)) loans.Add(loan);
            else errors.Add(line);
        }
    }

    static bool TryParseLine(string line, out Loan loan)
    {
        loan = null;
        string[] f = line.Split(',');
        if (f.Length != 6) return false;
        for (int i = 0; i < f.Length; i++) f[i] = f[i].Trim();

        // every field except return_day is required
        for (int i = 0; i < 5; i++)
            if (f[i] == "") return false;

        if (!TryParseType(f[2], out MemberType type)) return false;
        if (!TryParseDay(f[4], out int loanDay)) return false;

        int? returnDay = null;
        if (f[5] != "")
        {
            if (!TryParseDay(f[5], out int ret)) return false;
            if (ret < loanDay) return false;
            returnDay = ret;
        }

        loan = new Loan(f[0], f[1], type, f[3], loanDay, returnDay);
        return true;
    }

    static bool TryParseDay(string text, out int day)
    {
        // digits only: no sign, no spaces, no decimals
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out day);
    }

    static bool TryParseType(string text, out MemberType type)
    {
        switch (text)
        {
            case "student": type = MemberType.Student; return true;
            case "teacher": type = MemberType.Teacher; return true;
            case "guest": type = MemberType.Guest; return true;
            default: type = MemberType.Student; return false;
        }
    }
}

// ---------------------------------------------------------------- business rules

static class FineCalculator
{
    const int StudentFreeDays = 14;
    const int StudentMaxFine = 30;
    const int GuestFreeDays = 7;
    const int TeacherCheckDays = 60;

    public static int Fine(Loan loan, int reportDay)
    {
        int days = loan.DaysOut(reportDay);
        switch (loan.Type)
        {
            case MemberType.Student:
                int late = days - StudentFreeDays;
                if (late <= 1) return 0;                    // no fine, and one late day is forgiven
                return Math.Min(late, StudentMaxFine);      // 1 per late day, capped per loan
            case MemberType.Guest:
                int guestLate = days - GuestFreeDays;
                if (guestLate <= 0) return 0;
                return (5 * guestLate + 1) / 2;             // ceil(2.5 * guestLate) in integer math
            default:
                return 0;                                   // teacher
        }
    }

    // A teacher who has not returned the book after more than 60 days
    public static bool IsFlagged(Loan loan, int reportDay)
    {
        return loan.Type == MemberType.Teacher
            && !loan.IsReturned
            && loan.DaysOut(reportDay) > TeacherCheckDays;
    }
}

// ---------------------------------------------------------------- aggregation

static class ReportBuilder
{
    const int TopBooksCount = 3;
    const int AtRiskMinFinedLoans = 3;   // more than two loans with a fine

    public static Report Build(List<Loan> loans, List<string> errors, int reportDay)
    {
        Report report = new Report();
        report.Errors.AddRange(errors);

        Dictionary<string, ReaderSummary> byId = new Dictionary<string, ReaderSummary>();
        HashSet<string> flaggedIds = new HashSet<string>();

        foreach (Loan loan in loans)
        {
            if (!byId.TryGetValue(loan.ReaderId, out ReaderSummary reader))
            {
                reader = new ReaderSummary(loan.ReaderId, loan.Name);
                byId[loan.ReaderId] = reader;
                report.Readers.Add(reader);   // keeps first-appearance order
            }

            int fine = FineCalculator.Fine(loan, reportDay);
            reader.TotalFine += fine;
            if (fine > 0) reader.FinedLoans++;
            report.Total += fine;

            if (FineCalculator.IsFlagged(loan, reportDay) && flaggedIds.Add(loan.ReaderId))
                report.Flagged.Add(reader);
        }

        report.AtRisk = report.Readers.Where(r => r.FinedLoans >= AtRiskMinFinedLoans).ToList();
        report.TopBooks = TopBooks(loans);
        return report;
    }

    // Most borrowed first; ties by book name (ordinal order = Hebrew alphabet order)
    static List<KeyValuePair<string, int>> TopBooks(List<Loan> loans)
    {
        return loans
            .GroupBy(l => l.Book)
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            .OrderByDescending(p => p.Value)
            .ThenBy(p => p.Key, StringComparer.Ordinal)
            .Take(TopBooksCount)
            .ToList();
    }
}

// ---------------------------------------------------------------- output

static class ReportPrinter
{
    public static void Print(Report report)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("FINES");
        foreach (ReaderSummary r in report.Readers)
            sb.AppendLine(r.ReaderId + "," + r.Name + "," + r.TotalFine);

        sb.AppendLine("FLAGGED");
        foreach (ReaderSummary r in report.Flagged)
            sb.AppendLine(r.ReaderId + "," + r.Name);

        sb.AppendLine("ERRORS");
        foreach (string line in report.Errors)
            sb.AppendLine(line);

        sb.AppendLine("TOTAL," + report.Total);

        sb.AppendLine("TOP_BOOKS");
        foreach (KeyValuePair<string, int> p in report.TopBooks)
            sb.AppendLine(p.Key + "," + p.Value);

        sb.AppendLine("AT_RISK");
        foreach (ReaderSummary r in report.AtRisk)
            sb.AppendLine(r.ReaderId + "," + r.Name);

        Console.Write(sb.ToString());
    }
}
