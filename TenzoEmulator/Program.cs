using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

class WeightTerminalEmulator
{
    private static byte myAddr = 0x01;
    private static uint mySerial = 0x000001; // SN2=0, SN1=0, SN0=1
    private static double currentWeight = 123.4; // Simulated weight in kg
    private static int decimalPlaces = 1; // Number of decimal places
    private static bool isStable = true;
    private static bool isOverload = false;
    private static bool isNegative = false;

    private static SerialPort serialPort;

    public static void Main(string[] args)
    {
        Console.Write("Enter serial port name (e.g., COM1): ");
        string portName = Console.ReadLine();

        Console.Write("Enter baud rate (e.g., 9600): ");
        int baudRate = int.Parse(Console.ReadLine());

        serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        serialPort.Open();

        Console.WriteLine("Emulator running. Commands:");
        Console.WriteLine("- 'weight <value>' to set simulated weight (e.g., weight 456.7)");
        Console.WriteLine("- 'stable <true/false>' to set stability");
        Console.WriteLine("- 'overload <true/false>' to set overload");
        Console.WriteLine("- 'negative <true/false>' to set sign");
        Console.WriteLine("- 'decimals <number>' to set decimal places (0-3)");
        Console.WriteLine("- 'quit' to exit");

        Thread serialThread = new Thread(SerialListener);
        serialThread.Start();

        while (true)
        {
            string input = Console.ReadLine().Trim();
            if (input == "quit") break;

            if (input.StartsWith("weight "))
            {
                double.TryParse(input.Substring(7), out currentWeight);
            }
            else if (input.StartsWith("stable "))
            {
                bool.TryParse(input.Substring(7), out isStable);
            }
            else if (input.StartsWith("overload "))
            {
                bool.TryParse(input.Substring(9), out isOverload);
            }
            else if (input.StartsWith("negative "))
            {
                bool.TryParse(input.Substring(9), out isNegative);
            }
            else if (input.StartsWith("decimals "))
            {
                int.TryParse(input.Substring(9), out decimalPlaces);
                if (decimalPlaces < 0 || decimalPlaces > 3) decimalPlaces = 1;
            }
        }

        serialPort.Close();
    }

