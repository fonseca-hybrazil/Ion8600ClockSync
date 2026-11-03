namespace Ion8600ClockSync.Services.Dnp3;

public static class Dnp3Crc
{
    private const ushort ReflectedPolynomial = 0xA6BC;

    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0x0000;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int bit = 0; bit < 8; bit++)
            {
                if ((crc & 0x0001) != 0)
                    crc = (ushort)((crc >> 1) ^ ReflectedPolynomial);
                else
                    crc >>= 1;
            }
        }
        return (ushort)~crc;
    }
}
