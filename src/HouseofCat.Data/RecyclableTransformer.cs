﻿using HouseofCat.Compression.Recyclable;
using HouseofCat.Encryption;
using HouseofCat.Serialization;
using HouseofCat.Utilities;
using HouseofCat.Utilities.Errors;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HouseofCat.Data;

public class RecyclableTransformer
{
    public readonly RecyclableAesGcmEncryptionProvider EncryptionProvider;
    public readonly RecyclableGzipProvider CompressionProvider;
    public readonly ISerializationProvider SerializationProvider;

    public RecyclableTransformer(
        ISerializationProvider serializationProvider,
        RecyclableGzipProvider compressionProvider,
        RecyclableAesGcmEncryptionProvider encryptionProvider)
    {
        Guard.AgainstNull(serializationProvider, nameof(serializationProvider));
        Guard.AgainstNull(compressionProvider, nameof(compressionProvider));
        Guard.AgainstNull(encryptionProvider, nameof(encryptionProvider));

        SerializationProvider = serializationProvider;
        CompressionProvider = compressionProvider;
        EncryptionProvider = encryptionProvider;
    }

    /// <summary>
    /// Returns the bytes (which was the buffer) and actual length to use.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    public ReadOnlyMemory<byte> Transform<TIn>(TIn input)
    {
        using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
        SerializationProvider.Serialize(serializedStream, input);

        using var compressedStream = CompressionProvider.Compress(serializedStream, false);
        using var encryptedStream = EncryptionProvider.Encrypt(compressedStream, false);

        return encryptedStream.ToArray();
    }

    /// <summary>
    /// Returns the bytes (which was the buffer) and actual length to use.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<ReadOnlyMemory<byte>> TransformAsync<TIn>(TIn input)
    {
        using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
        await SerializationProvider
            .SerializeAsync(serializedStream, input)
            .ConfigureAwait(false);

        using var compressedStream = await CompressionProvider
            .CompressAsync(serializedStream, false)
            .ConfigureAwait(false);

        using var encryptedStream = await EncryptionProvider
            .EncryptAsync(compressedStream, false)
            .ConfigureAwait(false);

        return encryptedStream.ToArray();
    }

    public MemoryStream TransformToStream<TIn>(TIn input)
    {
        using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
        SerializationProvider.Serialize(serializedStream, input);

        using var compressedStream = CompressionProvider.Compress(serializedStream, false);

        return EncryptionProvider.Encrypt(compressedStream, false);
    }

    public async Task<MemoryStream> TransformToStreamAsync<TIn>(TIn input)
    {
        using var serializedStream = RecyclableManager.GetStream(nameof(RecyclableTransformer));
        await SerializationProvider
            .SerializeAsync(serializedStream, input)
            .ConfigureAwait(false);

        using var compressedStream = await CompressionProvider
            .CompressAsync(serializedStream, false)
            .ConfigureAwait(false);

        return await EncryptionProvider
            .EncryptAsync(compressedStream, false)
            .ConfigureAwait(false);
    }

    public TOut Restore<TOut>(ReadOnlyMemory<byte> data)
    {
        using var decryptStream = EncryptionProvider.DecryptToStream(data);
        using var decompressStream = CompressionProvider.Decompress(decryptStream, false);
        return SerializationProvider.Deserialize<TOut>(decompressStream);
    }

    public TOut Restore<TOut>(MemoryStream data)
    {
        using var decryptedStream = EncryptionProvider.Decrypt(data, false);
        using var decompressedStream = CompressionProvider.Decompress(decryptedStream, false);
        return SerializationProvider.Deserialize<TOut>(decompressedStream);
    }

    public async Task<TOut> RestoreAsync<TOut>(ReadOnlyMemory<byte> data)
    {
        using var decryptedStream = EncryptionProvider.DecryptToStream(data);
        using var compressionStream = await CompressionProvider
            .DecompressAsync(decryptedStream, false)
            .ConfigureAwait(false);

        return await SerializationProvider
            .DeserializeAsync<TOut>(compressionStream)
            .ConfigureAwait(false);
    }
}
