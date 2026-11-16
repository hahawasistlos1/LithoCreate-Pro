using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

internal static class SetupLauncher
{
    private const string ResourceName = "LithoCreate.Setup.msi";

    [STAThread]
    private static int Main(string[] args)
    {
        bool quiet = args.Length == 1 && string.Equals(args[0], "/quiet", StringComparison.OrdinalIgnoreCase);
        if (args.Length != 0 && !quiet)
        {
            ShowError("Setup per Doppelklick starten oder /quiet für eine unbeaufsichtigte Installation verwenden.");
            return 87;
        }

        string directory = Path.Combine(Path.GetTempPath(), "LithoCreate-Setup-" + Guid.NewGuid().ToString("N"));
        string package = Path.Combine(directory, "LithoCreate Pro.msi");
        try
        {
            Directory.CreateDirectory(directory);
            using (Stream payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
            {
                if (payload == null) throw new InvalidDataException("Das eingebettete Installationspaket fehlt.");
                using (var destination = new FileStream(package, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    payload.CopyTo(destination);
            }

            var installer = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "msiexec.exe"))
            {
                Arguments = "/i \"" + package + "\"" + (quiet ? " /qn /norestart" : ""),
                UseShellExecute = false,
                WorkingDirectory = directory
            };
            using (Process process = Process.Start(installer))
            {
                if (process == null) throw new InvalidOperationException("Windows Installer konnte nicht gestartet werden.");
                process.WaitForExit();
                return process.ExitCode;
            }
        }
        catch (Exception ex)
        {
            if (!quiet) ShowError(ex.Message);
            return 1603;
        }
        finally
        {
            try
            {
                File.Delete(package);
                Directory.Delete(directory);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void ShowError(string message)
    {
        MessageBox.Show(message, "LithoCreate Pro · Installation", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
