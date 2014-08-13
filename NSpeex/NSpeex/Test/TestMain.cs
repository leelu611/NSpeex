using System;
using System.Collections.Generic;
using System.Text;

namespace NSpeex.Test
{
    class TestMain
    {
        public void Test()
        {
            IEncoder encoder = new NbEncoder();
            
            encoder.Dtx = true;

            bool dtx = encoder.Dtx;

        }
    }
}
