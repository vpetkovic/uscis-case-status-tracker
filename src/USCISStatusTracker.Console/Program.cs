using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using USCIS.Common;

#region Configure Services
IConfiguration config = new ConfigurationBuilder()
    .SetBasePath(Environment.CurrentDirectory)
    .AddJsonFile("appsettings.json", true, true)
    .AddUserSecrets<Program>()
    .Build();

var services = new ServiceCollection()
    .AddSingleton<IDataAccess, DataAccess>(_ => new DataAccess(config["AzureTableStorage:ConnectionString"], config["AzureTableStorage:TableName"]))
    .AddSingleton(config);

services.AddHttpClient<StatusTrackerClient>(c =>
{
    c.BaseAddress = new Uri(config["USCISRequestUrl"]);
});
#endregion


using var scope = services.BuildServiceProvider().CreateScope();

var appSettings = scope.ServiceProvider.GetService<IConfiguration>();
if (appSettings!.GetSection("Cases").Exists() && appSettings.GetSection("Cases").GetChildren().Any())
{
    var uscisClient = scope.ServiceProvider.GetService<StatusTrackerClient>();
    var cases = appSettings.GetSection("Cases").GetChildren().Select(x => new Receipt() { ReceiptNumber = x.Value });

    var table = new Table()
        .Title("CASE STATUS CHECK")
        .AddColumn("Last Check").AddColumn("Receipt#").AddColumn("Status").AddColumn("Description");

    (await uscisClient!.GetReceiptStatusAsync(cases, persistCaseStatus:appSettings.GetValue<bool>("AzureTableStorage:Enabled")))
        .ForEach(r => table.AddRow(
            @$"[yellow]{DateTime.SpecifyKind(r.LastChecked, DateTimeKind.Utc).ToLocalTime().ToString("G")}[/]",
            $"{r.ReceiptNumber}{Environment.NewLine}",
            @$"{r.ToStatusString(colored:true)} {r.CurrentStatus}",
            $"{r.CurrentStatusDetails}{Environment.NewLine}"
        ));

    AnsiConsole.Write(table);

    if (appSettings.GetValue<bool>("KeepConsoleOpen")) Console.ReadLine();
}
else
{
    AnsiConsole.MarkupLine("[yellow]At least one receipt # must be provided. Check 'Cases' property in [blue]appsettings.json[/][/]");
}












