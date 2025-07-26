using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVDiff
{
    public static class FilesUtils
    {
        public static System.IO.FileInfo CopyFile(this System.IO.FileInfo source)
        {
            string filename = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{source.Name}_copy.csv");
            System.IO.File.Copy(source.FullName, filename, true);
            return new System.IO.FileInfo(filename);
        }

        public static bool IsFileLocked(this FileInfo file)
        {
            try
            {
                using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            //file is not locked
            return false;
        }
    }
}
