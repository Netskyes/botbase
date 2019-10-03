using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenGL;

namespace GameMap
{
    using Engine;
    using System.Drawing;
    using System.Reflection;

    public partial class MainWindow : Window
    {
        private Loader Loader;
        private Renderer Renderer;
        private RawModel Model;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WFH_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void GlControl_ContextCreated(object sender, GlControlEventArgs e)
        {
            Gl.MatrixMode(MatrixMode.Projection);
            Gl.LoadIdentity();
            Gl.Ortho(0.0, 1024, 768, 0.0, 0.0, 1);
            Gl.MatrixMode(MatrixMode.Modelview);
            Gl.LoadIdentity();

            Loader = new Loader();
            Renderer = new Renderer();

            Model = Loader.Load(canvas);
        }

        private void GlControl_Render(object sender, GlControlEventArgs e)
        {
            Renderer.Prepare();

            var glsender = sender as GlControl;

            Renderer.AdjustDisplay(glsender.ClientSize.Width, glsender.ClientSize.Height);


            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var bmp = new Bitmap(System.IO.Path.Combine(path, "map.png"));
            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), 
                System.Drawing.Imaging.ImageLockMode.ReadOnly, 
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            uint textId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, textId);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, 
                InternalFormat.Rgb8, bmp.Width, bmp.Height, 0, OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, bmpData.Scan0);
            Gl.Enable(EnableCap.Texture2d);
            Gl.Enable(EnableCap.TextureGenS);
            Gl.Enable(EnableCap.TextureGenT);

            bmp.UnlockBits(bmpData);
            

            Renderer.Render(Model);
        }

        private static readonly double[] canvas = {
            0, 0,
            1024, 0,
            0, 768
        };

        private static readonly float[] tex = {
            0, 0,
            1024, 0,
            0, 768
        };

    }
}
