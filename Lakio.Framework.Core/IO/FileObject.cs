
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

        public string FullName { get; set; }

        public FileInfo Infos { get; set; }

        public FileObject(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException("The path argument is not allowed to be empty."); }

            FullName = path;
            Infos = new FileInfo(path);
        }

        public bool Rename(string newName, bool overwrite = false)
        {
            string newPath = PathIO.Combine(Infos.DirectoryName, newName + Extension);

            try 
            {
                MoveTo(newPath, overwrite);
            }
            catch (Exception e)
            {

                return false;
            }

            return true;
        }

        public void Delete()
        {
            try
            {
                Infos.Delete();
            }
            catch { }
        }

        public void MoveTo(string destination, bool overwrite = false)
        {
            Infos.MoveTo(destination, overwrite);
        }
    }
}
