using System.Security;
using System.Security.Cryptography;

namespace QING.Core;
public static class CryptographyUtils {

    #region Hash

    public enum HashMethod {
        /// <summary>
        /// 使用 <see cref="MD5"/> 算法。在 16 进制下哈希为 32 长度字符串。
        /// </summary>
        Md5,
        /// <summary>
        /// 使用 <see cref="SHA1"/> 算法。在 16 进制下哈希为 40 长度字符串。
        /// </summary>
        Sha1,
        /// <summary>
        /// 使用 <see cref="SHA256"/> 算法。在 16 进制下哈希为 64 长度字符串。
        /// </summary>
        Sha256,
        /// <summary>
        /// 使用 <see cref="SHA512"/> 算法。在 16 进制下哈希为 128 长度字符串。
        /// </summary>
        Sha512
    }
    private static HashAlgorithm GetHashAlgorithm(HashMethod method) => method switch {
        HashMethod.Md5 => MD5.Create(),
        HashMethod.Sha1 => SHA1.Create(),
        HashMethod.Sha256 => SHA256.Create(),
        HashMethod.Sha512 => SHA512.Create(),
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    /// <summary>
    /// 计算文件的 Hash。返回 16 进制小写字符串。
    /// </summary>
    public static string ComputeFileHash(string filePath, HashMethod method = HashMethod.Md5) {
        using HashAlgorithm hashImpl = GetHashAlgorithm(method);
        Logger.Trace($"计算文件 {method}：{filePath}");
        using var file = FileUtils.ReadAsStream(filePath);
        return BitConverter.ToString(hashImpl.ComputeHash(file)).Replace("-", "").Lower();
    }

    /// <summary>
    /// 计算字节数组的 Hash。返回 16 进制小写字符串。
    /// </summary>
    public static string ComputeHash(byte[] input, HashMethod method = HashMethod.Md5) {
        using HashAlgorithm hashImpl = GetHashAlgorithm(method);
        return BitConverter.ToString(hashImpl.ComputeHash(input)).Replace("-", "").Lower();
    }
    /// <summary>
    /// 计算字符串的 Hash。返回 16 进制小写字符串。
    /// <para/> 使用 UTF-8 将字符串转换为字节数组。
    /// </summary>
    public static string ComputeHash(string input, HashMethod method = HashMethod.Md5) 
        => ComputeHash(Encoding.UTF8.GetBytes(input), method);

    #endregion

    #region DES
    private static readonly byte[] desInitialVector = Encoding.UTF8.GetBytes("95168702");

    /// <summary>
    /// 未指定密钥时使用的默认 DES 密钥。必须为 8 字节。
    /// </summary>
    public static string defaultDesKey = "@;$ Abv2";

    /// <summary>
    /// 使用 DES 对称加密算法加密字符串。
    /// </summary>
    public static string DesEncrypt(string sourceString, string? key = null) {
        key = key is null ? defaultDesKey : key!.GetStableHashCode().ToString().EnsureLength('X', 8)[..8];
        byte[] btKey = Encoding.UTF8.GetBytes(key);
        var des = new DESCryptoServiceProvider();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, des.CreateEncryptor(btKey, desInitialVector), CryptoStreamMode.Write);
        byte[] inData = Encoding.UTF8.GetBytes(sourceString);
        cs.Write(inData, 0, inData.Length);
        cs.FlushFinalBlock();
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// 使用 DES 对称加密算法解密字符串。
    /// </summary>
    public static string DesDecrypt(string encryptedString, string? key = null) {
        key = key is null ? defaultDesKey : key!.GetStableHashCode().ToString().EnsureLength('X', 8)[..8];
        byte[] btKey = Encoding.UTF8.GetBytes(key);
        var des = new DESCryptoServiceProvider();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, des.CreateDecryptor(btKey, desInitialVector), CryptoStreamMode.Write);
        byte[] inData = Convert.FromBase64String(encryptedString);
        cs.Write(inData, 0, inData.Length);
        cs.FlushFinalBlock();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    #endregion

    #region ECDSA

    /// <summary>
    /// 未指定公钥时的默认 ECDSA P-256 公钥。
    /// </summary>
    public static string defaultEcdsaPublicKey = "RUNTMSAAAAC4QTUNAewh23Q4Q6koHkyIrDIIZUSbua23sf2DiZmIRwSzadISDRyTVTbuWniH3KR7rKj8XBsabms1be6i3c+S";

    /// <summary>
    /// 进行 ECDSA P-256 非对称加密签名验证。
    /// 如果验证失败则抛出 <see cref="CryptographicException"/>。
    /// </summary>
    public static void EcdsaVerify(string sourceString, string sign, string? publicKey = null) {
        using var sha256 = GetHashAlgorithm(HashMethod.Sha256);
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sourceString));
        IntPtr algorithmHandle = IntPtr.Zero;
        IntPtr keyHandle = IntPtr.Zero;
        // 直接调用 DLL，以避免 .NET API 依赖的系统服务出现问题时导致验证失败
        try {
            int status = BCryptOpenAlgorithmProvider(out algorithmHandle, "ECDSA_P256", null, 0);
            if (status < 0) throw new CryptographicException($"{nameof(BCryptOpenAlgorithmProvider)} 失败，错误码 {status}");
            status = BCryptImportKeyPair(algorithmHandle, IntPtr.Zero, "ECCPUBLICBLOB", out keyHandle, Convert.FromBase64String(publicKey ?? defaultEcdsaPublicKey), 72, 0);
            if (status < 0) throw new CryptographicException($"{nameof(BCryptImportKeyPair)} 失败，错误码 {status}");
            status = BCryptVerifySignature(keyHandle, IntPtr.Zero, hash, hash.Length, Convert.FromBase64String(sign), 64, 0);
            if (status == unchecked((int) 0xC000A000)) throw new SecurityException("签名验证失败");
            if (status < 0) throw new CryptographicException($"{nameof(BCryptVerifySignature)} 失败，错误码 {status}");
        } finally {
            if (keyHandle != IntPtr.Zero) BCryptDestroyKey(keyHandle);
            if (algorithmHandle != IntPtr.Zero) BCryptCloseAlgorithmProvider(algorithmHandle, 0);
        }
    }
    [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)] private static extern int BCryptOpenAlgorithmProvider(out IntPtr algorithmHandle, string algorithmId, string? implementation, int flags);
    [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)] private static extern int BCryptImportKeyPair(IntPtr algorithmHandle, IntPtr importKey, string blobType, out IntPtr keyHandle, byte[] input, int inputSize, int flags);
    [DllImport("bcrypt.dll")] private static extern int BCryptVerifySignature(IntPtr keyHandle, IntPtr paddingInfo, byte[] hash, int hashSize, byte[] signature, int signatureSize, int flags);
    [DllImport("bcrypt.dll")] private static extern int BCryptDestroyKey(IntPtr keyHandle);
    [DllImport("bcrypt.dll")] private static extern int BCryptCloseAlgorithmProvider(IntPtr algorithmHandle, int flags);

    #endregion

}
