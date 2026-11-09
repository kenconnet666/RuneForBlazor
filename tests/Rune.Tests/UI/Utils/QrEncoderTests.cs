using System.Security.Cryptography;
using System.Text;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 二维码编码器：Reed–Solomon 按 ISO/IEC 18004 附录 I 的例子、格式与版本信息按标准的表、容量与校正图形位置（同 vue-tsx-pro 的测试）；
/// 整个矩阵与 vue-tsx-pro 的编码器逐模块一致（期望值由它生成：版本、掩码与全部模块的 SHA-256 前 16 位）。
/// </summary>
public class QrEncoderTests
{
    static readonly Dictionary<QrErrorCorrection, string[]> FormatTable = new()
    {
        [QrErrorCorrection.L] = ["111011111000100", "111001011110011", "111110110101010", "111100010011101", "110011000101111", "110001100011000", "110110001000001", "110100101110110"],
        [QrErrorCorrection.M] = ["101010000010010", "101000100100101", "101111001111100", "101101101001011", "100010111111001", "100000011001110", "100111110010111", "100101010100000"],
        [QrErrorCorrection.Q] = ["011010101011111", "011000001101000", "011111100110001", "011101000000110", "010010010110100", "010000110000011", "010111011011010", "010101111101101"],
        [QrErrorCorrection.H] = ["001011010001001", "001001110111110", "001110011100111", "001100111010000", "000011101100010", "000001001010101", "000110100001100", "000100000111011"],
    };

    [Fact]
    public void ReedSolomon按标准附录I与HELLO_WORLD的例子()
    {
        Assert.Equal([0xa5, 0x24, 0xd4, 0xc1, 0xed, 0x36, 0xc7, 0x87, 0x2c, 0x55],
            QrEncoder.ReedSolomon([16, 32, 12, 86, 97, 128, 236, 17, 236, 17, 236, 17, 236, 17, 236, 17], 10));
        Assert.Equal([196, 35, 39, 119, 235, 215, 231, 226, 93, 23],
            QrEncoder.ReedSolomon([32, 91, 11, 120, 209, 114, 220, 77, 67, 64, 236, 17, 236, 17, 236, 17], 10));
    }

    [Fact]
    public void 格式信息与版本信息与标准的表一致()
    {
        foreach (var (ecc, table) in FormatTable)
            for (var mask = 0; mask < 8; mask++)
                Assert.Equal(table[mask], Convert.ToString(QrEncoder.FormatBits(ecc, mask), 2).PadLeft(15, '0'));
        Assert.Equal([0x07c94, 0x085bc, 0x15683, 0x28c69], new[] { 7, 8, 21, 40 }.Select(QrEncoder.VersionBits));
    }

    [Fact]
    public void 容量与校正图形的位置()
    {
        QrErrorCorrection[] levels = [QrErrorCorrection.L, QrErrorCorrection.M, QrErrorCorrection.Q, QrErrorCorrection.H];
        Assert.Equal([19, 16, 13, 9], levels.Select(ecc => QrEncoder.DataCodewords(1, ecc)));
        Assert.Equal([2956, 2334, 1666, 1276], levels.Select(ecc => QrEncoder.DataCodewords(40, ecc)));
        Assert.Equal(216, QrEncoder.DataCodewords(10, QrErrorCorrection.M));
        Assert.Empty(QrEncoder.AlignmentPositions(1));
        Assert.Equal([6, 18], QrEncoder.AlignmentPositions(2));
        Assert.Equal([6, 22, 38], QrEncoder.AlignmentPositions(7));
        Assert.Equal([6, 34, 60, 86, 112, 138], QrEncoder.AlignmentPositions(32));
        Assert.Equal([6, 30, 58, 86, 114, 142, 170], QrEncoder.AlignmentPositions(40));
    }

    [Fact]
    public void 选最小的版本_放不下时抛异常()
    {
        Assert.Equal(1, QrEncoder.Encode(new string('a', 17), QrErrorCorrection.L).Version);
        Assert.Equal(2, QrEncoder.Encode(new string('a', 18), QrErrorCorrection.L).Version);
        Assert.Equal(40, QrEncoder.Encode(new string('a', 2953), QrErrorCorrection.L).Version);
        Assert.Throws<ArgumentException>(() => QrEncoder.Encode(new string('a', 2954), QrErrorCorrection.L));
        Assert.Equal(37, QrEncoder.Encode("a", minVersion: 5).Size);
    }

    [Theory]
    [InlineData("https://example.com", QrErrorCorrection.M, null, 2, 2, "2017c4b935bf9ab7")]
    [InlineData("二维码 Rune", QrErrorCorrection.H, null, 2, 6, "9339649a14441271")]
    [InlineData("x*300", QrErrorCorrection.Q, null, 16, 0, "c63fcbb3a85eab0e")]
    [InlineData("供应商对账单 2026-11 *12", QrErrorCorrection.Q, null, 17, 4, "b20695475fe69144")]
    [InlineData("长内容*120", QrErrorCorrection.H, null, 37, 1, "92ea15baec1beea1")]
    [InlineData("x*400", QrErrorCorrection.L, 3, 13, 3, "8cd36069388abcd4")]
    public void 与vue的编码器逐模块一致(string spec, QrErrorCorrection ecc, int? mask, int version, int chosenMask, string hash)
    {
        // “文字*次数”表示重复
        var star = spec.LastIndexOf('*');
        var text = star < 0 ? spec : string.Concat(Enumerable.Repeat(spec[..star], int.Parse(spec[(star + 1)..])));
        var qr = QrEncoder.Encode(text, ecc, mask: mask);
        var bits = new StringBuilder();
        for (var y = 0; y < qr.Size; y++)
            for (var x = 0; x < qr.Size; x++)
                bits.Append(qr.IsDark(x, y) ? '1' : '0');
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(bits.ToString())))[..16];
        Assert.Equal((version, chosenMask, hash), (qr.Version, qr.Mask, digest));
    }
}
