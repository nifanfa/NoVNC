static class StreamExtensions
{
    public static byte[] ToArray(this Stream stream)
    {
        List<byte> result = new List<byte>();
        byte[] bytes = new byte[4096];
        for (; ; )
        {
            int length = stream.Read(bytes, 0, bytes.Length);
            result.AddRange(new Span<byte>(bytes, 0, length));
            if (length <= 0)
            {
                break;
            }
        }
        return result.ToArray();
    }
}