    private static void SerialListener()
    {
        while (true)
        {
            try
            {
                List<byte> payload = ReadFrame();
                if (payload == null || payload.Count < 2) continue;

                // Parse address
                int pos = 0;
                byte addr1 = payload[pos++];
                bool isExtended = (addr1 == 0);
                uint receivedSerial = 0;
                byte addr = 0;
                if (isExtended)
                {
                    if (payload.Count < pos + 3) continue;
                    byte sn2 = payload[pos++];
                    byte sn1 = payload[pos++];
                    byte sn0 = payload[pos++];
                    receivedSerial = ((uint)sn2 << 16) | ((uint)sn1 << 8) | sn0;
                }
                else
                {
                    addr = addr1;
                }

                byte cop = payload[pos++];
                List<byte> data = new List<byte>(payload.GetRange(pos, payload.Count - pos - 1));
                byte receivedCrc = payload[payload.Count - 1];

                // Verify CRC over entire payload
                byte check = 0;
                foreach (byte b in payload)
                {
                    check = UpdateCRC(b, check);
                }
                if (check != 0) continue;

                // Check address match
                if (isExtended)
                {
                    if (receivedSerial != mySerial) continue;
                }
                else
                {
                    if (addr != myAddr) continue;
                }

                // Process command and get response data
                List<byte> responseData = ProcessCommand(cop, data);

                if (responseData != null)
                {
                    SendResponse(isExtended ? receivedSerial : addr, isExtended, cop, responseData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in serial listener: {ex.Message}");
            }
        }
    }

    private static List<byte> ProcessCommand(byte cop, List<byte> data)
    {
        List<byte> resp = new List<byte>();

        switch (cop)
        {
            case 0xA0: // Assign network address
                if (data.Count != 1) return null;
                byte newAddr = data[0];
                if (newAddr >= 0x01 && newAddr <= 0x9F)
                {
                    myAddr = newAddr;
                    return resp; // Empty response data
                }
                return null;

            case 0xA1: // Get serial number
                resp.Add((byte)((mySerial >> 16) & 0xFF));
                resp.Add((byte)((mySerial >> 8) & 0xFF));
                resp.Add((byte)(mySerial & 0xFF));
                return resp;

            case 0xC0: // Zero weight
                currentWeight = 0;
                return resp;

            case 0xC2: // Get net weight (simulated as current weight)
                return GetWeightResponseData();

            case 0xC3: // Get gross weight (simulated as current weight)
                return GetWeightResponseData();

            // Add more commands here as needed. For example:
            // case 0xBF: // Get status
            //     byte status = 0x00; // Simulate status byte
            //     resp.Add(status);
            //     return resp;

            default:
                // Unknown command, no response
                return null;
        }
    }

    private static List<byte> GetWeightResponseData()
    {
        List<byte> resp = new List<byte>();

        // Convert weight to 6-digit BCD (integer part scaled by decimals)
        double absWeight = Math.Abs(currentWeight);
        long intWeight = (long)(absWeight * Math.Pow(10, decimalPlaces));
        string strWeight = intWeight.ToString("D6"); // Pad to 6 digits

        byte w0 = (byte)(((strWeight[5] - '0') & 0x0F) | (((strWeight[4] - '0') & 0x0F) << 4));
        byte w1 = (byte)(((strWeight[3] - '0') & 0x0F) | (((strWeight[2] - '0') & 0x0F) << 4));
        byte w2 = (byte)(((strWeight[1] - '0') & 0x0F) | (((strWeight[0] - '0') & 0x0F) << 4));

        resp.Add(w0);
        resp.Add(w1);
        resp.Add(w2);

        byte con = (byte)((isNegative ? 0x80 : 0) |
                          (isStable ? 0x10 : 0) |
                          (isOverload ? 0x08 : 0) |
                          (decimalPlaces & 0x03));
        resp.Add(con);

        return resp;
    }

    private static void SendResponse(uint addrValue, bool isExtended, byte cop, List<byte> respData)
    {
        List<byte> payload = new List<byte>();

        if (isExtended)
        {
            payload.Add(0x00);
            payload.Add((byte)((addrValue >> 16) & 0xFF));
            payload.Add((byte)((addrValue >> 8) & 0xFF));
            payload.Add((byte)(addrValue & 0xFF));
        }
        else
        {
            payload.Add((byte)addrValue);
        }

        payload.Add(cop);
        payload.AddRange(respData);
        payload.Add(0x00); // CRC placeholder

        // Compute CRC
        byte crc = 0;
        for (int i = 0; i < payload.Count; i++)
        {
            crc = UpdateCRC(payload[i], crc);
        }
        payload[payload.Count - 1] = crc;

        // Send frame
        serialPort.Write(new byte[] { 0xFF }, 0, 1); // Start delimiter

        foreach (byte b in payload)
        {
            serialPort.Write(new byte[] { b }, 0, 1);
            if (b == 0xFF)
            {
                serialPort.Write(new byte[] { 0xFE }, 0, 1);
            }
        }

        serialPort.Write(new byte[] { 0xFF, 0xFF }, 0, 2); // End delimiter
    }

    private static byte UpdateCRC(byte input, byte crc)
    {
        byte al = input;
        byte ah = crc;

        for (int i = 0; i < 8; i++)
        {
            bool carry = (al & 0x80) != 0;
            al = (byte)((al << 1) | (carry ? 1 : 0));

            bool oldAh7 = (ah & 0x80) != 0;
            ah = (byte)((ah << 1) | (carry ? 1 : 0));

            if (oldAh7)
            {
                ah ^= 0x69;
            }
        }

        return ah;
    }

    private static List<byte> ReadFrame()
    {
        List<byte> payload = new List<byte>();

        // Skip leading FF bytes
        while (true)
        {
            int ib = serialPort.ReadByte();
            if (ib == -1) return null;
            byte b = (byte)ib;
            if (b != 0xFF)
            {
                if (b != 0xFE) // Skip stray FE if any
                {
                    payload.Add(b);
                    break;
                }
            }
        }

        // Read until two consecutive FF, handling stuffing
        while (true)
        {
            int ib = serialPort.ReadByte();
            if (ib == -1) return null;
            byte b = (byte)ib;

            if (b == 0xFE)
            {
                continue; // Skip stuffed FE
            }

            if (b == 0xFF)
            {
                int nextIb = serialPort.ReadByte();
                if (nextIb == -1) return null;
                byte next = (byte)nextIb;

                if (next == 0xFF)
                {
                    // End of frame
                    break;
                }
                else if (next == 0xFE)
                {
                    // Stuffed FF, add FF to payload
                    payload.Add(0xFF);
                }
                else
                {
                    // Unexpected, treat as data (add FF and next)
                    payload.Add(0xFF);
                    if (next != 0xFE)
                    {
                        payload.Add(next);
                    }
                }
            }
            else
            {
                payload.Add(b);
            }
        }

        return payload.Count >= 2 ? payload : null;
    }
}