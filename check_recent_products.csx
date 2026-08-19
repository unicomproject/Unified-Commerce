using System;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;
using E_POS.Infrastructure.Persistence;

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlite(@"Data Source=c:\Users\user\Desktop\E-Pos\Unified-Commerce\e-pos.db");
using var context = new ApplicationDbContext(optionsBuilder.Options);

var recentProducts = context.Products
    .OrderByDescending(p => p.CreatedAt)
    .Take(5)
    .ToList();

Console.WriteLine("Recent 5 products in DB:");
foreach (var p in recentProducts)
{
    Console.WriteLine($"ID: {p.Id}, Code: {p.ProductCode}, Name: {p.ProductName}, Status: {p.Status}, CreatedAt: {p.CreatedAt}");
}
