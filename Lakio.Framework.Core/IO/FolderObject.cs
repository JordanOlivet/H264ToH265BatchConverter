using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lakio.Framework.Core.IO
{
    public class FolderObject
    {
        public string Path { get; private set; }

        public DirectoryInfo Infos { get; private set; }

        public List<FileObject> Files { get; private set; }

        public List<FolderObject> Subfolders { get; private set; }

        public FolderObject(string path, bool recursiveInitrialize = false)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException("The path argument is not allowed tro be empty."); }

            Path = path;
            Infos = new DirectoryInfo(path);

            InitializeFiles();

            if(recursiveInitrialize)
            {
                InitializeSubfolders();
            }
        }

        public void InitializeFiles()
        {
            Files = new List<FileObject>();

            var fileInfos = Infos.EnumerateFiles();

            foreach (var f in fileInfos)
            {
                Files.Add(new FileObject(f.FullName));
            }
        }

        public void InitializeSubfolders()
        {
            Subfolders = new List<FolderObject>();

            var dirInfos = Infos.EnumerateDirectories();

            foreach (var d in dirInfos)
            {
                Subfolders.Add(new FolderObject(d.FullName));
            }
        }
    }
}
