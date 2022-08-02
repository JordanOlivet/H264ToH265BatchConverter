using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PathIO = System.IO.Path;

namespace Lakio.Framework.Core.IO
{
    public class FileObject
    {
        public string Name { get => Infos?.Name; }
        public string Extension { get => Infos?.Extension; }
        public long Size { get => Infos?.Length ?? 0; }

        /// <summary>
        /// Custom status goes here
        /// </summary>
        public object Status { get; set; }

        public string Path { get; set; }

        public FileInfo Infos { get; set; }

        public FileObject(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException("The path argument is not allowed to be empty."); }

            Path = path;
            Infos = new FileInfo(path);
        }

        public bool Rename(string newName, bool overwrite = false)
        {
            string newPath = PathIO.Combine(Infos.DirectoryName, newName + Extension);

            try 
            {
                Infos.MoveTo(newPath, overwrite);
            }
            catch(Exception e)
            {

                return false;
            }

            return true;
        }
    }
}
