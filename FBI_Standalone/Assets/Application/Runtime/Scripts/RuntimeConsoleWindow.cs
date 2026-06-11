using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using UnityEngine;

public class RuntimeConsoleWindow : MonoBehaviour
{
    [Header("Console")]
    [Tooltip("Titre de la fenêtre PowerShell")]
    public string windowTitle = "Game Console";

    [Tooltip("Afficher aussi en éditeur Unity (normalement inutile)")]
    public bool enableInEditor = false;

    [Tooltip("Inclure la stack trace pour les erreurs")]
    public bool showStackTrace = true;

    [Tooltip("Nombre max de lignes de stack trace")]
    public int stackTraceLines = 4;

    private NamedPipeServerStream _pipe;
    private StreamWriter _writer;
    private Process _psProcess;
    private Thread _pipeThread;
    private bool _running;
    private string _pipeName;

    private System.Collections.Generic.Queue<(string color, string msg)> _queue
        = new System.Collections.Generic.Queue<(string, string)>();
    private readonly object _lock = new object();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoStart()
    {
        var go = new GameObject("[RuntimeConsole]");
        DontDestroyOnLoad(go);
        go.AddComponent<RuntimeConsoleWindow>();
    }

    void Awake()
    {
#if UNITY_EDITOR
        if (!enableInEditor)
        {
            Destroy(gameObject);
            return;
        }
#endif

#if !UNITY_STANDALONE_WIN
        // Named Pipe + PowerShell = Windows uniquement
        Destroy(gameObject);
        return;
#endif

        DontDestroyOnLoad(gameObject);
        OpenConsole();
        Application.logMessageReceivedThreaded += OnLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceivedThreaded -= OnLog;
        Shutdown();
    }

    void OnApplicationQuit()
    {
        Application.logMessageReceivedThreaded -= OnLog;
        Send("Cyan", "──── Application quittée ────");
        Shutdown();
    }

    private void OnLog(string message, string stackTrace, LogType type)
    {
        string color, prefix;
        switch (type)
        {
            case LogType.Warning:
                color = "Yellow"; prefix = "[WARN] "; break;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                color = "Red"; prefix = "[ERR]  "; break;
            default:
                color = "Gray"; prefix = "[LOG]  "; break;
        }

        Enqueue(color, prefix + message);

        if (showStackTrace && !string.IsNullOrEmpty(stackTrace) &&
            (type == LogType.Error || type == LogType.Exception))
        {
            var lines = stackTrace.Split('\n');
            int limit = Mathf.Min(lines.Length, stackTraceLines);
            for (int i = 0; i < limit; i++)
            {
                var l = lines[i].Trim();
                if (!string.IsNullOrEmpty(l))
                    Enqueue("DarkRed", "       " + l);
            }
        }
    }

    private void Enqueue(string color, string msg)
    {
        lock (_lock)
            _queue.Enqueue((color, msg));
    }

    private void PipeFlushLoop()
    {
        while (_running)
        {
            (string color, string msg) item = default;
            bool hasItem = false;

            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    hasItem = true;
                }
            }

            if (hasItem)
            {
                try { _writer?.WriteLine($"{item.color}|{item.msg}"); }
                catch { _running = false; break; }
            }
            else
            {
                Thread.Sleep(16); // ~60 fps de flush
            }
        }
    }

    private void OpenConsole()
    {
        _pipeName = "UnityRuntime_" + Guid.NewGuid().ToString("N").Substring(0, 8);

        string ps = $@"
$host.UI.RawUI.WindowTitle = '{windowTitle}'
$host.UI.RawUI.BackgroundColor = 'Black'
$host.UI.RawUI.ForegroundColor = 'Gray'
try {{ $host.UI.RawUI.WindowSize  = New-Object System.Management.Automation.Host.Size(120, 40) }} catch {{}}
try {{ $host.UI.RawUI.BufferSize = New-Object System.Management.Automation.Host.Size(120, 3000) }} catch {{}}
Clear-Host
Write-Host '  {windowTitle}' -ForegroundColor Cyan
Write-Host ('  ' + [string]::new([char]0x2500, 80)) -ForegroundColor DarkGray
Write-Host ''
try {{
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream('.', '{_pipeName}', [System.IO.Pipes.PipeDirection]::In)
    $pipe.Connect(10000)
    $reader = New-Object System.IO.StreamReader($pipe)
    while (-not $reader.EndOfStream) {{
        $line = $reader.ReadLine()
        if ($null -eq $line) {{ break }}
        $sep  = $line.IndexOf('|')
        if ($sep -ge 0) {{ $color = $line.Substring(0, $sep); $msg = $line.Substring($sep + 1) }}
        else             {{ $color = 'Gray'; $msg = $line }}
        $ts = Get-Date -Format 'HH:mm:ss'
        Write-Host ""[$ts] "" -ForegroundColor DarkGray -NoNewline
        Write-Host $msg -ForegroundColor $color
    }}
    $reader.Dispose(); $pipe.Dispose()
}} catch {{ Write-Host ""Erreur : $_"" -ForegroundColor Red }}
Write-Host ''
Write-Host '  Appuyez sur une touche pour fermer...' -ForegroundColor DarkGray
$null = $host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
";
        byte[] bytes = System.Text.Encoding.Unicode.GetBytes(ps);
        string b64 = Convert.ToBase64String(bytes);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -EncodedCommand {b64}",
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal
        };

        try { _psProcess = Process.Start(psi); }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[RuntimeConsole] Impossible de lancer PowerShell : {ex.Message}");
            return;
        }

        _running = true;
        _pipeThread = new Thread(() =>
        {
            try
            {
                _pipe = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );

                var ar = _pipe.BeginWaitForConnection(null, null);
                ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10));

                if (!_pipe.IsConnected) { _running = false; return; }

                _writer = new StreamWriter(_pipe) { AutoFlush = true };
                Send("Cyan", $"Connecté — {DateTime.Now:HH:mm:ss}");

                PipeFlushLoop(); 
            }
            catch { _running = false; }
        });
        _pipeThread.IsBackground = true;
        _pipeThread.Start();
    }

    private void Send(string color, string msg) => Enqueue(color, msg);

    private void Shutdown()
    {
        _running = false;
        try { _writer?.Dispose(); } catch { }
        try { _pipe?.Dispose(); } catch { }
        _pipeThread?.Join(500);
    }
}