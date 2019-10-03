using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGL;

namespace GameMap.Engine
{
    public class Renderer
    {
        public void Prepare()
        {
            Gl.ClearColor(1, 0, 0, 1);
            Gl.Clear(ClearBufferMask.ColorBufferBit);
        }

        public void Render(RawModel model)
        {
            Gl.BindVertexArray(model.VaoId);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, model.VertexCount);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
        }

        public void AdjustDisplay(int width, int height, int x = 0, int y = 0)
        {
            Gl.Viewport(x, y, width, height);
        }
    }
}
