﻿namespace NoSugarNet.ServerCore
{
    public struct NetStatus
    {
        public int ClientUserCount;
        public int TunnelCount;
        public long srcReciveAllLenght;
        public long srcSendAllLenght;
        public long srcReciveSecSpeed;
        public long srcSendSecSpeed;
        public long tReciveAllLenght;
        public long tSendAllLenght;
        public long tReciveSecSpeed;
        public long tSendSecSpeed;
    }
}
