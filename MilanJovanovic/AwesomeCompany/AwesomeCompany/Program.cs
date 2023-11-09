using AwesomeCompany;
using AwesomeCompany.Entities;
using AwesomeCompany.Models;
using AwesomeCompany.Options;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureOptions<DatabaseOptionsSetup>();

builder.Services.AddDbContext<DatabaseContext>(
    (serviceProvider, dbContextOptionsBuilder) =>
    {
        var databaseOptions = serviceProvider.GetService<IOptions<DatabaseOptions>>()!.Value;

        var connectionString = builder.Configuration.GetConnectionString("Database");

        dbContextOptionsBuilder.UseSqlServer(databaseOptions.ConnectionString, sqlServerAction =>
        {
            sqlServerAction.EnableRetryOnFailure(databaseOptions.MaxRertyCount);

            sqlServerAction.CommandTimeout(databaseOptions.CommandTimeout);
        });

        dbContextOptionsBuilder.EnableDetailedErrors(databaseOptions.EnableDetailedErrors);

        dbContextOptionsBuilder.EnableSensitiveDataLogging(databaseOptions.EnableSensitiveDataLogging);
    });

var app = builder.Build();
app.UseHttpsRedirection();

app.MapGet("companies/{companyId:int}", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
        .Set<Company>()
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id '{companyId}' was not found.");
    }

    var response = new CompanyResponse(company.Id, company.Name);

    return Results.Ok(response);
});

app.MapPut("increae-salaries", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
        .Set<Company>()
        .Include(c => c.Employees)
        .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id '{companyId}' was not found.");
    }

    foreach (var employee in company.Employees)
    {
        employee.Salary *= 1.1m;
    }

    company.LastSalaryUpdateUtc = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});

app.MapPut("increae-salaries-sql", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
        .Set<Company>()
        .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id '{companyId}' was not found.");
    }

    await dbContext.Database.BeginTransactionAsync();

    await dbContext.Database.ExecuteSqlInterpolatedAsync(
        $"UPDATE Employees SET Salary = Salary * 1.1 WHERE CompanyId = {company.Id}");

    company.LastSalaryUpdateUtc = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    await dbContext.Database.CommitTransactionAsync();

    return Results.NoContent();
});

app.MapPut("increae-salaries-sql-dapper", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
        .Set<Company>()
        .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id '{companyId}' was not found.");
    }

    var transaction = await dbContext.Database.BeginTransactionAsync();

    await dbContext.Database.GetDbConnection().ExecuteAsync(
        "UPDATE Employees" +
        "SET Salary = Salary * 1.1" +
        "WHERE CompanyId = @CompanyId",
        new { CompanyId = company.Id },
        transaction.GetDbTransaction());

    company.LastSalaryUpdateUtc = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    await dbContext.Database.CommitTransactionAsync();

    return Results.NoContent();
});

app.Run();
