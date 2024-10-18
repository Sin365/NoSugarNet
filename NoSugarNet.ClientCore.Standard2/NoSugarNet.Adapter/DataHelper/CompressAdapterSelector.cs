using NoSugarNet.Adapter.DataHelper;
using System.Collections.Generic;

namespace NoSugarNet.Adapter.DataHelper
{
    public static class CompressAdapterSelector
    {
        static Dictionary<E_CompressAdapter, CompressAdapter> mDictAdapter = new Dictionary<E_CompressAdapter, CompressAdapter>();

        public static CompressAdapter Adapter(E_CompressAdapter adptType)
        {
            if(mDictAdapter.ContainsKey(adptType))
                return mDictAdapter[adptType];

            mDictAdapter[adptType] = new CompressAdapter(adptType);
            return mDictAdapter[adptType];
        }
    }
}