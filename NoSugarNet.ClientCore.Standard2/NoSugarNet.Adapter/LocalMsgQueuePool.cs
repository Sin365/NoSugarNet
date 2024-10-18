using System.Collections.Generic;

namespace NoSugarNet.Adapter
{
    public class IdxWithMsg
    {
        public byte Idx;
        public byte[] data;
    }

    public class MsgQueuePool
    {
        public static MsgQueuePool _MsgPool = new MsgQueuePool(1000);

        Queue<IdxWithMsg> msg_pool;

        public MsgQueuePool(int capacity)
        {
            msg_pool = new Queue<IdxWithMsg>(capacity);
        }

        /// <summary>
        /// 向 Queue 的末尾添加一个对象。
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(IdxWithMsg item)
        {
            lock (msg_pool)
            {
                item.Idx = 0;
                item.data = null;
                msg_pool.Enqueue(item);
            }
        }

        //移除并返回在 Queue 的开头的对象。
        public IdxWithMsg Dequeue()
        {
            lock (msg_pool)
            {
                if(msg_pool.Count > 0)
                    return msg_pool.Dequeue();
                return new IdxWithMsg();
            }
        }

        public int Count
        {
            get { return msg_pool.Count; }
        }

        public void Clear()
        {
            msg_pool.Clear();
        }
    }
}
