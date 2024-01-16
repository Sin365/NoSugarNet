using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoSugarNet.ServerCore.Common
{
    /// <summary>
    /// 压缩适配器
    /// </summary>
    public class CompressAdapter
    {
        IDataCompress mIDataCompress;
        public CompressAdapter(ushort type)
        {
            switch (type)
            {
                //不压缩
                case 0:
                    mIDataCompress = new NoCompress();
                    break;
                    //TODO 其他压缩对比
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
}
