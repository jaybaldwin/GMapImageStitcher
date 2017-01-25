using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using CommandLine;
using CommandLine.Text;
using System.Net;

namespace ImageStitcher
{
    
    class Program
    {
        class Options
        {
            [Option('d', "Download", Required = false, DefaultValue = false, HelpText = "Do the download?")]
            public bool Download { get; set; }

            [Option('u', "DownloadRoot", Required = false, HelpText = "Download root directory. -d must also be true")]
            public string DownloadRoot { get; set; }

            [Option('z', "ZoomLevel", Required = true, HelpText = "Zoom Level, if present.  Set to 0 if not.")]
            public int ZoomLevel { get; set; }

            [Option('t', "TileSize", Required = true, HelpText = "Width / Height of tiles in pixels")]
            public int TileSize { get; set; }

            [Option('l', "Location", Required = false, HelpText = "Absolute path to folder where images are located. Current directory used if ommitted.")]
            public string Location { get; set; }

            [Option('p', "Pattern", Required = false, DefaultValue = "Z_X_Y.jpg", HelpText = "File pattern of file names.")]
            public string Pattern { get; set; }

            [Option('x', "Xmin", Required = false, DefaultValue = 0, HelpText = "Min X to process? (Nth column of tiles), beginning with 0.")]
            public int Xmin { get; set; }

            [Option('X', "Xmax", Required = true, HelpText = "Max X to process? (Nth column of tiles), beginning with 0.")]
            public int Xmax { get; set; }

            [Option('y', "Ymin", Required = false, DefaultValue = 0, HelpText = "Min Y to process? (Nth row of tiles), beginning with 0.")]
            public int Ymin { get; set; }

            [Option('Y', "Ymax", Required = true, HelpText = "Max Y to process? (Nth row of tiles), beginning with 0.")]
            public int Ymax { get; set; }

            [Option('o', "OutputFile", Required = false, DefaultValue = "", HelpText = "File name to output JPG to.")]
            public string OutputLocation { get; set; }

            [Option('s', "StitchImage", Required = false, DefaultValue = false, HelpText = "Should we stitch the image together?")]
            public bool StitchImage { get; set; }

            [Option('f', "ForceStitch", Required = false, DefaultValue = false, HelpText = "Wait for prompts?")]
            public bool SilentDo { get; set; }

            [Option('v', "Verbose", DefaultValue = true, HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption('?',"help")]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }


        static bool PromptToContinue(Options options)
        {
            // If SilentDo is not selected, prompt the user and allow them to bail.
            bool blnDoWork = options.SilentDo;

            if (!blnDoWork)
            {
                Console.WriteLine("Press Y to continue or N to cancel.");
                ConsoleKeyInfo result = Console.ReadKey();

                while (result == null || (result.Key != ConsoleKey.Y && result.Key != ConsoleKey.N))
                {
                    result = Console.ReadKey();
                }

                if (result.Key == ConsoleKey.Y) blnDoWork = true;
            }

            if (!blnDoWork && options.Verbose)
            {
                Console.WriteLine("");
                Console.WriteLine("Job canceled by user.");
            }
            return blnDoWork;
        }


        static void DownloadWork(Options options)
        {
            if (options.Verbose) Console.WriteLine("Preparing to download the tiles.");
            if (options.Verbose) Console.WriteLine("Destination: {0}", options.Location);

            // If SilentDo is not selected, prompt the user and allow them to bail.
            if (!options.SilentDo && !PromptToContinue(options)) return;

            for (int x = options.Xmin; x <= options.Xmax; x++)
            {
                for (int y = options.Ymin; y <= options.Ymax; y++)
                {
                    string strFileName = options.Pattern;
                    strFileName = strFileName.Replace("Z", options.ZoomLevel.ToString());
                    strFileName = strFileName.Replace("X", x.ToString());
                    strFileName = strFileName.Replace("Y", y.ToString());

                    string strAbsolutePath = (options.Location + strFileName);

                    if (File.Exists(strAbsolutePath)) continue;
                    
                    using (var client = new WebClient())
                    {
                        string url = options.DownloadRoot.Trim() + strFileName;
                        if (options.Verbose) Console.Write("Downloading " + url + " ... ");

                        client.DownloadFile(url, options.Location + strFileName);

                        if (options.Verbose) Console.WriteLine("done.");
                    }
                }
            }
        }


