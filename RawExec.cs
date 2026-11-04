using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using NXOpen;

// Standalone experimental transport. No reference to CADOperator assemblies.
public class RawExec
{
    public static void Main(string[] args)
    {
        int port = int.Parse(args[0]);
        if (port < 20700 || port > 20799) throw new ArgumentException("Dedicated experiment ports only");
        Session session = Session.GetSession();
        var json = new JavaScriptSerializer { MaxJsonLength = 2000000 };
        var listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
        listener.Start();
        Console.WriteLine("RAW_EXEC_READY " + port);
        // All requests execute serially on this NX entry thread.
        while (true)
        {
            var context = listener.GetContext();
            object response;
            try
            {
                if (context.Request.HttpMethod != "POST") throw new ArgumentException("POST required");
                string body;
                using (var reader = new StreamReader(context.Request.InputStream)) body = reader.ReadToEnd();
                var request = json.Deserialize<Dictionary<string, object>>(body);
                if (request.Count != 1 || !request.ContainsKey("code")) throw new ArgumentException("Only code is accepted");
                string code = request["code"] as string;
                if (string.IsNullOrWhiteSpace(code) || code.Length > 100000) throw new ArgumentException("code length must be 1..100000");
                response = Execute(session, code);
            }
            catch (Exception ex) { response = new { status = "error", error = ex.ToString() }; }
            byte[] bytes = Encoding.UTF8.GetBytes(json.Serialize(response));
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
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
