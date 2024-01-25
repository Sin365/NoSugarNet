using System.IO.Compression;

namespace NoSugarNet.DataHelper
{
    public enum E_CompressAdapter
    {
        //不压缩
        None = 0,
        //GIPZ
        GZIP_Plan1 = 1,
    }

    /// <summary>
    /// 压缩适配器
    /// </summary>
    public class CompressAdapter
    {
        IDataCompress mIDataCompress;
        public CompressAdapter(E_CompressAdapter type)
        {
            switch (type)
            {
                //不压缩
                case E_CompressAdapter.None:
                    mIDataCompress = new NoCompress();
                    break;
                //GZIP Plan1
                case E_CompressAdapter.GZIP_Plan1:
                    mIDataCompress = new GZipCompress();
                    break;
                    //TODO 其他压缩对比
                    //……
                default:
                    mIDataCompress = new NoCompress();
                    break;
            }
        }

        public byte[] Compress(byte[] data)
        {
            return mIDataCompress.Compress(data);
        }
        public byte[] Decompress(byte[] data)
        {
            return mIDataCompress.Decompress(data);
        }
    }


    public interface IDataCompress
    {
        public byte[] Compress(byte[] data);
        public byte[] Decompress(byte[] data);
    }

    public class NoCompress : IDataCompress
    {
        public byte[] Compress(byte[] data)
        {
            return data;
        }
        public byte[] Decompress(byte[] data)
        {
            return data;
        }
    }

    public class GZipCompress : IDataCompress
    {
        public byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        public byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
    }

}
