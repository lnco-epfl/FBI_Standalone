using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using UnityEngine;

public class RuntimeConsoleWindow : MonoBehaviour
{
    [Header("Console")]
    public string windowTitle = "FBI Console";
    public bool enableInEditor = false;
    public bool showStackTrace = true;
    public int stackTraceLines = 4;

    private NamedPipeServerStream pipe;
    private StreamWriter writer;
    private Process psProcess;
    private Thread pipeThread;
    private bool running;
    private string pipeName;

    private System.Collections.Generic.Queue<(string color, string msg)> queue = new System.Collections.Generic.Queue<(string, string)>();
    private readonly object @lock = new object();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoStart()
    {
        var go = new GameObject("RuntimeConsole");
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

        Send("Cyan", "──── Application quit ────");
        Shutdown();
    }

    private void OnLog(string message, string stackTrace, LogType type)
    {
        string color, prefix = string.Empty;
        switch (type)
        {
            case LogType.Warning:
                color = "Yellow"; prefix = "[Warning] "; break;
            case LogType.Error:
                color = "Red"; prefix = "[Error] "; break;
            case LogType.Exception:
                color = "Red"; prefix = "[Error] "; break;
            case LogType.Assert:
                color = "Red"; prefix = "[Error] "; break;
            default:
                color = "Gray"; break;
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
        lock (@lock)
            queue.Enqueue((color, msg));
    }

    private void PipeFlushLoop()
    {
        while (running)
        {
            (string color, string msg) item = default;
            bool hasItem = false;

            lock (@lock)
            {
                if (queue.Count > 0)
                {
                    item = queue.Dequeue();
                    hasItem = true;
                }
            }

            if (hasItem)
            {
                try { writer?.WriteLine($"{item.color}|{item.msg}"); }
                catch { running = false; break; }
            }
            else
            {
                Thread.Sleep(16); // ~60 fps de flush
            }
        }
    }

    private void OpenConsole()
    {
        pipeName = "UnityRuntime_" + Guid.NewGuid().ToString("N").Substring(0, 8);

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
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream('.', '{pipeName}', [System.IO.Pipes.PipeDirection]::In)
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

        try { psProcess = Process.Start(psi); }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[RuntimeConsole] Impossible de lancer PowerShell : {ex.Message}");
            return;
        }

        running = true;
        pipeThread = new Thread(() =>
        {
            try
            {
                pipe = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );

                var ar = pipe.BeginWaitForConnection(null, null);
                ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10));

                if (!pipe.IsConnected) { running = false; return; }

                writer = new StreamWriter(pipe) { AutoFlush = true };
                //Send("Cyan", $"Connecté — {DateTime.Now:HH:mm:ss}");

                PipeFlushLoop();
            }
            catch { running = false; }
        });
        pipeThread.IsBackground = true;
        pipeThread.Start();
    }

    private void Send(string color, string msg) => Enqueue(color, msg);

    private void Shutdown()
    {
        running = false;
        try { writer?.Dispose(); } catch { }
        try { pipe?.Dispose(); } catch { }
        pipeThread?.Join(500);

        try
        {
            if (psProcess != null && !psProcess.HasExited)
            {
                psProcess.Kill();
            }
        }
        catch { }
        finally
        {
            psProcess?.Dispose();
            psProcess = null;
        }
    }
}