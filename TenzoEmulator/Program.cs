using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

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

    private static bool quietMode = false;   // тихий режим (меньше спама)
    private static bool logEnabled = true;   // полный лог
    private static int requestCount = 0;     // счётчик запросов для сводки

    static void Main(string[] args)
    {
        Console.WriteLine("Тензо-М эмулятор терминала (консольная версия)");
        Console.WriteLine("Команды:");
        Console.WriteLine("  weight 123.45 | stable true/false | overload true/false | neg true/false | dec 0-3");
        Console.WriteLine("  addr 2 | serial 123");
        Console.WriteLine("  quiet on/off — включить/выключить тихий режим (меньше спама в консоли)");
        Console.WriteLine("  log on/off — включить/выключить весь лог");
        Console.WriteLine("  quit");

        if (args.Length >= 2)
        {
            portName = args[0];
            baudRate = int.Parse(args[1]);
        }
        else
        {
            Console.Write("COM-порт (например COM8): ");
            portName = Console.ReadLine()?.Trim() ?? "COM2";
            Console.Write("Скорость (обычно 9600): ");
            int.TryParse(Console.ReadLine(), out baudRate);
            if (baudRate == 0) baudRate = 9600;
        }

        Thread worker = new Thread(SerialWorker) { IsBackground = true };
        worker.Start();

        while (true)
        {
            string cmd = Console.ReadLine()?.Trim().ToLower();
            if (cmd == "quit" || cmd == "q") break;

            if (cmd.StartsWith("weight ")) double.TryParse(cmd.Substring(7), out currentWeight);
            else if (cmd.StartsWith("stable ")) bool.TryParse(cmd.Substring(7), out isStable);
            else if (cmd.StartsWith("overload ")) bool.TryParse(cmd.Substring(9), out isOverload);
            else if (cmd.StartsWith("neg ")) bool.TryParse(cmd.Substring(4), out isNegative);
            else if (cmd.StartsWith("dec ")) int.TryParse(cmd.Substring(4), out decimalPlaces);
            else if (cmd.StartsWith("addr ")) byte.TryParse(cmd.Substring(5), out myAddr);
            else if (cmd.StartsWith("serial ")) uint.TryParse(cmd.Substring(7), out mySerial);
            else if (cmd == "quiet on") { quietMode = true; Console.WriteLine("[INFO] Тихий режим включён"); }
            else if (cmd == "quiet off") { quietMode = false; Console.WriteLine("[INFO] Тихий режим выключён"); }
            else if (cmd == "log on") { logEnabled = true; Console.WriteLine("[INFO] Лог включён"); }
            else if (cmd == "log off") { logEnabled = false; Console.WriteLine("[INFO] Лог выключён"); }
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
                    RtsEnable = true,
                    DtrEnable = true
                };

                port.Open();
                Log($"[OK] {portName} открыт (адрес: 0x{myAddr:X2})");

                requestCount = 0;

                while (port.IsOpen)
                {
                    var frame = ReadFrame(port);
                    if (frame == null) continue;

                    var response = ProcessFrame(frame);
                    if (response != null)
                    {
                        SendFrame(port, response);
                        requestCount++;

                        // В тихом режиме — редкая сводка
                        if (quietMode && requestCount % 100 == 0)
                        {
                            Log($"[INFO] Обработано {requestCount} запросов веса");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[ERR] {ex.Message}");
            }
            finally
            {
                try { port?.Close(); port?.Dispose(); } catch { }
            }

            Log("Переподключение через 1 секунду...");
            Thread.Sleep(1000);
        }
    }

    static void Log(string text)
    {
        if (logEnabled)
            Console.WriteLine(text);
    }

    static byte[] ProcessFrame(byte[] payload)
    {
        if (payload.Length < 3) return null;

        int pos = 0;
        bool extended = payload[pos] == 0x00;

        uint receivedSerial = 0;
        byte addr = 0;

        if (extended)
        {
            if (payload.Length < 4) return null;
            receivedSerial = (uint)((payload[1] << 16) | (payload[2] << 8) | payload[3]);
            pos = 4;

            if (receivedSerial != mySerial)
                return null;
        }
        else
        {
            addr = payload[0];
            pos = 1;

            if (addr != myAddr)
                return null;
        }

        byte cop = payload[pos++];

        if (!quietMode)
            Log($"[RX ←] Получено: {BytesToString(payload)} | Команда: 0x{cop:X2}");

        switch (cop)
        {
            case 0xA0:
                if (payload.Length >= pos + 1 && payload[pos] >= 1 && payload[pos] <= 0x9F)
                {
                    myAddr = payload[pos];
                    Log($"[SET] Новый адрес: 0x{myAddr:X2}");
                }
                return BuildResponsePayload(extended, cop, new byte[0]);

            case 0xA1:
                return BuildResponsePayload(extended, cop, new byte[] {
                    (byte)(mySerial >> 16), (byte)(mySerial >> 8), (byte)mySerial
                });

            case 0xC0:
                currentWeight = 0;
                Log("[ACTION] Тарирование (вес = 0)");
                return BuildResponsePayload(extended, cop, new byte[0]);

            case 0xC2:
            case 0xC3:
                if (!quietMode)
                    Log($"[REPLY] Вес: {currentWeight} ({(cop == 0xC2 ? "Нетто" : "Брутто")})");
                return BuildWeightResponse(extended, cop);

            default:
                return null;
        }
    }

    static byte[] BuildResponsePayload(bool extended, byte cop, byte[] data)
    {
        var resp = new List<byte>();
        if (extended) resp.Add(0x00);
        resp.Add(extended ? (byte)0 : myAddr);
        resp.Add(cop);
        resp.AddRange(data);
        return resp.ToArray();
    }

    static byte[] BuildWeightResponse(bool extended, byte cop)
    {
        long val = (long)Math.Abs(currentWeight * Math.Pow(10, decimalPlaces));
        string s = val.ToString("D6");

        byte w0 = (byte)((s[5] - '0') | ((s[4] - '0') << 4));
        byte w1 = (byte)((s[3] - '0') | ((s[2] - '0') << 4));
        byte w2 = (byte)((s[1] - '0') | ((s[0] - '0') << 4));

        byte con = (byte)(
            (isNegative ? 0x80 : 0) |
            (isStable ? 0x10 : 0) |
            (isOverload ? 0x08 : 0) |
            (decimalPlaces & 0x03));

        return BuildResponsePayload(extended, cop, new byte[] { w0, w1, w2, con });
    }

    static void SendFrame(SerialPort port, byte[] payload)
    {
        if (!quietMode)
            Console.Write($"[TX →] Отправка: {BytesToString(payload)}");

        port.Write(new byte[] { 0xFF }, 0, 1);

        byte crc = 0;
        foreach (byte b in payload)
        {
            crc = CalcCrc(b, crc);
            port.Write(new byte[] { b }, 0, 1);
            if (b == 0xFF) port.Write(new byte[] { 0xFE }, 0, 1);
        }

        crc = CalcCrc(0, crc); // placeholder 0

        port.Write(new byte[] { crc }, 0, 1);
        if (crc == 0xFF) port.Write(new byte[] { 0xFE }, 0, 1);

        if (!quietMode)
            Console.WriteLine($" | CRC: {crc:X2}");

        port.Write(new byte[] { 0xFF, 0xFF }, 0, 2);

        Thread.Sleep(50);
    }

    static byte[] ReadFrame(SerialPort port)
    {
        try
        {
            while (port.ReadByte() != 0xFF) { }

            var payload = new List<byte>();

            while (true)
            {
                int b = port.ReadByte();

                if (b == 0xFF)
                {
                    int next = port.ReadByte();
                    if (next == 0xFF)
                        return payload.Count >= 3 ? payload.ToArray() : null;
                    if (next == 0xFE)
                        payload.Add(0xFF);
                    else
                    {
                        payload.Add(0xFF);
                        payload.Add((byte)next);
                    }
                }
                else if (b == 0xFE)
                {
                    continue;
                }
                else
                {
                    payload.Add((byte)b);
                }
            }
        }
        catch (TimeoutException) { return null; }
        catch { return null; }
    }

    static byte CalcCrc(byte input, byte crc)
    {
        byte IN = input;
        byte CRC = crc;

        for (int bit = 0; bit < 8; bit++)
        {
            bool CL = (IN & 0x80) != 0;
            IN = (byte)((IN << 1) & 0xFF);

            bool CH = (CRC & 0x80) != 0;
            CRC = (byte)((CRC << 1) & 0xFF);

            if (CL) CRC |= 1;

            if (CH) CRC ^= 0x69;
        }

        return CRC;
    }

    static string BytesToString(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", " ");
    }
}