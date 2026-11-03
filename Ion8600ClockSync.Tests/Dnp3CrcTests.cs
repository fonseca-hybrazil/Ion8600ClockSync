using System.Text;
using Ion8600ClockSync.Services.Dnp3;
using Xunit;

namespace Ion8600ClockSync.Tests;

public class Dnp3CrcTests
{
    [Fact]
    public void Compute_ChecksAgainstPublishedCrc16DnpCheckValue()
    {
        var input = Encoding.ASCII.GetBytes("123456789");

        ushort crc = Dnp3Crc.Compute(input);

        // Valor de referência "82EA" (ordem de transmissão, LSB primeiro) publicado para CRC-16/DNP
        // em "123456789" (ex: libcrc). Como Dnp3Crc.Compute devolve o CRC como inteiro numérico
        // (MSB primeiro), o valor esperado aqui é o mesmo com os bytes trocados: 0xEA82.
        Assert.Equal(0xEA82, crc);
    }
}
