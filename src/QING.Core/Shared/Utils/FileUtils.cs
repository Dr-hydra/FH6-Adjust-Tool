using System.IO.Compression;

namespace QING.Core;
public static class FileUtils {

    #region 读取

    /// <summary>
    /// 打开指定文件的只读 <see cref="FileStream"/>。
    /// </summary>
    public static FileStream ReadAsStream(string filePath)
        => ResilientUtils.RetryOn<IOException, FileStream>(()
            => new(PathUtils.ForApi(filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

    /// <summary>
    /// 读取文件中的所有内容。
    /// </summary>
    public static byte[] ReadAsBytes(string filePath) {
        using Stream fs = FileUtils.ReadAsStream(filePath); // 不能使用 File.ReadAllBytes，它不指定 FileShare.ReadWrite，会在文件被占用时抛出异常
        using MemoryStream ms = new();
        fs.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// 读取文件中的所有内容。
    /// </summary>
    public static string ReadAsString(string filePath, Encoding? encoding = null) 
        => FileUtils.ReadAsBytes(filePath).GetString(encoding);
    /// <summary>
    /// 读取文件中的所有内容。
    /// 若文件不存在或读取失败，返回 <see langword="null"/>，而不是抛出异常。
    /// </summary>
    public static string? TryReadAsString(string filePath, Encoding? encoding = null) {
        try {
            if (!FileUtils.Exists(filePath)) return null;
            return FileUtils.ReadAsBytes(filePath).GetString(encoding);
        } catch {
            return null;
        }
    }

    /// <summary>
    /// 读取文件中的所有内容，并按行分割。
    /// </summary>
    public static string[] ReadAsLines(string filePath, bool skipEmptyLines = false, Encoding? encoding = null)
        => FileUtils.ReadAsString(filePath, encoding).SplitLines(skipEmptyLines);
    /// <summary>
    /// 读取文件中的所有内容，并按行分割。
    /// 若文件不存在或读取失败，返回空数组，而不是抛出异常。
    /// </summary>
    public static string[] TryReadAsLines(string filePath, bool skipEmptyLines = false, Encoding? encoding = null)
        => FileUtils.TryReadAsString(filePath, encoding)?.SplitLines(skipEmptyLines) ?? [];

    #endregion

    #region 创建 / 写入

    /// <summary>
    /// 创建文件，并将 <paramref name="text" /> 写入文件。
    /// 若文件已存在，则会覆盖原文件。
    /// </summary>
    public static void Write(string filePath, string text, Encoding? encoding = null) 
        => FileUtils.Write(filePath, (encoding ?? new UTF8Encoding()).GetBytes(text));
    /// <summary>
    /// 创建文件，并将 <paramref name="content" /> 写入文件。
    /// 若文件已存在，则会覆盖原文件。
    /// </summary>
    public static void Write(string filePath, IEnumerable<byte> content) 
        => FileUtils.Write(filePath, [.. content]);
    /// <summary>
    /// 创建文件，并将 <paramref name="content" /> 写入文件。
    /// 若文件已存在，则会覆盖原文件。
    /// </summary>
    public static void Write(string filePath, byte[] content) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(filePath));
        Logger.Trace($"写入文件：{filePath}（{content.Length} 字节）");
        ResilientUtils.RetryOn<IOException>(() => {
            FileUtils.Delete(filePath);
            File.WriteAllBytes(PathUtils.ForApi(filePath), content);
        });
    }

    /// <summary>
    /// 创建文件，并将 <paramref name="stream" /> 写入文件。
    /// 会将流的位置主动重置到开头。
    /// 若文件已存在，则会覆盖原文件。
    /// </summary>
    public static void Write(string filePath, Stream stream) {
        ResilientUtils.RetryOn<IOException>(() => {
            FileUtils.Delete(filePath);
            using FileStream fileStream = FileUtils.CreateAsStream(filePath);
            if (stream.CanSeek && stream.Position != 0) stream.Seek(0, SeekOrigin.Begin);
            Logger.Trace($"写入文件：{filePath}（{stream.GetType().Name}{(stream.CanSeek ? $" {stream.Length} 字节" : "")}）");
            stream.CopyTo(fileStream);
        });
    }
    /// <summary>
    /// 创建文件，并打开 <see cref="FileStream"/>。
    /// 若文件已存在，则会覆盖原文件。
    /// </summary>
    public static FileStream CreateAsStream(string filePath) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(filePath));
        Logger.Trace($"创建文件流：{filePath}");
        return ResilientUtils.RetryOn<IOException, FileStream>(() => {
            FileUtils.Delete(filePath);
            return new FileStream(PathUtils.ForApi(filePath), FileMode.Create);
        });
    }

