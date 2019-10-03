using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGL;

namespace GameMap.Engine
{
    public class Loader : IDisposable
    {
        private readonly List<uint> vaos = new List<uint>();
        private readonly List<uint> vbos = new List<uint>();

        public RawModel Load(double[] positions)
        {
            uint vaoId = CreateVao();
            AddDataToBuffer(0, positions);
            UnbindVao();

            return new RawModel(vaoId, positions.Length / 2);
        }

        private void AddDataToBuffer(uint index, double[] data)
        {
            uint vboId = Gl.GenBuffer();
            vbos.Add(vboId);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, vboId);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(8 * data.Length), data, BufferUsage.StaticDraw);
            Gl.VertexAttribPointer(index, 2, VertexAttribType.Double, false, 0, IntPtr.Zero);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private uint CreateVao()
        {
            uint vaoId = Gl.GenVertexArray();
            vaos.Add(vaoId);
            Gl.BindVertexArray(vaoId);

            return vaoId;
        }

        private void UnbindVao()
        {
            Gl.BindVertexArray(0);
        }

        public void Dispose()
        {
            vaos.ForEach(x => Gl.DeleteVertexArrays(vaos.ToArray()));
            vbos.ForEach(x => Gl.DeleteBuffers(vbos.ToArray()));
        }
    }
}
