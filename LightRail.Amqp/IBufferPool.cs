namespace LightRail.Amqp
{
    public interface IBufferPool
    {
        bool TryGetByteBuffer(out ByteBuffer buffer);
        void FreeBuffer(ByteBuffer buffer);
    }
}
