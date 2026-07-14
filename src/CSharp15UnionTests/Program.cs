using System;
public record class Cat(string Name);
public record class Dog(string Name);

// https://devblogs.microsoft.com/dotnet/csharp-15-union-types/
public union Pet(Cat, Dog);

// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns#closed-hierarchy-patterns
public closed record class PaymentMethod;
public record class Cash : PaymentMethod;
public record class Card(string Last4) : PaymentMethod;
public record class BankTransfer(string Iban) : PaymentMethod;

class Program
{
    static void Main()
    {
        Pet pet = new Cat("Whiskers");
        Console.WriteLine(pet switch
        {
            Cat c => $"Cat: {c.Name}",
            Dog d => $"Dog: {d.Name}",
        });
    }
}