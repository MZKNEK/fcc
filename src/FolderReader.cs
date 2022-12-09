using System.Text;

namespace FCC
{
    internal class FolderReader
    {
        private readonly bool _verbose;
        private readonly bool _recurse;
        private readonly DirectoryInfo? _mainFolder;

        internal FolderReader(bool v, bool r, DirectoryInfo? folder)
        {
            _verbose = v;
            _recurse = r;
            _mainFolder = folder;
        }

        internal struct Stats
        {
            public int Files;
            public int Groups;
        }

        internal struct Output
        {
            public Output()
            {
                Result = new StringBuilder();
                Stas = new Stats();
            }

            public StringBuilder Result;
            public Stats Stas;
        }

        internal Output Analyze()
        {
            var o = new Output();
            var dirList = new List<DirectoryInfo>();

            if (_mainFolder is not null)
            {
                dirList.Add(_mainFolder);
                if (_recurse)
                {
                    dirList.AddRange(_mainFolder.GetDirectories());
                }
            }

            foreach (var d in dirList)
            {
                int cCnt = 0;
                int lcCnt = 0;
                string cName = "";
                string lcName = "";
                bool matched = false;
                foreach (var f in d.GetFiles())
                {
                    if (_verbose)
                    {
                        o.Stas.Files++;
                        o.Result.AppendLine(f.Name);
                        continue;
                    }

                    if (cCnt == 0)
                    {
                        cCnt++;
                        lcCnt = cCnt;
                        cName = f.Name;
                        continue;
                    }

                    if (f.Name.Contains(cName))
                    {
                        cCnt++;
                        lcCnt = cCnt;
                        continue;
                    }

                    var ncName = cName;
                    bool cont = false;
                    do
                    {
                        if (ncName.Length < 20 || matched)
                        {
                            o.Result.AppendLine(cName + $" x{cCnt}");
                            o.Stas.Files += cCnt;
                            matched = false;
                            cName = f.Name;
                            o.Stas.Groups++;
                            cont = true;
                            lcCnt = 1;
                            cCnt = 1;
                            break;
                        }
                        ncName = ncName.Truncate(ncName.Length - 2);
                    } while (!f.Name.Contains(ncName));

                    if (cont)
                    {
                        continue;
                    }

                    cCnt++;
                    lcCnt = cCnt;
                    matched = true;
                    cName = ncName;
                    lcName = cName;
                }

                if (lcName is not "")
                {
                    o.Result.AppendLine(lcName + $" x{lcCnt}");
                    o.Stas.Files += lcCnt;
                    o.Stas.Groups++;
                }
            }

            if (!_verbose)
            {
                o.Result.AppendLine("-----------------------------")
                .AppendLine($"TOTAL: {o.Stas.Files} FILES | {o.Stas.Groups} GROUPS");
            }

            return o;
        }
    }
}