using NAudio.Wave;

// Declare global variables
bool _isCurrentlyRecording = false;
float _fractionThresholdBeforeRecord = 0.1F;
WaveFileWriter? _writer = null;
DateTime? _initiateRecordingTime = null;
DateTime? _recordingTimeoutStartTime = null;
double _recordingTimeoutSeconds = 30;
string _baseOutputFolder = $"E:\\SoundMonitor_Output";

#if DEBUG
_recordingTimeoutSeconds = 90;
_fractionThresholdBeforeRecord = 0.1F;
_baseOutputFolder = $"E:\\SoundMonitor_Output";
#endif

// Start the program
var _inputDevices = GetAudioInputDevices();

var selectedDeviceId = GetAudioInputDeviceSelection(_inputDevices);

var _waveIn = new WaveInEvent
{
    DeviceNumber = selectedDeviceId,
    //WaveFormat = new WaveFormat(rate: 44100, bits: 16, channels: 1), //high quality
    WaveFormat = new WaveFormat(rate: 8000, bits: 16, channels: 1), //phone quality
    BufferMilliseconds = 20
};

_waveIn.DataAvailable += WaveIn_DataAvailable;
_waveIn.StartRecording();

Console.WriteLine("\n(press any key to exit)");
Console.ReadKey();

if (_isCurrentlyRecording)
    StopRecordingToFile();

StopListeningToDevice();


void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
{
    Int16[] values = new Int16[e.Buffer.Length / 2];
    Buffer.BlockCopy(e.Buffer, 0, values, 0, e.Buffer.Length);

    // 32768 is maximum possible value of Buffer
    float fraction = (float)values.Max() / 32768;
    PrintMeterBar(fraction);

    if (_isCurrentlyRecording &&
        _recordingTimeoutStartTime.HasValue &&
        (DateTime.Now - _recordingTimeoutStartTime.Value).TotalSeconds >= _recordingTimeoutSeconds)
    {
        StopRecordingToFile();
    }
    else
    {
        if (fraction >= _fractionThresholdBeforeRecord)
        {
            _recordingTimeoutStartTime = DateTime.Now;

            RecordToFile(e);
        }
        else if (_isCurrentlyRecording)
        {
            RecordToFile(e);
        }
    }
}

void RecordToFile(WaveInEventArgs args)
{
    if (!_isCurrentlyRecording)
    {
        _isCurrentlyRecording = true;

        _initiateRecordingTime = DateTime.Now;

        var outputFolder = GetOutputRecordingFolder();
        var outputFilePath = Path.Combine(outputFolder, $"{DateTime.Now:HHmmss}.wav");

        _writer = new WaveFileWriter(outputFilePath, _waveIn.WaveFormat);
    }
    
    if (_writer != null)
    {
        _writer.Write(args.Buffer, 0, args.BytesRecorded);
    }
}

void StopListeningToDevice()
{
    _waveIn.StopRecording();
    _waveIn.Dispose();
}

void StopRecordingToFile()
{
    _isCurrentlyRecording = false;

    _writer?.Dispose();
    _writer = null;

    LogRecording();
}

void LogRecording()
{
    var outputFolder = GetOutputRecordingFolder();
    var outputFilePath = Path.Combine(outputFolder, $"{DateTime.Now:HHmmss}.txt");

    if (_initiateRecordingTime.HasValue)
    {
        var now = DateTime.Now;
        var difference = now - _initiateRecordingTime.Value;

        var logContents = new List<string>
        {
            $"Start    : {_initiateRecordingTime.Value:yyyyMMdd HH:mm:ss}",
            $"End      : {now:yyyyMMdd HH:mm:ss}",
            $"Duration : {(int)difference.TotalMinutes} minutes, {(int)difference.TotalSeconds % 60} seconds"
        };

        if (File.Exists(outputFilePath))
        {
            File.AppendAllLines(outputFilePath, logContents);
        }
        else
        {
            File.WriteAllLines(outputFilePath, logContents);
        }
    }
}

string GetOutputRecordingFolder()
{
    var outputFolder = Path.Combine(_baseOutputFolder, DateTime.Now.ToString("yyyyMMdd"));
    if (!Directory.Exists(outputFolder))
        Directory.CreateDirectory(outputFolder);

    return outputFolder;
}

void PrintMeterBar(float fraction)
{
    if (fraction < 0)
        fraction = 0;
    if (fraction > 1)
        fraction = 1;

    string bar = new('#', (int)(fraction * 60));
    string meter = "[" + bar.PadRight(50, '-') + "]";

    Console.CursorLeft = 0;
    Console.CursorVisible = false;
    Console.Write($"{meter} {fraction * 100:00.0}%");
}

List<AudioInputDevice> GetAudioInputDevices()
{
    var deviceList = new List<AudioInputDevice>();

    for (int i = -1; i < WaveIn.DeviceCount; i++)
    {
        var caps = WaveIn.GetCapabilities(i);

        deviceList.Add(new AudioInputDevice
        {
            Id = i,
            ProductName = caps.ProductName
        });
    }

    return deviceList;
}

int GetAudioInputDeviceSelection(List<AudioInputDevice> inputDevices)
{
    var deviceChoice = string.Empty;
    int deviceChoiceInt = -100;

    var inputDeviceIds = inputDevices.Select(d => d.Id).ToList();

    while (!int.TryParse(deviceChoice, out deviceChoiceInt)
        || !inputDeviceIds.Contains(deviceChoiceInt))
    {
        Console.WriteLine("Select an Audio Device to monitor:");

        foreach (var device in inputDevices)
        {
            Console.WriteLine($"{device.Id}: {device.ProductName}");
        }

        deviceChoice = Console.ReadLine();
    }

    return deviceChoiceInt;
}
