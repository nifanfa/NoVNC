using System.Net;
using System.Net.Sockets;

public class websockify : WebSocketBehavior
{
    public static IPEndPoint vnc_server = null;
    Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

    protected override void OnOpen()
    {
        try
        {
            byte[] buffer = new byte[ushort.MaxValue];
            socket.Connect(vnc_server);

            socket.BeginReceive(
                buffer,
                0,
                buffer.Length,
                SocketFlags.None,
                ReceiveCallback,
                buffer
                );

            Program.WriteLine($"WS#({this.UserEndPoint})# Opened", ConsoleColor.DarkGreen);
        }
        catch
        {
            Program.WriteLine($"#Unable to connect to VNC server#", ConsoleColor.DarkRed);
            socket.Close();
            this.Close();
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        byte[] buffer = (byte[])ar.AsyncState;

        try
        {
            int length = socket.EndReceive(ar);
            if (!this.IsAlive) throw new InvalidOperationException();
            this.Send(new Span<byte>(buffer, 0, length).ToArray());

            socket.BeginReceive(
                buffer,
                0,
                buffer.Length,
                SocketFlags.None,
                ReceiveCallback,
                buffer
                );
        }
        catch
        {
            Program.WriteLine($"WS#({this.UserEndPoint})# Closed", ConsoleColor.DarkRed);
            socket.Close();
            this.Close();
        }
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        try
        {
            socket.Send(e.RawData);
        }
        catch
        {
            Program.WriteLine($"WS#({this.UserEndPoint})# Closed", ConsoleColor.DarkRed);
            socket.Close();
            this.Close();
        }
    }
}
