namespace Ion8600ClockSync.Services.Dnp3;

public enum Dnp3FunctionCode : byte
{
    Read = 1,
    Write = 2,
    DelayMeasure = 23,
    Response = 129,
}

public sealed class Dnp3ResponseException(string message) : Exception(message);

public readonly record struct Dnp3Indications(bool NeedTime, bool DeviceRestart, byte Iin1, byte Iin2)
{
    private const byte NeedTimeBit = 0x10;
    private const byte DeviceRestartBit = 0x80;

    public static Dnp3Indications FromBytes(byte iin1, byte iin2) => new(
        NeedTime: (iin1 & NeedTimeBit) != 0,
        DeviceRestart: (iin1 & DeviceRestartBit) != 0,
        Iin1: iin1,
        Iin2: iin2);
}

public static class Dnp3ApplicationLayer
{
    private const byte TransportFirstFinalSeqZero = 0xC0;
    private const byte GroupTimeAndDate = 50;
    private const byte VariationTimeAndDate = 1;
    private const byte GroupTimeDelay = 52;
    private const byte VariationTimeDelayFine = 2;

    public static byte[] BuildDelayMeasureRequest(byte sequence)
    {
        Span<byte> fragment = stackalloc byte[2];
        fragment[0] = AppControlByte(sequence);
        fragment[1] = (byte)Dnp3FunctionCode.DelayMeasure;
        return PrependTransportHeader(fragment);
    }

    public static byte[] BuildReadTimeAndDateRequest(byte sequence)
    {
        Span<byte> fragment = stackalloc byte[5];
        fragment[0] = AppControlByte(sequence);
        fragment[1] = (byte)Dnp3FunctionCode.Read;
        fragment[2] = GroupTimeAndDate;
        fragment[3] = VariationTimeAndDate;
        fragment[4] = 0x06;
        return PrependTransportHeader(fragment);
    }

    public static byte[] BuildWriteTimeAndDateRequest(byte sequence, DateTimeOffset utcTime)
    {
        Span<byte> fragment = stackalloc byte[12];
        fragment[0] = AppControlByte(sequence);
        fragment[1] = (byte)Dnp3FunctionCode.Write;
        fragment[2] = GroupTimeAndDate;
        fragment[3] = VariationTimeAndDate;
        fragment[4] = 0x07;
        fragment[5] = 1;
        long ms = utcTime.ToUnixTimeMilliseconds();
        for (int i = 0; i < 6; i++)
            fragment[6 + i] = (byte)(ms >> (8 * i));
        return PrependTransportHeader(fragment);
    }

    public static int ParseDelayMeasureResponse(ReadOnlySpan<byte> frameUserData, out Dnp3Indications indications)
    {
        var fragment = StripTransportHeader(frameUserData);
        int dataOffset = ValidateResponseAndGetObjectOffset(fragment, out indications);
        var header = ReadObjectHeader(fragment, dataOffset)
            ?? throw new Dnp3ResponseException("Resposta de Delay Measurement sem objeto reconhecível.");

        if (header.Group != GroupTimeDelay || header.Variation != VariationTimeDelayFine)
            throw new Dnp3ResponseException($"Objeto inesperado na resposta de Delay Measurement: group={header.Group} var={header.Variation}.");

        if (header.DataOffset + 2 > fragment.Length)
            throw new Dnp3ResponseException("Resposta de Delay Measurement truncada.");

        return fragment[header.DataOffset] | (fragment[header.DataOffset + 1] << 8);
    }

    public static DateTimeOffset ParseTimeAndDateResponse(ReadOnlySpan<byte> frameUserData, out Dnp3Indications indications)
    {
        var fragment = StripTransportHeader(frameUserData);
        int dataOffset = ValidateResponseAndGetObjectOffset(fragment, out indications);
        var header = ReadObjectHeader(fragment, dataOffset)
            ?? throw new Dnp3ResponseException("Resposta de Time and Date sem objeto reconhecível.");

        if (header.Group != GroupTimeAndDate || header.Variation != VariationTimeAndDate)
            throw new Dnp3ResponseException($"Objeto inesperado na resposta de Time and Date: group={header.Group} var={header.Variation}.");

        if (header.DataOffset + 6 > fragment.Length)
            throw new Dnp3ResponseException("Resposta de Time and Date truncada.");

        long ms = 0;
        for (int i = 0; i < 6; i++)
            ms |= (long)fragment[header.DataOffset + i] << (8 * i);

        return DateTimeOffset.FromUnixTimeMilliseconds(ms);
    }

    public static void ValidateWriteResponse(ReadOnlySpan<byte> frameUserData, out Dnp3Indications indications)
    {
        var fragment = StripTransportHeader(frameUserData);
        ValidateResponseAndGetObjectOffset(fragment, out indications);
    }

    private static byte AppControlByte(byte sequence) => (byte)(0xC0 | (sequence & 0x0F));

    private static byte[] PrependTransportHeader(ReadOnlySpan<byte> appFragment)
    {
        var result = new byte[appFragment.Length + 1];
        result[0] = TransportFirstFinalSeqZero;
        appFragment.CopyTo(result.AsSpan(1));
        return result;
    }

    private static ReadOnlySpan<byte> StripTransportHeader(ReadOnlySpan<byte> frameUserData)
    {
        if (frameUserData.Length < 1)
            throw new Dnp3ResponseException("Frame DNP3 sem cabeçalho de pseudo-transporte.");

        return frameUserData[1..];
    }

    private static int ValidateResponseAndGetObjectOffset(ReadOnlySpan<byte> fragment, out Dnp3Indications indications)
    {
        if (fragment.Length < 4)
            throw new Dnp3ResponseException("Fragmento de aplicação DNP3 truncado.");

        if (fragment[1] != (byte)Dnp3FunctionCode.Response)
            throw new Dnp3ResponseException($"Function code inesperado na resposta: {fragment[1]}.");

        byte iin1 = fragment[2];
        byte iin2 = fragment[3];
        indications = Dnp3Indications.FromBytes(iin1, iin2);

        const byte ObjectUnknownOrParameterError = 0x06; // IIN2 bits 1 (OBJECT_UNKNOWN) e 2 (PARAMETER_ERROR)
        if ((iin2 & ObjectUnknownOrParameterError) != 0)
            throw new Dnp3ResponseException($"Medidor reportou erro na resposta DNP3 (IIN2=0x{iin2:X2}).");

        return 4;
    }

    private readonly record struct ObjectHeaderInfo(byte Group, byte Variation, int DataOffset);

    private static ObjectHeaderInfo? ReadObjectHeader(ReadOnlySpan<byte> fragment, int offset)
    {
        if (offset + 3 > fragment.Length)
            return null;

        byte group = fragment[offset];
        byte variation = fragment[offset + 1];
        byte qualifier = fragment[offset + 2];

        int headerLength = qualifier switch
        {
            0x06 => 3,
            0x07 => 4,
            0x00 => 5,
            0x01 => 7,
            _ => -1,
        };

        if (headerLength < 0 || offset + headerLength > fragment.Length)
            return null;

        return new ObjectHeaderInfo(group, variation, offset + headerLength);
    }
}
