using System.IO;

namespace MeterMonitor.Reader
{
    public class FileWriter : IWriteFile
    {
        private string _contents = string.Empty;
        private string _path = string.Empty;
        private string _filename = string.Empty;

        public IWriteFile WithContents(string contents)
        {
            _contents = contents; //.Replace("\n","\r\n");

            return this;
        }

        public IWriteFile WithPath(string path)
        {
            _path = path;

            return this;
        }

        public IWriteFile WithFilename(string filename)
        {
            _filename = filename;

            return this;
        }

        public void Write()
        {
            string fullPath = Path.Combine(_path, _filename);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, _contents);
        }
    }
}
