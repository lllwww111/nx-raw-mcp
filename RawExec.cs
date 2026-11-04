using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using NXOpen;

// Load this library in an interactive NX session. No CAD tool router is used.
public class RawExec
{
    private static GuiHost host;

    public static void Main()
    {
        // NX owns the entry-point arguments; they are not an HTTP port.
        const int port = 20790;
        if (host != null)
        {
            ShowMessage("Already listening on " + host.Endpoint);
            return;
        }
        Session session = Session.GetSession();
        var candidate = new GuiHost(session, port);
        try { candidate.Start(); }
        catch { candidate.Stop(); throw; }
        host = candidate;
        ShowMessage("Listening on " + host.Endpoint);
        // Return to NX immediately; the listener must not occupy the UI thread.
    }

    public static int GetUnloadOption(string arg)
    {
        return (int)Session.LibraryUnloadOption.Explicitly;
    }

    public static void UnloadLibrary(string arg)
    {
        if (host == null) return;
        host.Stop();
        host = null;
    }

    private static void ShowMessage(string message)
    {
        var window = Session.GetSession().ListingWindow;
        window.Open();
        window.WriteLine("NX Raw MCP: " + message);
    }

    private sealed class GuiHost
    {
        private readonly Session session;
        private readonly Control dispatcher;
        private readonly HttpListener listener;
        private volatile bool running;
        public readonly string Endpoint;

        // Constructed by NX on its UI thread, just like the existing GUI dispatcher.
        public GuiHost(Session currentSession, int port)
        {
            session = currentSession;
            dispatcher = new Control();
            var handle = dispatcher.Handle;
            Endpoint = "http://127.0.0.1:" + port + "/";
            listener = new HttpListener();
            listener.Prefixes.Add(Endpoint);
        }

        public void Start()
        {
            listener.Start(); // A busy port is an error, not a reason to switch ports.
            running = true;
            var thread = new Thread(Listen);
            thread.IsBackground = true;
            thread.Name = "NX Raw MCP listener";
            thread.Start();
        }

        public void Stop()
        {
            running = false;
            listener.Close();
            dispatcher.Dispose();
            // Do not join here: the listener may be waiting for this UI thread.
        }

        private void Listen()
        {
            var json = new JavaScriptSerializer { MaxJsonLength = 2000000 };
            while (running)
            {
                HttpListenerContext context;
                try { context = listener.GetContext(); }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
                try
                {
                    object response;
                    try
                    {
                        if (context.Request.HttpMethod != "POST") throw new ArgumentException("POST required");
                        string body;
                        using (var reader = new StreamReader(context.Request.InputStream)) body = reader.ReadToEnd();
                        var request = json.Deserialize<Dictionary<string, object>>(body);
                        if (request == null || request.Count != 1 || !request.ContainsKey("code"))
                            throw new ArgumentException("Only code is accepted");
                        string code = request["code"] as string;
                        if (string.IsNullOrWhiteSpace(code) || code.Length > 100000)
                            throw new ArgumentException("code length must be 1..100000");
                        // Receive HTTP off-thread; execute all NX API calls on the UI thread.
                        response = dispatcher.Invoke(new Func<object>(delegate
                        {
                            if (!running) throw new InvalidOperationException("NX Raw MCP has stopped.");
                            return Execute(session, code);
                        }));
                    }
                    catch (Exception ex) { response = new { status = "error", error = ex.ToString() }; }
                    byte[] bytes = Encoding.UTF8.GetBytes(json.Serialize(response));
                    context.Response.ContentType = "application/json; charset=utf-8";
                    context.Response.ContentLength64 = bytes.Length;
                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
                finally
                {
                    try { context.Response.Close(); } catch { }
                }
            }
        }
    }

    private static object Execute(Session session, string code)
    {
        string dir = Path.Combine(Path.GetTempPath(), "nx_raw_exec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string source = Path.Combine(dir, "journal.cs");
        string output = Path.Combine(dir, "result.txt");
        string error = Path.Combine(dir, "error.txt");
        try
        {
            File.WriteAllText(source, @"
using System;
using System.IO;
using System.Globalization;
using NXOpen;
using NXOpen.UF;
public class RawJournal {
 public static void Main(string[] args) {
  Session session = Session.GetSession();
  UFSession ufSession = UFSession.GetUFSession();
  Part workPart = session.Parts.Work;
  Part displayPart = session.Parts.Display;
  object result = null;
  try {
" + code + @"
   File.WriteAllText(args[0], Convert.ToString(result, CultureInfo.InvariantCulture));
  } catch(Exception ex) { File.WriteAllText(args[1], ex.ToString()); throw; }
 }
 public static int GetUnloadOption(string dummy) { return (int)Session.LibraryUnloadOption.Immediately; }
}
", new UTF8Encoding(false));
            var info = session.JournalManager.PlayDotNetJournal(source, new[] { output, error });
            string failure = File.Exists(error) ? File.ReadAllText(error) : info.ErrorMessage;
            if (!string.IsNullOrWhiteSpace(failure))
                return new { status = "error", error = failure, warning = info.WarningMessage };
            return new { status = "success", result = File.Exists(output) ? File.ReadAllText(output) : "", warning = info.WarningMessage };
        }
        finally
        {
            // Only our generated transport files are cleaned; never NX model state.
            foreach (string file in new[] { source, output, error })
                try { if (File.Exists(file)) File.Delete(file); } catch { }
            try { Directory.Delete(dir, false); } catch { }
        }
    }
}
