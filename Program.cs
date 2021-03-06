﻿
using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter.Nova;

[Database]
public class Person
{
    // We need to declare database fields using properties with
    // auto-implemented getter and setter.
    //
    // They must also be public and virtual.
    public virtual string FirstName { get; set; }

    // Adding the [Index] attribute to a database field
    // will cause an index to be created on it if needed.
    [Index]
    public virtual string LastName { get; set; }

    // This property won't be stored in the database since it fails
    // the requirements listed above (it's not writeable).
    public string FullName => FirstName + " " + LastName;
}

[Database]
public class Spender : Person
{
    // A Db.SQL result is an IEnumerable<T> over the database class instances.
    public IEnumerable<Expense> Expenses
        => Db.SQL<Expense>("SELECT e FROM Expense e WHERE e.Spender = ?", this);

    // The new QP implementation currently can't do SUM(), but this is fast enough.
    public decimal CurrentDebt => Expenses.Sum(e => e.Amount);
}

[Database]
public class Expense
{
    [Index]
    public virtual Spender Spender { get; set; }
    public virtual decimal Amount { get; set; }
}

class Program
{
    public static void Main(string[] args)
    {
        string dbname = args.Length > 0 ? args[0] : "Program_LocalDb";

        // Make sure we have a database, create one if not.
        if (!Starcounter.Nova.Options.StarcounterOptions.TryOpenExisting(dbname))
        {
            System.IO.Directory.CreateDirectory(dbname);
            Starcounter.Nova.Bluestar.ScCreateDb.Execute(dbname);
        }

        using (var appHost = new Starcounter.Nova.Hosting.AppHostBuilder()
            .UseDatabase(dbname)
            .Build())
        {
            // Start the app host inside the using so that
            // we get cleanup if an exception occurs.
            appHost.Start();

            // We are now connected to the given database
            // and are free to access it.
            Db.Transact(() =>
            {
                if (Db.SQL<Spender>("SELECT s FROM Spender s").First == null)
                {
                    // Creates a new instance of Spender in the database
                    var p = Db.Insert<Spender>();
                    p.FirstName = "John";
                    p.LastName = "Doe";
                }
            });

            Db.Transact(() =>
            {
                foreach (var p in Db.SQL<Spender>("SELECT s FROM Spender s"))
                {
                    Console.WriteLine("Found Spender {0} with debt balance {1}",
                        p.FullName, p.CurrentDebt);
                }
            });
        }
    }
}