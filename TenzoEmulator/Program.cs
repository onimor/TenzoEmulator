using System.IO.Ports;

class WeightTerminalEmulator
{
    private static byte myAddr = 0x01;
    private static uint mySerial = 0x000001;
    private static double currentWeight = 123.45;
    private static int decimalPlaces = 2;
    private static bool isStable = true;
    private static bool isOverload = false;
    private static bool isNegative = false;

    private static string portName = "COM2";
    private static int baudRate = 9600;

    static void Main(string[] args)
    {
        Console.WriteLine("Тензо-М эмулятор терминала");
        Console.WriteLine("Команды: weight 123.45 | stable true/false | over true/false | neg true/false | dec 0-3 | quit");

        if (args.Length >= 2)
        {
            portName = args[0];
            baudRate = int.Parse(args[1]);
        }
        else
        {
            Console.Write("COM-порт (например COM2): ");
            portName = Console.ReadLine();
            Console.Write("Скорость (обычно 9600): ");
            baudRate = int.Parse(Console.ReadLine());
        }

        Thread worker = new Thread(SerialWorker) { IsBackground = true };
        worker.Start();

        while (true)
        {
            string cmd = Console.ReadLine()?.Trim().ToLower();
            if (cmd == "quit" || cmd == "q") break;

            if (cmd.StartsWith("weight ")) double.TryParse(cmd.Substring(7), out currentWeight);
            else if (cmd.StartsWith("stable ")) bool.TryParse(cmd.Substring(7), out isStable);
            else if (cmd.StartsWith("over ")) bool.TryParse(cmd.Substring(5), out isOverload);
            else if (cmd.StartsWith("neg ")) bool.TryParse(cmd.Substring(4), out isNegative);
            else if (cmd.StartsWith("dec ")) int.TryParse(cmd.Substring(4), out decimalPlaces);
        }
    }

    static void SerialWorker()
    {
        while (true)
        {
            SerialPort port = null;
            try
            {
                port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                    Handshake = Handshake.None,
                    RtsEnable = true,
                    DtrEnable = true
                };

                port.Open();
                Console.WriteLine($"[OK] {portName} открыт");

                while (port.IsOpen)
                {
                    var frame = ReadFrame(port);
                    if (frame == null) continue;

                    var response = ProcessFrame(frame);
                    if (response != null)
                        SendFrame(port, response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERR] {ex.Message}");
            }
            finally
            {
                try { port?.Close(); port?.Dispose(); } catch { }
            }

            Console.WriteLine("Переподключение через 1 секунду...");
            Thread.Sleep(1000);
        }
    }

    static byte[] ProcessFrame(byte[] payload)
    {
        if (payload.Length < 3) return null;

        int pos = 0;
        bool extended = payload[pos] == 0x00;
        if (extended) pos += 4; // пропускаем 00 + 3 байта серийника
        else pos += 1;

        byte cop = payload[pos++];
        bool addrOk = extended ? true : payload[0] == myAddr; // упрощённо, можно проверять серийник
        if (!addrOk) return null;

        switch (cop)
        {
            case 0xA0: // назначение адреса
                if (payload.Length >= pos + 1 && payload[pos] >= 1 && payload[pos] <= 0x9F)
                    myAddr = payload[pos];
                return new byte[] { payload[0], cop }; // пустой ответ

            case 0xA1: // запрос серийного номера
                return new byte[] { 0x00, cop, (byte)(mySerial >> 16), (byte)(mySerial >> 8), (byte)mySerial };

            case 0xC0: // тарирование
                currentWeight = 0;
                return new byte[] { payload[0], cop };

            case 0xC2: // нетто
            case 0xC3: // брутто
                return BuildWeightResponse((byte)(extended ? 0 : myAddr), cop);

            default:
                return null;
        }
    }

    static byte[] BuildWeightResponse(byte addrByte, byte cop)
    {
        long weightInt = (long)Math.Abs(currentWeight * Math.Pow(10, decimalPlaces));
        string s = weightInt.ToString("D6");
        byte w0 = (byte)((s[5] - '0') | ((s[4] - '0') << 4));
        byte w1 = (byte)((s[3] - '0') | ((s[2] - '0') << 4));
        byte w2 = (byte)((s[1] - '0') | ((s[0] - '0') << 4));

        byte con = (byte)(
            (isNegative ? 0x80 : 0) |
            (isStable ? 0x10 : 0) |
            (isOverload ? 0x08 : 0) |
            (decimalPlaces & 0x03));

        return new byte[] { addrByte, cop, w0, w1, w2, con };
    }

    static void SendFrame(SerialPort port, byte[] payload)
    {
        port.Write(new byte[] { 0xFF }, 0, 1);

        byte crc = 0;
        foreach (byte b in payload)
        {
            crc = CalcCrc(b, crc);
            port.Write(new byte[] { b }, 0, 1);
            if (b == 0xFF) port.Write(new byte[] { 0xFE }, 0, 1);
        }

        port.Write(new byte[] { crc }, 0, 1);
        if (crc == 0xFF) port.Write(new byte[] { 0xFE }, 0, 1);

        port.Write(new byte[] { 0xFF, 0xFF }, 0, 2);
    }

    static byte[] ReadFrame(SerialPort port)
    {
        try
        {
            // ждём стартовый 0xFF
            while (port.ReadByte() != 0xFF) { }

            var payload = new List<byte>();
            byte crc = 0;

            while (true)
            {
                int b = port.ReadByte();
                if (b == 0xFF)
                {
                    int next = port.ReadByte();
                    if (next == 0xFF) // конец кадра
                        return payload.Count >= 3 ? payload.ToArray() : null;
                    if (next == 0xFE)
                        b = 0xFF; // распакованный FF
                    else
                    {
                        payload.Add(0xFF);
                        payload.Add((byte)next);
                        continue;
                    }
                }
                else if (b == 0xFE)
                    continue; // пропускаем stuffing

                payload.Add((byte)b);
                crc = CalcCrc((byte)b, crc);
            }
        }
        catch (TimeoutException) { return null; }
        catch { return null; }
    }

    static byte CalcCrc(byte b, byte crc)
    {
        byte x = (byte)(crc ^ b);
        x = (byte)(x ^ (x << 4));
        return (byte)(x ^ (x >> 3) ^ (x << 5));
    }
}