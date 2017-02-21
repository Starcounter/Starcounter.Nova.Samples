﻿
using System;
using Starcounter.Core;
using Starcounter.Core.Abstractions.Database;
using Starcounter.Core.Hosting;

namespace HelloWorldCore
{
    [Database]
    public class Person
    {
        // NOTE! We need to declare database class fields using
        // public virtual properties.
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
    }

    [Database]
    public class Spender : Person
    {
        public ISQLResult<Expense> Spendings =>
            Db.SQL<Expense>("SELECT e FROM Expense e WHERE e.Spender = ?", this);

        public decimal CurrentBalance =>
            Db.SQL<decimal>("SELECT SUM(e.Amount) FROM Expense e WHERE e.Spender = ?", this).First;
    }

    [Database]
    public class Expense
    {
        // NOTE! Adding the [Index] attribute to a property
        // will cause an index to be created on it if needed.
        [Index]
        public virtual Spender Spender { get; set; }
        public virtual decimal Amount { get; set; }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            using (var appHost = new AppHostBuilder()
                .AddCommandLine(args)
                .Build())
            {
                // Start the app host inside the using so that
                // we get cleanup if an exception occurs.
                appHost.Start();

                // We are now connected to the given database
                // and are free to access it.
                Db.Transact(() =>
                {
                    var anyone = Db.SQL<Spender>("SELECT s FROM Spender s").First;
                    if (anyone == null)
                    {
                        // NOTE! We must use Db.Insert<T>() instead of new T()
                        var p = Db.Insert<Spender>();
                        p.FirstName = "John";
                        p.LastName = "Doe";
                    }
                });
                
                Db.Transact(() =>
                {
                    foreach (var p in Db.SQL<Spender>("SELECT s FROM Spender s"))
                    {
                        Console.WriteLine("Found Spender {0} {1} with balance {2}",
                            p.FirstName, p.LastName, p.CurrentBalance);
                    }
                });
            }
        }
    }
}
