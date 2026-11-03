using Ion8600ClockSync.Services.Dnp3;
using Xunit;

namespace Ion8600ClockSync.Tests;

public class Dnp3DataLinkFrameTests
{
    [Fact]
    public void Build_HeaderCrcCoversSyncBytesPlusLengthControlDestSource()
    {
        // Frame de referência verificado publicamente (Reset Link States, destino=1, origem=1024):
        // "05 64 05 C0 01 00 00 04 E9 21" — CRC do header = E9 21 (no fio). Esse teste existe porque
        // o bug original calculava o CRC do header só sobre length+control+dest+source (6 bytes),
        // sem os 2 bytes de sync 05/64 — o que fazia o medidor real descartar todo frame enviado
        // sem nenhuma resposta (CRC sempre incorreto do ponto de vista do medidor).
        byte[] referenceHeaderWithSyncBytes = { 0x05, 0x64, 0x05, 0xC0, 0x01, 0x00, 0x00, 0x04 };

        ushort crc = Dnp3Crc.Compute(referenceHeaderWithSyncBytes);

        Assert.Equal(0x21E9, crc);
    }


    [Theory]
    [InlineData(2)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(40)]
    public void BuildThenParseUserData_RoundTripsForVariousPayloadSizes(int payloadLength)
    {
        var userData = new byte[payloadLength];
        for (int i = 0; i < payloadLength; i++)
            userData[i] = (byte)(i + 1);

        var frame = Dnp3DataLinkFrame.Build(destination: 100, source: 1, userData);

        int totalLength = Dnp3DataLinkFrame.GetTotalFrameLength(frame[2]);
        Assert.Equal(frame.Length, totalLength);

        var parsed = Dnp3DataLinkFrame.ParseUserData(frame);

        Assert.Equal(userData, parsed);
    }

    [Fact]
    public void ParseUserData_ThrowsWhenHeaderCrcIsCorrupted()
    {
        var frame = Dnp3DataLinkFrame.Build(destination: 100, source: 1, new byte[] { 0x01, 0x02 });
        frame[8] ^= 0xFF;

        Assert.Throws<InvalidDataException>(() => Dnp3DataLinkFrame.ParseUserData(frame));
    }

    [Fact]
    public void ParseUserData_ThrowsWhenBlockCrcIsCorrupted()
    {
        var frame = Dnp3DataLinkFrame.Build(destination: 100, source: 1, new byte[] { 0x01, 0x02, 0x03 });
        frame[^1] ^= 0xFF;

        Assert.Throws<InvalidDataException>(() => Dnp3DataLinkFrame.ParseUserData(frame));
    }
}