    /// <summary>
    /// 在临时文件夹下创建一个随机名称的文件，并返回其路径。
    /// </summary>
    public static string CreateRandom()
        => ResilientUtils.RetryOn<IOException, string>(Path.GetTempFileName);

    #endregion

    #region 复制 / 剪切

    /// <summary>
    /// 复制文件。
    /// 会创建对应文件夹、覆盖已有的文件。
    /// 若复制自身到自身，则不执行操作；若仅大小写不同，则重命名此文件。
    /// </summary>
    public static void Copy(string sourceFilePath, string destFilePath) {
        sourceFilePath = PathUtils.ForCompare(sourceFilePath);
        destFilePath = PathUtils.ForCompare(destFilePath);
        if (string.Compare(sourceFilePath, destFilePath, ignoreCase:false) == 0) {
            // 复制自身到自身，则不执行操作
            Logger.Trace($"复制文件到自身，不执行操作：{sourceFilePath} → {destFilePath}");
        } else if (string.Compare(sourceFilePath, destFilePath, ignoreCase:true) == 0) {
            // 路径仅大小写不同，等效于重命名
            Logger.Trace($"复制文件到自身，但大小写不同，等效于重命名文件：{sourceFilePath} → {destFilePath}");
            FileUtils.Move(sourceFilePath, destFilePath);
        } else {
            // 实际的复制
            DirectoryUtils.Create(PathUtils.RemoveLastPart(destFilePath));
            Logger.Trace($"复制文件：{sourceFilePath} → {destFilePath}");
            ResilientUtils.RetryOn<IOException>(() 
                => File.Copy(PathUtils.ForApi(sourceFilePath), PathUtils.ForApi(destFilePath), true));
        }
    }

    /// <summary>
    /// 剪切文件。
    /// 会创建对应文件夹、覆盖已有的文件。
    /// </summary>
    public static void Move(string sourceFilePath, string destFilePath) {
        sourceFilePath = PathUtils.ForCompare(sourceFilePath);
        destFilePath = PathUtils.ForCompare(destFilePath);
        if (string.Compare(sourceFilePath, destFilePath, ignoreCase: false) == 0) {
            // 剪切自身到自身，则不执行操作
            Logger.Trace($"剪切文件到自身，不执行操作：{sourceFilePath} → {destFilePath}");
        } else if (string.Compare(sourceFilePath, destFilePath, ignoreCase: true) == 0) {
            // 路径仅大小写不同
            Logger.Trace($"剪切文件到自身，但大小写不同：{sourceFilePath} → {destFilePath}");
            ResilientUtils.RetryOn<IOException>(() => {
                var temp = Path.Combine(PathUtils.RemoveLastPart(sourceFilePath), Path.GetRandomFileName());
                File.Move(PathUtils.ForApi(sourceFilePath), PathUtils.ForApi(temp));
                File.Move(PathUtils.ForApi(temp), PathUtils.ForApi(destFilePath));
            });
        } else {
            // 实际的剪切
            DirectoryUtils.Create(PathUtils.RemoveLastPart(destFilePath));
            FileUtils.Delete(destFilePath);
            Logger.Trace($"剪切文件：{sourceFilePath} → {destFilePath}");
            ResilientUtils.RetryOn<IOException>(()
                => File.Move(PathUtils.ForApi(sourceFilePath), PathUtils.ForApi(destFilePath)));
        }
    }

    #endregion

    #region 删除

    /// <summary>
    /// 删除文件。
    /// <para/>若指定了 <paramref name="toRecycleBin"/>，则会尝试删除到回收站，但如果失败则会回退到永久删除。
    /// </summary>
    /// <returns>
    /// 如果文件不存在，返回 <see langword="null"/>。
    /// <para/>如果成功删除到回收站，返回 <see langword="true"/>。
    /// <para/>如果永久删除，返回 <see langword="false"/>。
    /// </returns>
    public static bool? Delete(string filePath, bool toRecycleBin = false) {
        if (!FileUtils.Exists(filePath)) return null;
        Logger.Trace($"{(toRecycleBin ? "将文件删除到回收站" : "删除文件")}：{filePath}");
        // 删除到回收站
        if (toRecycleBin) {
            try {
                DeleteToRecycleBin(filePath);
                return true;
            } catch (Exception ex) {
                Logger.Warn(ex, $"无法将文件删除到回收站，回退到永久删除：{filePath}");
            }
        }
        // 永久删除
        ResilientUtils.RetryOn<IOException>(()
            => File.Delete(PathUtils.ForApi(filePath)));
        return false;
    }

