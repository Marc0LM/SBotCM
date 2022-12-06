
using WMPLib;


internal class Program
{
    
    private static void Main(string[] args)
    {
        for (int i = 0; i < 9; i++)
        {
            new bc().Call();
        }
        bc b = new bc();
        bc d = new dc();
        b.Call();
        d.Call();
        Console.Read();
    }
}

class bc
{
    static WindowsMediaPlayer officerWarningPlayer = new();
    Task? t;
    public void Call()
    {
        if (t == null)
        {
            t=Task.Run(() =>
            {
                officerWarningPlayer.settings.setMode("loop", true);
                officerWarningPlayer.URL = "w_officer.mp3";
                officerWarningPlayer.controls.play();
            });
        }
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