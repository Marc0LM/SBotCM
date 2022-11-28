


// See https://aka.ms/new-console-template for more information
bc b = new bc();
bc d = new dc();
b.Call();
d.Call();
class bc
{
    public void Call()
    {
        M1();
    }
    public virtual void M1()
    {
        Console.WriteLine("M1");
    }
}
class dc : bc
{
    public override void M1()
    {
        Console.WriteLine("M2");
    }
}