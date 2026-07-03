using System;
public record class Cat(string Name);
public record class Dog(string Name);

// https://devblogs.microsoft.com/dotnet/csharp-15-union-types/
public union Pet(Cat, Dog);

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

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
        AllowMultiple = false)]
    public sealed class UnionAttribute : Attribute;

    public interface IUnion
    {
        object? Value { get; }
    }
}
