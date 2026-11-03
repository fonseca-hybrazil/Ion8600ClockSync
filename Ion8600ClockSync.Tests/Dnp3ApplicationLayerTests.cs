using Ion8600ClockSync.Services.Dnp3;
using Xunit;

namespace Ion8600ClockSync.Tests;

public class Dnp3ApplicationLayerTests
{
    [Fact]
    public void BuildDelayMeasureRequest_MatchesExpectedByteLayout()
    {
        var request = Dnp3ApplicationLayer.BuildDelayMeasureRequest(sequence: 5);

        Assert.Equal(new byte[] { 0xC0, 0xC5, 0x17 }, request);
    }

    [Fact]
    public void BuildReadTimeAndDateRequest_MatchesExpectedByteLayout()
    {
        var request = Dnp3ApplicationLayer.BuildReadTimeAndDateRequest(sequence: 0);

        Assert.Equal(new byte[] { 0xC0, 0xC0, 0x01, 50, 1, 0x06 }, request);
    }

    [Fact]
    public void BuildWriteTimeAndDateRequest_MatchesExpectedByteLayout()
    {
        var utc = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000);

        var request = Dnp3ApplicationLayer.BuildWriteTimeAndDateRequest(sequence: 0, utc);

        var expectedHeader = new byte[] { 0xC0, 0xC0, 0x02, 50, 1, 0x07, 0x01 };
        Assert.Equal(expectedHeader, request[..7]);

        long decodedMs = 0;
        for (int i = 0; i < 6; i++)
            decodedMs |= (long)request[7 + i] << (8 * i);

        Assert.Equal(1_700_000_000_000, decodedMs);
    }

    [Fact]
    public void ParseDelayMeasureResponse_ReadsTimeDelayFineValue()
    {
        byte[] response = { 0xC0, 0xC1, 129, 0x00, 0x00, 52, 2, 0x07, 0x01, 0xFA, 0x00 };

        int delayMs = Dnp3ApplicationLayer.ParseDelayMeasureResponse(response, out var indications);

        Assert.Equal(250, delayMs);
        Assert.False(indications.NeedTime);
        Assert.False(indications.DeviceRestart);
    }

    [Fact]
    public void ParseDelayMeasureResponse_DetectsNeedTimeAndDeviceRestartBits()
    {
        // IIN1 = 0x90 = NEED_TIME (0x10) | DEVICE_RESTART (0x80)
        byte[] response = { 0xC0, 0xC1, 129, 0x90, 0x00, 52, 2, 0x07, 0x01, 0xFA, 0x00 };

        Dnp3ApplicationLayer.ParseDelayMeasureResponse(response, out var indications);

        Assert.True(indications.NeedTime);
        Assert.True(indications.DeviceRestart);
    }

    [Fact]
    public void ParseTimeAndDateResponse_ReadsAbsoluteTimeValue()
    {
        const long expectedMs = 1_700_000_000_000;
        byte[] response = new byte[14];
        response[0] = 0xC0;
        response[1] = 0xC1;
        response[2] = 129;
        response[3] = 0x00;
        response[4] = 0x00;
        response[5] = 50;
        response[6] = 1;
        response[7] = 0x06;
        for (int i = 0; i < 6; i++)
            response[8 + i] = (byte)(expectedMs >> (8 * i));

        var result = Dnp3ApplicationLayer.ParseTimeAndDateResponse(response, out _);

        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(expectedMs), result);
    }

    [Fact]
    public void ValidateWriteResponse_AcceptsNullResponseWithoutObjects()
    {
        byte[] response = { 0xC0, 0xC1, 129, 0x00, 0x00 };

        Dnp3ApplicationLayer.ValidateWriteResponse(response, out _);
    }

    [Fact]
    public void ValidateWriteResponse_ThrowsWhenMeterReportsError()
    {
        // IIN2 = 0x02 = OBJECT_UNKNOWN
        byte[] response = { 0xC0, 0xC1, 129, 0x00, 0x02 };

        Assert.Throws<Dnp3ResponseException>(() => Dnp3ApplicationLayer.ValidateWriteResponse(response, out _));
    }
}
