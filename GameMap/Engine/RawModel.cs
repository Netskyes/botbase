namespace GameMap.Engine
{
    public class RawModel
    {
        public uint VaoId { get; set; }
        public int VertexCount { get; set; }

        public RawModel(uint vaoId, int vertexCount)
        {
            this.VaoId = vaoId;
            this.VertexCount = vertexCount;
        }
    }
}