    /// <summary>
    /// 将文件或文件夹删除到回收站。
    /// </summary>
    internal static void DeleteToRecycleBin(string target) {
        // 实际的删除方法
        void Run() {
            IShellItem? item = null;
            IFileOperation? op = null;
            try {
                var iid = typeof(IShellItem).GUID;
                Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(PathUtils.RemoveExtendedPrefix(target), IntPtr.Zero, ref iid, out item));
                op = (IFileOperation) new FileOperation();
                op.SetOperationFlags(0x0040 | 0x0010 | 0x0004); // FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT
                op.DeleteItem(item, IntPtr.Zero);
                op.PerformOperations();
                op.GetAnyOperationsAborted(out bool aborted);
                if (aborted) throw new OperationCanceledException("Delete operation was aborted.");
            } finally {
                if (op != null && Marshal.IsComObject(op)) Marshal.FinalReleaseComObject(op);
                if (item != null && Marshal.IsComObject(item)) Marshal.FinalReleaseComObject(item);
            }
        }
        // 在 STA 线程中执行删除方法
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) {
            ResilientUtils.RetryOn<IOException>(Run);
        } else {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo? internalEx = null; // 捕获内部异常
            var thread = new Thread(() => { 
                try { 
                    ResilientUtils.RetryOn<IOException>(Run); 
                } catch (Exception ex) { 
                    internalEx = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex); 
                } 
            }) { IsBackground = true, Name = nameof(DeleteToRecycleBin) };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            internalEx?.Throw();
        }
    }
    // API
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItem { }
    [ComImport, Guid("3AD05575-8857-4850-9277-11B85BDB8E09")]
    class FileOperation { }
    [ComImport, Guid("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IFileOperation {
        void Advise(IntPtr pfops, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOperationFlags(uint dwOperationFlags);
        void SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string pszMessage);
        void SetProgressDialog(IntPtr popd);
        void SetProperties(IntPtr pproparray);
        void SetOwnerWindow(IntPtr hwndOwner);
        void ApplyPropertiesToItem(IShellItem psiItem);
        void ApplyPropertiesToItems(IntPtr punkItems);
        void RenameItem(IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IntPtr pfopsItem);
        void RenameItems(IntPtr pUnkItems, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        void MoveItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IntPtr pfopsItem);
        void MoveItems(IntPtr punkItems, IShellItem psiDestinationFolder);
        void CopyItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszCopyName, IntPtr pfopsItem);
        void CopyItems(IntPtr punkItems, IShellItem psiDestinationFolder);
        void DeleteItem(IShellItem psiItem, IntPtr pfopsItem);
        void DeleteItems(IntPtr punkItems);
        void NewItem(IShellItem psiDestinationFolder, uint dwFileAttributes, [MarshalAs(UnmanagedType.LPWStr)] string pszName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, IntPtr pfopsItem);
        void PerformOperations();
        void GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)] out bool pfAnyOperationsAborted);
    }

    #endregion

    #region 压缩 / 解压

    /// <summary>
    /// 以只读模式打开压缩文件。
    /// 会先尝试 UTF8 编码，失败后换用 GB18030 编码。
    /// </summary>
    public static ZipArchive OpenZip(string zipFilePath) {
        ZipArchive TryOpen(Encoding encoding) {
            Logger.Trace($"尝试以 {encoding.EncodingName} 编码打开压缩包：{zipFilePath}");
            var result = ZipFile.Open(PathUtils.ForApi(zipFilePath), ZipArchiveMode.Read, encoding);
            try {
                _ = result.Entries; // 如果编码有误，会在这里抛出 DecoderFallbackException；如果文件异常，会在这里抛出 InvalidDataException
                return result;
            } catch {
                result.Dispose();
                throw;
            }
        }
        try {
            try { // 尝试两种编码
                return TryOpen(new UTF8Encoding(false, true));
            } catch (DecoderFallbackException) {
                return TryOpen(Encoding.GetEncoding("GB18030"));
            }
        } catch (InvalidDataException ex) {
            throw new InvalidDataException($"文件不是压缩包，或者文件已损坏（{zipFilePath}）", ex);
        }
    }

    /// <summary>
    /// 尝试根据后缀名判断文件种类并解压文件，支持 gz 与 zip，会尝试将 jar 以 zip 方式解压。
    /// 会自动创建文件夹。会覆盖已有文件，但不会删除多余文件。
    /// </summary>
    /// <param name="progressHandler">参数为已完成的总比例（0~1）。</param>
    public static void ExtractToDirectory(string compressionFile, string outputDirectory, Action<double>? progressHandler = null) {
        compressionFile = PathUtils.ForApi(compressionFile);
        DirectoryUtils.Create(outputDirectory);
        // 解压 gz（gz 不需要考虑编码）
        if (compressionFile.EndsWithF(".gz", true)) {
            string outFilePath = Path.Combine(outputDirectory, PathUtils.GetFileNameWithoutExtension(compressionFile));
            Logger.Trace($"解压 gz 文件：{compressionFile} → {outFilePath}");
            using var fileStream = FileUtils.ReadAsStream(compressionFile);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            FileUtils.Write(outFilePath, gzipStream);
            progressHandler?.Invoke(1);
            return;
        }
        // 解压 zip
        using var archive = FileUtils.OpenZip(compressionFile);
        int totalCount = archive.Entries.Count;
        int doneCount = 0;
        Logger.Trace($"解压 zip 文件：{compressionFile} → {outputDirectory}（共 {totalCount} 项）");
        foreach (var entry in archive.Entries) {
            doneCount++;
            if (totalCount > 0) progressHandler?.Invoke((double) doneCount / totalCount);
            if (string.IsNullOrEmpty(entry.Name)) continue; // 跳过文件夹条目（ZipArchive 会将文件夹也作为一个 entry，但它们的 Name 为空）
            // ZipSlip 修复
            string outputFilePath = PathUtils.ForCompare(Path.Combine(outputDirectory, entry.FullName));
            if (!outputFilePath.StartsWithF(PathUtils.AddSlashSuffix(PathUtils.ForCompare(outputDirectory)), ignoreCase:true))
                throw new ZipSlipException($"Zip 文件项 {entry.FullName} 的路径在压缩包之外，这可能导致安全问题");
            // 实际的解压
            using var entryStream = entry.Open();
            FileUtils.Write(outputFilePath, entryStream);
        }
    }
    public class ZipSlipException(string message) : Exception(message) {}

    /// <summary>
    /// 将指定文件夹的内容打包为 zip 文件。
    /// 会自动创建文件夹。会覆盖已有文件。
    /// </summary>
    public static void CreateZipFromDirectory(string outputFullPath, string sourceDirectory) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(outputFullPath));
        FileUtils.Delete(outputFullPath);
        Logger.Trace($"将文件夹中的内容压缩为 zip 文件：{sourceDirectory} → {outputFullPath}");
        ResilientUtils.RetryOn<IOException>(()
            => ZipFile.CreateFromDirectory(PathUtils.ForApi(sourceDirectory), PathUtils.ForApi(outputFullPath)));
    }

    /// <summary>
    /// 将多个文件打包为 zip 文件，所有文件都会被放在压缩文件的根目录。
    /// 会自动创建文件夹。会覆盖已有文件。
    /// </summary>
    public static void CreateZipFromFiles(string outputFullPath, params string[] sourceFiles) {
        var sources = new Dictionary<string, string>();
        foreach (var source in sourceFiles) {
            string fileName = PathUtils.GetLastPart(source);
            if (sources.ContainsKey(fileName)) throw new ArgumentException($"尝试将多个同文件名的文件放进压缩包中（{fileName}）", nameof(sourceFiles));
            sources.Add(fileName, source);
        }
        FileUtils.CreateZipFromFiles(outputFullPath, sources);
    }

    /// <summary>
    /// 将多个文件打包为 zip 文件。
    /// 会自动创建文件夹。会覆盖已有文件。
    /// </summary>
    /// <param name="sources">键为 zip 文件下的路径，值为文件的本地路径。</param>
    public static void CreateZipFromFiles(string outputFullPath, IDictionary<string, string> sources) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(outputFullPath));
        FileUtils.Delete(outputFullPath);
        using var archive = ResilientUtils.RetryOn<IOException, ZipArchive>(()
            => ZipFile.Open(PathUtils.ForApi(outputFullPath), ZipArchiveMode.Create));
        Logger.Trace($"创建 zip 文件：{sources.Count} 个文件 → {outputFullPath}\n{sources.Select(p => $"- {p.Value} → {p.Key}").Join('\n')}");
        foreach (var pair in sources) archive.CreateEntryFromFile(PathUtils.ForApi(pair.Value), pair.Key.Replace('\\', '/'), CompressionLevel.Optimal);
    }

    #endregion

    /// <summary>
    /// 确定指定的文件是否存在。
    /// </summary>
    public static bool Exists(string filePath) 
        => File.Exists(PathUtils.ForApi(filePath));

    /// <summary>
    /// 获取 <see cref="FileInfo"/> 对象。
    /// </summary>
    public static FileInfo GetInfo(string path) 
        => new(PathUtils.ForApi(path));

}
