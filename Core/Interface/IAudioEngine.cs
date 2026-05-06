using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vcv_etagere_remaster.Core.Interface
{
    public interface IAudioEngine
    {
        void FillBuffer(float[] buffer, int offset, int count);
    }
}
