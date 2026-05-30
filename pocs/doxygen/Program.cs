using System;

namespace work;

public class Program : IProgram {

    private int privateIntExample;

    public int publicIntExample;

    public Program()
    {

    }

    public static void Main(String[] args ){
        Console.WriteLine("Hello, World!");
    }

    public int GetIntegerValue()
    {
        return 1;
    }
}
