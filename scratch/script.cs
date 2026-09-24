using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using E_POS.Infrastructure.Persistence;

var options = new DbContextOptionsBuilder<EPosDbContext>()
    .UseNpgsql("Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin")
    .Options;

using var db = new EPosDbContext(options, null, null);
var logs = db.HardwareTestLogs
    .OrderByDescending(x => x.TestedAt)
    .Take(3)
    .ToList();

foreach(var log in logs) {
    Console.WriteLine($"TestStatus: {log.TestStatus}, TestedAt: {log.TestedAt}, Result: {log.ResultPayloadJson}");
}
