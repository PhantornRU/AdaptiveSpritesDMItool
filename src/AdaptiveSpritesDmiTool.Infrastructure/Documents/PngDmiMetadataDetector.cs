using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace AdaptiveSpritesDmiTool.Infrastructure.Documents;

internal static class PngDmiMetadataDetector
{
    private const int MaximumMetadataBytes = 1024 * 1024;
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static async Task<bool> HasDmiMetadataAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 16 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var signature = new byte[PngSignature.Length];
        await stream.ReadExactlyAsync(signature, cancellationToken).ConfigureAwait(false);
        if (!signature.AsSpan().SequenceEqual(PngSignature))
        {
            return false;
        }

        var header = new byte[8];
        while (stream.Position + 12 <= stream.Length)
        {
            await stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
            var chunkLength = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0, 4));
            var chunkType = Encoding.ASCII.GetString(header, 4, 4);
            var remainingWithCrc = stream.Length - stream.Position;
            if (chunkLength > int.MaxValue || chunkLength + 4L > remainingWithCrc)
            {
                throw new InvalidDataException("PNG contains a truncated or oversized chunk.");
            }

            if (chunkType is "tEXt" or "zTXt" or "iTXt")
            {
                if (chunkLength > MaximumMetadataBytes)
                {
                    throw new InvalidDataException("PNG text metadata exceeds the supported limit.");
                }

                var data = new byte[(int)chunkLength];
                await stream.ReadExactlyAsync(data, cancellationToken).ConfigureAwait(false);
                stream.Seek(4, SeekOrigin.Current);
                var description = await TryReadDescriptionAsync(chunkType, data, cancellationToken).ConfigureAwait(false);
                if (description is not null &&
                    description.Contains("# BEGIN DMI", StringComparison.Ordinal) &&
                    description.Contains("# END DMI", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            else
            {
                stream.Seek(chunkLength + 4L, SeekOrigin.Current);
            }

            if (chunkType == "IEND")
            {
                break;
            }
        }

        return false;
    }

    private static async Task<string?> TryReadDescriptionAsync(
        string chunkType,
        byte[] data,
        CancellationToken cancellationToken)
    {
        var keywordEnd = Array.IndexOf(data, (byte)0);
        if (keywordEnd <= 0 || !Encoding.Latin1.GetString(data, 0, keywordEnd).Equals("Description", StringComparison.Ordinal))
        {
            return null;
        }

        return chunkType switch
        {
            "tEXt" => Encoding.Latin1.GetString(data, keywordEnd + 1, data.Length - keywordEnd - 1),
            "zTXt" => await ReadCompressedTextAsync(data, keywordEnd + 2, cancellationToken).ConfigureAwait(false),
            "iTXt" => await ReadInternationalTextAsync(data, keywordEnd, cancellationToken).ConfigureAwait(false),
            _ => null
        };
    }

    private static async Task<string?> ReadInternationalTextAsync(
        byte[] data,
        int keywordEnd,
        CancellationToken cancellationToken)
    {
        var position = keywordEnd + 1;
        if (position + 2 > data.Length)
        {
            return null;
        }

        var compressed = data[position++] == 1;
        var compressionMethod = data[position++];
        if (compressed && compressionMethod != 0)
        {
            return null;
        }

        position = SkipNullTerminatedField(data, position);
        position = SkipNullTerminatedField(data, position);
        if (position > data.Length)
        {
            return null;
        }

        return compressed
            ? await ReadCompressedTextAsync(data, position, cancellationToken).ConfigureAwait(false)
            : Encoding.UTF8.GetString(data, position, data.Length - position);
    }

    private static int SkipNullTerminatedField(byte[] data, int position)
    {
        var end = Array.IndexOf(data, (byte)0, position);
        return end < 0 ? data.Length + 1 : end + 1;
    }

    private static async Task<string?> ReadCompressedTextAsync(
        byte[] data,
        int compressedDataOffset,
        CancellationToken cancellationToken)
    {
        if (compressedDataOffset <= 0 || compressedDataOffset >= data.Length)
        {
            return null;
        }

        await using var compressed = new MemoryStream(data, compressedDataOffset, data.Length - compressedDataOffset, writable: false);
        await using var zlib = new ZLibStream(compressed, CompressionMode.Decompress, leaveOpen: false);
        await using var output = new MemoryStream();
        var buffer = new byte[8 * 1024];
        while (true)
        {
            var bytesRead = await zlib.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            if (output.Length + bytesRead > MaximumMetadataBytes)
            {
                throw new InvalidDataException("Decompressed PNG text metadata exceeds the supported limit.");
            }

            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
        }

        return Encoding.Latin1.GetString(output.GetBuffer(), 0, checked((int)output.Length));
    }
}
