namespace Ion8600ClockSync.Services.Dnp3;

public static class Dnp3DataLinkFrame
{
    public const byte Sync1 = 0x05;
    public const byte Sync2 = 0x64;
    private const byte PrimaryUnconfirmedUserData = 0xC4;
    private const int MaxUserDataLength = 250;

    public static byte[] Build(ushort destination, ushort source, ReadOnlySpan<byte> userData)
    {
        if (userData.Length > MaxUserDataLength)
            throw new ArgumentException($"DNP3 data link frame suporta no máximo {MaxUserDataLength} bytes de dados.", nameof(userData));

        Span<byte> header = stackalloc byte[8];
        header[0] = Sync1;
        header[1] = Sync2;
        header[2] = (byte)(5 + userData.Length);
        header[3] = PrimaryUnconfirmedUserData;
        header[4] = (byte)(destination & 0xFF);
        header[5] = (byte)(destination >> 8);
        header[6] = (byte)(source & 0xFF);
        header[7] = (byte)(source >> 8);
        ushort headerCrc = Dnp3Crc.Compute(header);

        using var ms = new MemoryStream();
        ms.Write(header);
        ms.WriteByte((byte)(headerCrc & 0xFF));
        ms.WriteByte((byte)(headerCrc >> 8));

        int offset = 0;
        while (offset < userData.Length)
        {
            int blockLen = Math.Min(16, userData.Length - offset);
            var block = userData.Slice(offset, blockLen);
            ms.Write(block);
            ushort blockCrc = Dnp3Crc.Compute(block);
            ms.WriteByte((byte)(blockCrc & 0xFF));
            ms.WriteByte((byte)(blockCrc >> 8));
            offset += blockLen;
        }

        return ms.ToArray();
    }

    public static int GetTotalFrameLength(byte lengthField)
    {
        int userDataLength = lengthField - 5;
        if (userDataLength < 0)
            throw new InvalidDataException("Campo length do frame DNP3 inválido.");

        return 10 + DataBlockOverhead(userDataLength);
    }

    public static byte[] ParseUserData(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 10 || frame[0] != Sync1 || frame[1] != Sync2)
            throw new InvalidDataException("Frame DNP3 inválido: sync bytes ausentes.");

        var header = frame[..8];
        ushort headerCrcReceived = (ushort)(frame[8] | (frame[9] << 8));
        if (Dnp3Crc.Compute(header) != headerCrcReceived)
            throw new InvalidDataException("CRC do header DNP3 inválido.");

        int userDataLength = header[2] - 5;
        if (userDataLength < 0)
            throw new InvalidDataException("Campo length do frame DNP3 inválido.");

        var result = new byte[userDataLength];
        int resultOffset = 0;
        int readOffset = 10;
        while (resultOffset < userDataLength)
        {
            int blockLen = Math.Min(16, userDataLength - resultOffset);
            if (readOffset + blockLen + 2 > frame.Length)
                throw new InvalidDataException("Frame DNP3 truncado.");

            var block = frame.Slice(readOffset, blockLen);
            ushort blockCrcReceived = (ushort)(frame[readOffset + blockLen] | (frame[readOffset + blockLen + 1] << 8));
            if (Dnp3Crc.Compute(block) != blockCrcReceived)
                throw new InvalidDataException("CRC de bloco de dados DNP3 inválido.");

            block.CopyTo(result.AsSpan(resultOffset));
            resultOffset += blockLen;
            readOffset += blockLen + 2;
        }

        return result;
    }

    private static int DataBlockOverhead(int userDataLength)
    {
        if (userDataLength <= 0) return 0;
        int fullBlocks = userDataLength / 16;
        int remainder = userDataLength % 16;
        return userDataLength + fullBlocks * 2 + (remainder > 0 ? 2 : 0);
    }
}
