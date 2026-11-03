using System.Net.Sockets;

namespace Ion8600ClockSync.Services.Dnp3;

public sealed class Dnp3Client(string host, int port, ushort outstationAddress, ushort masterAddress) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private byte _sequence;

    public Dnp3Indications LastIndications { get; private set; }

    public async Task<int> MeasureDelayMsAsync(CancellationToken ct)
    {
        var request = Dnp3ApplicationLayer.BuildDelayMeasureRequest(NextSequence());
        var responseUserData = await SendAndReceiveAsync(request, ct);
        int delayMs = Dnp3ApplicationLayer.ParseDelayMeasureResponse(responseUserData, out var indications);
        LastIndications = indications;
        return delayMs;
    }

    public async Task WriteTimeAsync(DateTimeOffset utcTime, CancellationToken ct)
    {
        var request = Dnp3ApplicationLayer.BuildWriteTimeAndDateRequest(NextSequence(), utcTime);
        var responseUserData = await SendAndReceiveAsync(request, ct);
        Dnp3ApplicationLayer.ValidateWriteResponse(responseUserData, out var indications);
        LastIndications = indications;
    }

    public async Task<DateTimeOffset> ReadTimeAsync(CancellationToken ct)
    {
        var request = Dnp3ApplicationLayer.BuildReadTimeAndDateRequest(NextSequence());
        var responseUserData = await SendAndReceiveAsync(request, ct);
        var time = Dnp3ApplicationLayer.ParseTimeAndDateResponse(responseUserData, out var indications);
        LastIndications = indications;
        return time;
    }

    private byte NextSequence() => _sequence = (byte)((_sequence + 1) & 0x0F);

    private async Task<byte[]> SendAndReceiveAsync(byte[] applicationFragment, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var stream = await EnsureConnectedAsync(ct);
            var frame = Dnp3DataLinkFrame.Build(outstationAddress, masterAddress, applicationFragment);
            await stream.WriteAsync(frame, ct);

            var header = new byte[3];
            await ReadExactAsync(stream, header, ct);
            if (header[0] != Dnp3DataLinkFrame.Sync1 || header[1] != Dnp3DataLinkFrame.Sync2)
                throw new InvalidDataException("Resposta DNP3 sem sync bytes esperados.");

            int totalLength = Dnp3DataLinkFrame.GetTotalFrameLength(header[2]);
            var fullFrame = new byte[totalLength];
            header.CopyTo(fullFrame, 0);
            await ReadExactAsync(stream, fullFrame.AsMemory(3, totalLength - 3), ct);

            return Dnp3DataLinkFrame.ParseUserData(fullFrame);
        }
        catch
        {
            DropConnection();
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task ReadExactAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken ct)
    {
        int read = 0;
        while (read < buffer.Length)
        {
            int n = await stream.ReadAsync(buffer[read..], ct);
            if (n == 0)
                throw new IOException("Conexão DNP3 encerrada pelo medidor antes da resposta completa.");
            read += n;
        }
    }

    private async Task<NetworkStream> EnsureConnectedAsync(CancellationToken ct)
    {
        if (_tcpClient is { Connected: true } && _stream is not null)
            return _stream;

        DropConnection();
        var client = new TcpClient();
        await client.ConnectAsync(host, port, ct);
        _tcpClient = client;
        _stream = client.GetStream();
        return _stream;
    }

    private void DropConnection()
    {
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _stream = null;
        _tcpClient = null;
    }

    public async ValueTask DisposeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            DropConnection();
        }
        finally
        {
            _lock.Release();
        }
    }
}
