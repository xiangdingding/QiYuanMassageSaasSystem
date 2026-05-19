using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;

namespace MassageSaas.Cs.Services.Devices.EscPos;

/// <summary>
/// 把字节流送到打印机。每次打印"开-写-关"，不长期占用端口——
/// 打印机掉电再上电也能继续，端口被占用时也只影响单次打印。
/// </summary>
public interface IReceiptTransport
{
    void Send(byte[] data);
}

/// <summary>串口（COM）打印机传输。</summary>
public sealed class SerialReceiptTransport : IReceiptTransport
{
    private readonly string _port;
    private readonly int _baud;

    public SerialReceiptTransport(string port, int baud)
    {
        _port = port;
        _baud = baud;
    }

    public void Send(byte[] data)
    {
        using var sp = new SerialPort(_port, _baud, Parity.None, 8, StopBits.One)
        {
            WriteTimeout = 5000,
            Handshake = Handshake.None
        };
        sp.Open();
        sp.Write(data, 0, data.Length);
        // 给打印机留出把缓冲区吐完的时间，避免 Dispose 过早关闭端口
        Thread.Sleep(120);
    }
}

/// <summary>网口（TCP，常见 9100 端口）打印机传输。</summary>
public sealed class NetworkReceiptTransport : IReceiptTransport
{
    private readonly string _host;
    private readonly int _port;

    public NetworkReceiptTransport(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public void Send(byte[] data)
    {
        using var client = new TcpClient { SendTimeout = 5000 };
        client.Connect(_host, _port);
        using var stream = client.GetStream();
        stream.Write(data, 0, data.Length);
        stream.Flush();
    }
}
