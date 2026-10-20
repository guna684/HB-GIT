using System;

class Program
{
    static void Main()
    {
        string hash = BCrypt.Net.BCrypt.HashPassword("123456");
        Console.WriteLine("HASH:" + hash);
    }
}
