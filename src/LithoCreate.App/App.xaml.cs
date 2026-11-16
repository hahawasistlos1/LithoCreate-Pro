using System.Windows;
using System.Globalization;
using System.IO;
using LithoCreate.App.Services;
namespace LithoCreate.App;
public partial class App : Application
{
    InstanceLock? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        CultureInfo.DefaultThreadCurrentCulture=CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentUICulture=CultureInfo.GetCultureInfo("de-DE");
        DispatcherUnhandledException+=(s,args)=> { MessageBox.Show(args.Exception.Message,"LithoCreate Pro · Fehler",MessageBoxButton.OK,MessageBoxImage.Error);args.Handled=true; };
        try
        {
            string directory=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LithoCreate Pro");
            instance=InstanceLock.TryAcquire(directory);
            if(instance==null){MessageBox.Show("LithoCreate Pro ist für diesen Datenordner bereits geöffnet. Bitte das bestehende Fenster verwenden.","LithoCreate Pro");Shutdown();return;}
        }
        catch(Exception ex){MessageBox.Show("Der lokale Datenordner konnte nicht geöffnet werden: "+ex.Message,"LithoCreate Pro");Shutdown(1);return;}
        base.OnStartup(e);
    }
    protected override void OnExit(ExitEventArgs e){instance?.Dispose();base.OnExit(e);}
}