        static void StitchImageWork(Options options)
        {
            if (options.Verbose) Console.WriteLine("Preparing to build the image.");
            if (options.Verbose) Console.WriteLine("Looking in: {0}", options.Location);

            if (options.Verbose) Console.WriteLine("TileSize: " + options.TileSize.ToString());

            if (options.Verbose) Console.WriteLine("Xmin: " + options.Xmin.ToString());
            if (options.Verbose) Console.WriteLine("Xmax: " + options.Xmax.ToString());

            if (options.Verbose) Console.WriteLine("Ymin: " + options.Ymin.ToString());
            if (options.Verbose) Console.WriteLine("Ymax: " + options.Ymax.ToString());

            if (options.Verbose) Console.WriteLine("zoomLevel: " + options.ZoomLevel.ToString());


            int intImageWidth = (options.TileSize * (1 + options.Xmax)) - (options.TileSize * options.Xmin);
            int intImageHeight = (options.TileSize * (1 + options.Ymax)) - (options.TileSize * options.Ymin);

            Console.WriteLine("Image will be " + intImageWidth.ToString() + " x " + intImageHeight.ToString());


            // If SilentDo is not selected, prompt the user and allow them to bail.
            if (!options.SilentDo && !PromptToContinue(options)) return;


            Bitmap final = new Bitmap(intImageWidth, intImageHeight, PixelFormat.Format24bppRgb);

            bool blnDidWork = false;

            for (int x = options.Xmin; x <= options.Xmax; x++)
            {
                for (int y = options.Ymin; y <= options.Ymax; y++)
                {
                    string strFileName = options.Pattern;
                    strFileName = strFileName.Replace("Z", options.ZoomLevel.ToString());
                    strFileName = strFileName.Replace("X", x.ToString());
                    strFileName = strFileName.Replace("Y", y.ToString());

                    string strAbsolutePath = (options.Location + strFileName);

                    bool blnExists = File.Exists(strAbsolutePath);

                    if (options.Verbose) Console.WriteLine("File [" + (blnExists ? "EXISTS" : "MISSING") + "]: " + strAbsolutePath);

                    if (blnExists)
                    {
                        using (Stream BitmapStream = File.Open(strAbsolutePath, FileMode.Open))
                        {
                            Image imgTile = Image.FromStream(BitmapStream);
                            Bitmap tile = new Bitmap(imgTile);

                            for (int tileX = 0; tileX < tile.Width; tileX++)
                            {
                                for (int tileY = 0; tileY < tile.Height; tileY++)
                                {
                                    int firstPixelX = (options.TileSize * x) - (options.TileSize * options.Xmin);
                                    int firstPixelY = (options.TileSize * y) - (options.TileSize * options.Ymin);

                                    final.SetPixel(firstPixelX + tileX, firstPixelY + tileY, tile.GetPixel(tileX, tileY));
                                    if (!blnDidWork) blnDidWork = true;
                                }
                            }

                            tile.Dispose();
                        }

                    }
                }
            }

            // Work on saving our file.

            string strOutputFilename = options.OutputLocation;
            if (strOutputFilename == "") strOutputFilename = options.Location + "stiched" + ("_" + options.Xmin.ToString() + "-" + options.Ymin.ToString() + "_to_" + options.Xmax.ToString() + "-" + options.Ymax.ToString()) + ".jpg";

            if (options.Verbose) Console.Write("Saving file as \"" + strOutputFilename + "\"...");


            // Set the quality to what you want by configuring a codec.

            ImageFormat format = ImageFormat.Jpeg;

            ImageCodecInfo jpgCodec = null;
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid) jpgCodec = codec;
            }

            EncoderParameters codecParams = new EncoderParameters(1);
            codecParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

            if (File.Exists(strOutputFilename))
            {
                strOutputFilename = options.Location + "stiched" + ("_" + options.Xmin.ToString() + "-" + options.Ymin.ToString() + "_to_" + options.Xmax.ToString() + "-" + options.Ymax.ToString()) + "_" + Guid.NewGuid().ToString().ToLower() + ".jpg";
            }

            // Do the save.
            final.Save(strOutputFilename, jpgCodec, codecParams);

            Console.Write("Done.");
            Console.WriteLine("");

            final.Dispose();
        }

        static void Main(string[] args)
        {
            // Increase buffer.
            int lines = 65536 / 2 / Console.BufferWidth;
            Console.SetBufferSize(Console.BufferWidth, lines);

            // Settup options.
            Options options = new Options();
            Parser parser = new CommandLine.Parser(configuration: (Settings) => new CommandLine.ParserSettings
            {
                CaseSensitive = true,
                IgnoreUnknownArguments = true,
                MutuallyExclusive = false,
                HelpWriter = Console.Error
            });
            
            bool blnOptions = parser.ParseArguments(args, options);

            //bool blnOptions = CommandLine.Parser.Default.ParseArguments(args, options);

            if (!blnOptions)
            {
                Console.WriteLine(options.GetUsage());
                return;
            }

            // Options are available.  Load defaults.
            if (options.Location == "") options.Location = Directory.GetCurrentDirectory(); // @"D:\Users\Jay\Desktop\IsaiahScroll\tiles\"; 
            options.Location = options.Location.Trim().Replace("\"", "");
            options.Location += (options.Location.Substring(options.Location.Length - 1, 1) == "\\" ? "" : "\\"); // with trailing slash.

            options.Pattern = options.Pattern.Replace("\"", "").Trim();
            
            // Does the user want us to download?
            if (options.Download && options.DownloadRoot != "") DownloadWork(options);

            // Does the user want us to stitch?
            if (options.StitchImage) StitchImageWork(options);
        }
        
    }
}
