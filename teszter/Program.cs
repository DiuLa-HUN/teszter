using System.Diagnostics;
using System.Management;
using System.Text;
using NickStrupat;
using OpenCL.Net;
using Environment = System.Environment;


// Egyszerű tesztelés, hogy mennyire gyors a számítás, többszálas megoldás
class Program
{
    static void Main()
    {
        Console.WriteLine("Üdvözöllek a CPU, RAM és GPU tesztelő programban!");
        Console.WriteLine("Kérlek válassz egy lehetőséget:");
        Console.WriteLine("1. CPU teszt");
        Console.WriteLine("2. RAM teszt");
        Console.WriteLine("3. GPU teszt");
        Console.WriteLine("4. Kilépés");
        Console.Write("Választás: ");
        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                var program = new Program();
                program.CPUteszt();
                break;
            case "2":
                var program2 = new Program();
                program2.RAMteszt();
                break;
            case "3":
                var program3 = new Program();
                program3.GPUteszt();
                break;
            case "4":
                Console.WriteLine("Kilépés a programból...");
                Environment.Exit(0);
                break;
            default:
                Console.WriteLine("Érvénytelen választás. Kérlek próbáld újra.");
                break;
        }
    }

    void CPUteszt()
    {
        Console.Clear();
        Console.WriteLine("CPU információk lekérdezése...");
        var searcher = new ManagementObjectSearcher("select Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed from Win32_Processor");
        int logicalCores = 0;
        List<string> ujSor = new List<string>();
        string sor = "";

        foreach (var item in searcher.Get())
        {
            Console.WriteLine("CPU neve: " + item["Name"]);
            sor = "CPU neve: " + item["Name"];
            ujSor.Add(sor);
            Console.WriteLine("Fizikai magok: " + item["NumberOfCores"]);
            sor = "Fizikai magok: " + item["NumberOfCores"];
            ujSor.Add(sor);
            Console.WriteLine("Logikai szálak: " + item["NumberOfLogicalProcessors"]);
            sor = "Logikai szálak: " + item["NumberOfLogicalProcessors"];
            ujSor.Add(sor);
            logicalCores = Convert.ToInt32(item["NumberOfLogicalProcessors"]);
            Console.WriteLine("Max órajel (MHz): " + item["MaxClockSpeed"]);
            sor = "Max órajel (MHz): " + item["MaxClockSpeed"];
            ujSor.Add(sor);
        }

        long total = 0;
        string formatted = "";
        int iterationsPerThread = 0; // alapértelmezett érték
        object locker = new object();
        Console.WriteLine("Mennyi művelettel szeretnéd tesztelni a processzorod?");
        Console.WriteLine("(Ha nem írsz be semmit, akkor alapból 100 000 000 művelet lesz a beállított érték):");
        string input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            iterationsPerThread = 100_000_000;
        }
        else if (int.TryParse(input, out iterationsPerThread))
        {
            iterationsPerThread = Convert.ToInt32(input);
        }
        else
        {
            Console.WriteLine("Érvénytelen szám. Alapérték került beállításra.");
            iterationsPerThread = 100_000_000;
        }

        formatted = iterationsPerThread.ToString("#,0");
        Console.WriteLine($"A teszt közben {formatted} művelettel lesz bombázva minden szál a processzorban.");
        sor = $"Műveletek száma: {formatted}";
        ujSor.Add(sor);
        Console.WriteLine("CPU többszálas teszt indításához nyomj meg egy gombot...");
        Console.ReadKey(); // várakozás a billentyűleütésre
        Console.WriteLine("Teszt indul...");

        Stopwatch[] timers = new Stopwatch[logicalCores];

        Parallel.For(0, logicalCores, i =>
        {
            timers[i] = Stopwatch.StartNew();

            long localSum = 0;
            for (int j = 0; j < iterationsPerThread; j++)
            {
                localSum += j % 13;
            }

            timers[i].Stop();

            lock (locker)
            {
                total += localSum;
            }

            Console.WriteLine($"[{i}. szál] Idő: {timers[i].Elapsed.TotalSeconds:F4} s");
            sor = $"[{i}. szál] Idő: {timers[i].Elapsed.TotalSeconds:F4} s";
            ujSor.Add(sor);
        });

        formatted = total.ToString("#,0");
        Console.WriteLine($"Számítás kész. Eredmény: {formatted}");
        sor = $"Eredmény: {formatted}";
        ujSor.Add(sor);
        sor = "Legutóbb tesztelve: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        ujSor.Add(sor);

        try
        {
            File.WriteAllLines("eredmények.csv", ujSor, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hiba a mentés során:\n" + ex.Message);
        }

        Console.WriteLine("Eredmények mentve: eredmények.csv");
        Console.WriteLine("Teszt vége.");
        ujSor.Clear();
        Console.ReadKey(); // várakozás a billentyűleütésre
        Console.Clear();
        Main();
    }

    void RAMteszt()
    {
        Console.Clear();
        Console.WriteLine("RAM információk lekérdezése...");
        int sizeMB = 0;
        string formatted = "";

        // Teljes és szabad memória lekérdezése
        var computerInfo = new ComputerInfo();

        ulong totalMemory = computerInfo.TotalPhysicalMemory;
        ulong availableMemory = computerInfo.AvailablePhysicalMemory;

        Console.WriteLine($"Teljes fizikai memória: {totalMemory / (1024 * 1024)} MB");
        Console.WriteLine($"Elérhető memória: {availableMemory / (1024 * 1024)} MB");

        // RAM órajel lekérdezése (ha elérhető)
        try
        {
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            foreach (ManagementObject obj in searcher.Get())
            {
                Console.WriteLine($"RAM órajele: {obj["Speed"]} MHz");
                Console.WriteLine($"RAM kapacitása: {Convert.ToUInt64(obj["Capacity"]) / (1024 * 1024)} MB");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Nem sikerült lekérdezni a RAM órajelét: {ex.Message}");
        }

        Console.WriteLine("Mennyi MB memóriával szeretnéd tesztelni a RAM-odat?");
        Console.WriteLine("(Ha nem írsz be semmit, akkor alapból 500 MB memória lesz a beállított érték):");
        string input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            sizeMB = 500;
        }
        else if (int.TryParse(input, out sizeMB))
        {
            sizeMB = Convert.ToInt32(input);
        }
        else
        {
            Console.WriteLine("Érvénytelen szám. Alapérték került beállításra.");
            sizeMB = 500;
        }

        Console.WriteLine("RAM teszt indításához nyomj meg egy gombot...");
        Console.ReadKey(); // várakozás a billentyűleütésre
        Console.WriteLine("Teszt indul...");
        int arraySize = (sizeMB * 1024 * 1024) / sizeof(int);
        int[] memoryArray = new int[arraySize];

        var stopwatch = new Stopwatch();

        // Írás teszt
        stopwatch.Start();

        for (int i = 0; i < memoryArray.Length; i++)
        {
            memoryArray[i] = i;
        }

        stopwatch.Stop();
        Console.WriteLine($"Írási idő ({sizeMB}MB): {stopwatch.Elapsed.TotalSeconds:F4} s");

        // Olvasás teszt
        long sum = 0;
        stopwatch.Restart();

        for (int i = 0; i < memoryArray.Length; i++)
        {
            sum += memoryArray[i];
        }

        stopwatch.Stop();
        formatted = sum.ToString("#,0");
        Console.WriteLine($"Olvasási idő ({sizeMB}MB): {stopwatch.Elapsed.TotalSeconds:F4} s");
        Console.WriteLine($"RAM teszt vége. Ellenőrző összeg: {formatted}");
        Console.ReadKey(); // várakozás a billentyűleütésre
        Console.Clear();
        Main();
    }

    void GPUteszt()
    {
        Console.Clear();
        Console.WriteLine("GPU információk lekérdezése...");
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
        
        foreach (ManagementObject obj in searcher.Get())
        {
            Console.WriteLine("GPU neve: " + obj["Name"]);
            long vram = Convert.ToInt64(obj["AdapterRAM"]);
            Console.WriteLine("VRAM: " + (vram / (1024 * 1024)) + " MB");
            Console.WriteLine("Gyártó: " + obj["AdapterCompatibility"]);
            Console.WriteLine("GPU gyártó: " + obj["AdapterDACType"]);
            Console.WriteLine("GPU architektúra / chip típusa: " + obj["VideoProcessor"]);
            Console.WriteLine("----------------------------------");
        }

        int dataSize = 100_000_000;
        Console.WriteLine($"Mennyi elemet szeretnél tesztelni a GPU-val? (Alapértelmezett: {dataSize} (Súlyos terhelés))");
        Console.WriteLine("(Ha nem írsz be semmit, akkor alapból 100 000 000 elem lesz a beállított érték):");
        string input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            dataSize = 100_000_000;
        }
        else if (int.TryParse(input, out dataSize))
        {
            dataSize = Convert.ToInt32(input);
        }
        else
        {
            Console.WriteLine("Érvénytelen szám. Alapérték került beállításra.");
            dataSize = 100_000_000;
        }

        Console.WriteLine("GPU teszt indításához nyomj meg egy gombot...");
        Console.ReadKey(); // várakozás a billentyűleütésre
        Console.WriteLine("Teszt indul...");
        float[] a = new float[dataSize];
        float[] b = new float[dataSize];
        float[] result = new float[dataSize];

        // Inicializálás
        for (int i = 0; i < dataSize; i++)
        {
            a[i] = 1.0f;
            b[i] = 2.0f;
        }

        // Kernel forrás
        string kernelSource = @"
        __kernel void vector_add(__global const float* a,
                                 __global const float* b,
                                 __global float* result)
        {
            int id = get_global_id(0);
            result[id] = a[id] + b[id];
        }";

        ErrorCode error;
        Platform[] platforms = Cl.GetPlatformIDs(out error);
        Platform platform = platforms[0];
        Device[] devices = Cl.GetDeviceIDs(platform, DeviceType.Gpu, out error);
        Device device = devices[0];

        // Kontextus, parancssor, program
        Context context = Cl.CreateContext(null, 1, new[] { device }, null, IntPtr.Zero, out error);
        CommandQueue queue = Cl.CreateCommandQueue(context, device, (CommandQueueProperties)0, out error);
        OpenCL.Net.Program program = Cl.CreateProgramWithSource(context, 1, new[] { kernelSource }, null, out error);
        error = Cl.BuildProgram(program, 0, null, string.Empty, null, IntPtr.Zero);
        Kernel kernel = Cl.CreateKernel(program, "vector_add", out error);

        // Bufferek
        IMem bufferA = Cl.CreateBuffer(context, MemFlags.ReadOnly | MemFlags.CopyHostPtr, a.Length * sizeof(float), a, out error);
        IMem bufferB = Cl.CreateBuffer(context, MemFlags.ReadOnly | MemFlags.CopyHostPtr, b.Length * sizeof(float), b, out error);
        IMem bufferResult = Cl.CreateBuffer(context, MemFlags.WriteOnly, result.Length * sizeof(float), out error);

        // Kernel argumentumok
        error = Cl.SetKernelArg(kernel, 0, bufferA);
        error |= Cl.SetKernelArg(kernel, 1, bufferB);
        error |= Cl.SetKernelArg(kernel, 2, bufferResult);

        // Időmérés indítása
        Stopwatch sw = Stopwatch.StartNew();

        // Kernel futtatása
        Event clevent;
        IntPtr[] workSize = new IntPtr[] { (IntPtr)dataSize };
        error = Cl.EnqueueNDRangeKernel(queue, kernel, 1, null, workSize, null, 0, null, out clevent);

        // Várakozás és idő leállítás
        Cl.Finish(queue);
        sw.Stop();

        // Eredmény beolvasása
        error = Cl.EnqueueReadBuffer(queue, bufferResult, Bool.True, IntPtr.Zero, new IntPtr(result.Length * sizeof(float)), result, 0, null, out _);

        // Eredmény kiírása
        Console.WriteLine($"GPU számítás kész: {dataSize} elem");
        Console.WriteLine($"Idő: {sw.ElapsedMilliseconds} ms");

        // Ellenőrzés
        Console.WriteLine($"Ellenőrzés: result[0] = {result[0]} (elvárt: 3.0)");
        Console.WriteLine("Nyomj meg egy gombot a folytatáshoz.");
        Console.ReadKey(); // várakozás a billentyűleütésre
        Console.Clear();
        Main();
    }
}