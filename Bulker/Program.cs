using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulker
{
    class Program
    {
        static void Main(string[] args)
        {
            Bulker bulker = new Bulker();

            Task tsk = bulker.Run(args);

            tsk.Wait();
        }
    }
}
