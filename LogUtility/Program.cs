string _baseDirectory = "E:\\SoundMonitor_Output";
string[] _ommitFolders = new string[] { "_test" };

string _outputFilePath = Path.Combine(_baseDirectory, "report.txt");

if (File.Exists(_outputFilePath))
{
    File.Delete(_outputFilePath);
}

var directories = Directory.GetDirectories(_baseDirectory);
foreach (var directory in directories)
{
    var folderName = new DirectoryInfo(directory).Name;
    if (!_ommitFolders.Contains(folderName))
    {
        File.AppendAllLines(_outputFilePath, new[] { folderName, "" });

        var files = Directory.GetFiles(directory);
        for (int i = 0; i < files.Length; i++)
        {
            var currentFile = files[i];
            if (Path.GetExtension(currentFile) == ".txt")
            {
                var audioFileName = string.Empty;

                var previousFileIndex = i - 1;
                if (previousFileIndex >= 0)
                {
                    audioFileName = files[previousFileIndex];
                }

                var logFileContents = File.ReadAllLines(currentFile).ToList();
                logFileContents.Add($"Audio    : {folderName}\\{Path.GetFileName(audioFileName)}");
                logFileContents.Add("");

                File.AppendAllLines(_outputFilePath, logFileContents);
            }
        }

        File.AppendAllLines(_outputFilePath, new[] { "----------------------" });
    }
}