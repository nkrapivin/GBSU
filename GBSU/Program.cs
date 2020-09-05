using System;
using System.IO;
using System.Text;

namespace GBSU
{
    public class Program
    {
        private static int Main(string[] args)
        {
            // Exit code 0 - all is fine
            // Exit code 1 - wrong arguments.
            // Exit code 2 - File I/O failure (file doesn't exist, folder doesn't exist, etc)
            if (args.Length < 2)
            {
                PrintUsage();
                return 1;
            }

            switch (args[0])
            {
                case "-unpack":
                    {
                        if (!Unpack(args[1], args[2])) return 2;
                        break;
                    }

                case "-pack":
                    {
                        if (!Pack(args[1], args[2])) return 2;
                        break;
                    }

                default:
                    {
                        PrintUsage();
                        return 1;
                    }
            }

            return 0; // All is fine.
        }

        private static bool Pack(string out_file_path, string file_dir)
        {
            if (!Directory.Exists(file_dir))
            {
                Console.WriteLine("ERROR: Savefile directory does not exist.");
                return false;
            }

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write((byte)0x01); // "version"?

            foreach (var file in Directory.EnumerateFiles(file_dir))
            {
                WriteGBString(writer, Path.GetFileName(file));

                try
                {
                    byte[] data = File.ReadAllBytes(file);

                    writer.Write(data.Length);
                    writer.Write(data);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Unable to read file.");
                    Console.WriteLine(e.Message);
                    return false;
                }
            }

            writer.Write(0xDEADBEEF);
            writer.Write(0xDEADBEEF);

            // padding? aligning? wtf?
            while (stream.Position % 262144 != 0)
                writer.Write((byte)0x00);

            writer.Dispose();

            try
            {
                File.WriteAllBytes(out_file_path, stream.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Unable to write output savefile.");
                Console.WriteLine(e.Message);
                return false;
            }

            stream.Dispose();

            return true;
        }

        private static bool Unpack(string save, string outdir)
        {
            if (!File.Exists(save))
            {
                Console.WriteLine("ERROR: Savefile does not exist.");
                return false;
            }

            if (!Directory.Exists(outdir))
            {
                try
                {
                    Directory.CreateDirectory(outdir);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Unable to create output directory!");
                    Console.WriteLine(e.Message);
                    return false;
                }
            }

            try
            {
                byte[] savedata = File.ReadAllBytes(save);
                var stream = new MemoryStream(savedata);
                var reader = new BinaryReader(stream);

                reader.ReadByte();
                while (true)
                {
                    string fname = ReadGBString(reader);
                    if (fname.Length == 0) break;

                    int filesize = reader.ReadInt32();
                    byte[] data = reader.ReadBytes(filesize);

                    File.WriteAllBytes(Path.Combine(outdir, fname), data);
                }

                reader.Dispose();
                stream.Dispose();
                savedata = null;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Unable to read savedata file!");
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        private static void WriteGBString(BinaryWriter writer, string val)
        {
            writer.Write(val.Length);
            foreach (var chr in val)
            {
                writer.Write(chr);
            }
        }

        private static string ReadGBString(BinaryReader reader)
        {
            int len = reader.ReadInt32();
            if (len == 0 || len == -559038737) return string.Empty;
            // -559038737 or 0xDEADBEEF (in big endian) is the end.
            return Encoding.UTF8.GetString(reader.ReadBytes(len));
        }

        private static void PrintUsage()
        {
            Console.WriteLine("GBSU - Game Baker Save Unpacker.");
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("Unpack: GBSU.exe -unpack <path to savefile> <path to output dir>");
            Console.WriteLine("Pack: GBSU.exe -pack <path to output savefile> <path to file directory>");
            Console.WriteLine();
            Console.WriteLine("If output directory doesn't exist. It will be created.");
            Console.WriteLine("If output savefile exists. It will be overwritten.");
        }

        /*
        private static void PrintArgs(string[] args)
        {
            foreach (string arg in args) Console.WriteLine(arg);
        }
        */
    }
}
