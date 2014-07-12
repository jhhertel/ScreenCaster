using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using SlimDX.Direct3D9;
using SlimDX;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ScreenCasterSystemTray
{

    class ScreenGrabThread 
    {
        static int screenWidth = 1920;
        static int screenHeight = 1080;
        static System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();

        static int x1, x2;
        static int y1, y2;

        static bool useSocket = true;

        public void run()
        {
            NetworkStream serverStream = null;
            QuickLZ lz = new QuickLZ();

            try
            {
                clientSocket.Connect(ScreenCasterSystemTray.Program.hostName, 6000);
                serverStream = clientSocket.GetStream();
            }
            catch (Exception e)
            {
                useSocket = false;
                MessageBox.Show("Could not connect to '" + ScreenCasterSystemTray.Program.hostName + "' port 6000");
                return;
            }
            DxScreenCapture sc = new DxScreenCapture();
            UInt32[] screenbuf = new UInt32[1920 * 1200];
            byte[] outputBuf = new byte[1024 * 10000];
            byte[] outputBufCompressed = new byte[1024 * 10000];

            
            bool done = false;
            try
            {
                while (!done)
                {
                    x1 = -1;
                    x2 = -1;
                    y1 = -1;
                    y2 = -1;

                    Surface s = sc.CaptureScreen();
                    System.Drawing.Point mPosition = Cursor.Position;
                    //s.LockRectangle(LockFlags.ReadOnly);
                    DataRectangle dr = s.LockRectangle(LockFlags.None);
                    DataStream gs = dr.Data;
                    for (int y = 0; y < screenHeight; y++)
                    {
                        for (int x = 0; x < screenWidth; x++)
                        {
                            UInt32 pixel = gs.Read<UInt32>();
                            int offset = ((y * screenWidth) + x);
                            if (pixel != screenbuf[offset])
                            {
                                differentPixel(x, y);

                            }
                            screenbuf[offset] = pixel;
                        }
                    }
                    gs.Dispose();

                    while ((x1 % 4) != 0) x1--;
                    while ((x2 % 4) != 0) x2++;
                    while ((y1 % 4) != 0) y1--;
                    while ((y2 % 4) != 0) y2++;

                    s.UnlockRectangle();
                    if (x1 == -4)
                    {
                        x1 = 0; x2 = 4; y1 = 0; y2 = 4;
                    }
                    if (x1 != -4)
                    {
                        DataStream ds = Surface.ToStream(s, ImageFileFormat.Bmp, new System.Drawing.Rectangle(x1, y1, x2 - x1, y2 - y1));
                        int dsLength = Convert.ToInt32(ds.Length);
                        ds.Read(outputBuf, 0, dsLength);
                        ds.Dispose();
                        uint compressedLength = lz.Compress(outputBuf, outputBufCompressed, dsLength);
                        int mouseX = mPosition.X;
                        int mouseY = mPosition.Y;
                        if (useSocket)
                        {
                            byte[] header = new byte[24];
                            header[0] = 1;
                            header[1] = 2;
                            header[2] = 3;
                            header[3] = 4;
                            int length = Convert.ToInt32(compressedLength);
                            intToBytes(header, 4, length);
                            intToBytes(header, 8, x1);
                            intToBytes(header, 12, y1);
                            intToBytes(header, 16, mouseX);
                            intToBytes(header, 20, mouseY);
                            serverStream.Write(header, 0, header.Length);

                            serverStream.Write(outputBufCompressed, 0, (int)compressedLength);
                            serverStream.Flush();
                        }


                    }

                    s.Dispose();
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Connection lost in screengrab");
                return;
            }
            
        }

        static void intToBytes(byte[] array,int offset,int i) {
            array[offset] = Convert.ToByte(i / (256 * 256 * 256));
            i -= (array[offset] * (256 * 256 * 256));

            array[offset+1] = Convert.ToByte(i / (256 * 256));
            i -= (array[offset+1] * (256 * 256));

            array[offset+2] = Convert.ToByte(i / (256));
            i -= (array[offset+2] * (256));

            array[offset+3] = Convert.ToByte(i);
            i -= (array[offset+3]);

        }
        static void differentPixel(int x, int y)
        {
            if (x1 == -1)
            {
                x1 = x;
                x2 = x+1;
            }
            else
            {
                x1 = Math.Min(x1, x);
                x2 = Math.Max(x2, x+1);
            }

            if (y1 == -1)
            {
                y1 = y;
                y2 = y+1;
            }
            else
            {
                y1 = Math.Min(y1, y);
                y2 = Math.Max(y2, y+1);
            }


        }
    }

    class QuickLZ
    {
        // The C library passes many integers through the C type size_t which is 32 or 64 bits on 32 or 64 bit 
        // systems respectively. The C# type IntPtr has the same property but because IntPtr doesn't allow 
        // arithmetic we cast to and from int on each reference. To pass constants use (IntPrt)1234.
        [DllImport("../quicklz150_32_1.dll")]
        public static extern IntPtr qlz_compress(byte[] source, byte[] destination, IntPtr size, byte[] scratch);
        [DllImport("../quicklz150_32_1.dll")]
        public static extern IntPtr qlz_decompress(byte[] source, byte[] destination, byte[] scratch);
        [DllImport("../quicklz150_32_1.dll")]
        public static extern IntPtr qlz_size_compressed(byte[] source);
        [DllImport("../quicklz150_32_1.dll")]
        public static extern IntPtr qlz_size_decompressed(byte[] source);
        [DllImport("../quicklz150_32_1.dll")]
        public static extern int qlz_get_setting(int setting);

        private byte[] state_compress;
        private byte[] state_decompress;

        public QuickLZ()
        {
            state_compress = new byte[qlz_get_setting(1)];
            if (QLZ_STREAMING_BUFFER == 0)
                state_decompress = state_compress;
            else
                state_decompress = new byte[qlz_get_setting(2)];
        }

        public uint Compress(byte[] Source,byte[] Destination,int length=-1)
        {
            
            uint s;
            if (length == -1) length = Source.Length;
            s = (uint)qlz_compress(Source, Destination, (IntPtr)length, state_compress);

            return s;
        }

        public byte[] Decompress(byte[] Source)
        {
            byte[] d = new byte[(uint)qlz_size_decompressed(Source)];
            uint s;

            s = (uint)qlz_decompress(Source, d, state_decompress);
            return d;
        }

        public uint SizeCompressed(byte[] Source)
        {
            return (uint)qlz_size_compressed(Source);
        }

        public uint SizeDecompressed(byte[] Source)
        {
            return (uint)qlz_size_decompressed(Source);
        }

        public uint QLZ_COMPRESSION_LEVEL
        {
            get
            {
                return (uint)qlz_get_setting(0);
            }
        }

        public uint QLZ_SCRATCH_COMPRESS
        {
            get
            {
                return (uint)qlz_get_setting(1);
            }
        }

        public uint QLZ_SCRATCH_DECOMPRESS
        {
            get
            {
                return (uint)qlz_get_setting(2);
            }
        }

        public uint QLZ_VERSION_MAJOR
        {
            get
            {
                return (uint)qlz_get_setting(7);
            }
        }

        public uint QLZ_VERSION_MINOR
        {
            get
            {
                return (uint)qlz_get_setting(8);
            }
        }


        public int QLZ_VERSION_REVISION
        {
            // negative means beta
            get
            {
                return (int)qlz_get_setting(9);
            }
        }

        public uint QLZ_STREAMING_BUFFER
        {
            get
            {
                return (uint)qlz_get_setting(3);
            }
        }


        public bool QLZ_MEMORY_SAFE
        {
            get
            {
                return qlz_get_setting(6) == 1 ? true : false;
            }
        }



    }
    public class DxScreenCapture
    {
        Device d;

        public DxScreenCapture()
        {
            PresentParameters present_params = new PresentParameters();
            present_params.Windowed = true;
            present_params.SwapEffect = SwapEffect.Discard;
            d = new Device(new Direct3D(), 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.SoftwareVertexProcessing, present_params);
        }

        public Surface CaptureScreen()
        {
            Surface s = Surface.CreateOffscreenPlain(d, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, Format.A8R8G8B8, Pool.Scratch);
            d.GetFrontBufferData(0, s);
            return s;
        }
    }
}
