using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeterMonitor.Reader
{
    public interface IWriteFile
    {
        IWriteFile WithContents(string contents);
        IWriteFile WithPath(string path);
        IWriteFile WithFilename(string filename);
        void Write();
    }
}
