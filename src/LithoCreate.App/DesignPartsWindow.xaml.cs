using System.IO;
using System.Windows;
using System.Windows.Controls;
using LithoCreate.App.Services;
using LithoCreate.App.ViewModels;
using LithoCreate.Core;
using Microsoft.Win32;

namespace LithoCreate.App;

public partial class DesignPartsWindow : Window
{
    readonly StudioViewModel studio;
    CancellationTokenSource? export;

    public DesignPartsWindow(StudioViewModel studio)
    {
        this.studio = studio;
        InitializeComponent();
        WindowWorkArea.KeepVisible(this);
        Heading.Text = studio.SelectedShape.Name;
        Instructions.Text = DesignParts.Instructions(studio.Model.Shape);
        PrinterText.Text = $"Einzelteile für {studio.Printer.Name}. Das Archiv enthält die Druckdateien und die Montageanleitung.";
        MotifPanel.Visibility = studio.Model.HasWallMotifs ? Visibility.Visible : Visibility.Collapsed;
        if (studio.Model.HasWallMotifs)
        {
            WallChoice.ItemsSource = DesignParts.WallNames;
            WallChoice.SelectedIndex = 0;
        }
        Closing += (_, args) =>
        {
            if (export is null) return;
            args.Cancel = true;
            export.Cancel();
        };
    }

    void WallChanged(object sender, SelectionChangedEventArgs e) => RefreshWall();

    void RefreshWall()
    {
        int side = WallChoice.SelectedIndex;
        if (side < 0) return;
        var motif = studio.Project.WallMotifs[side];
        try
        {
            WallPreview.Source = motif is null ? studio.Image : ImageProcessor.Decode(motif, 300);
            WallSource.Text = motif is null ? "Projektfoto" : "Eigenes Wandmotiv";
        }
        catch (Exception ex)
        {
            WallPreview.Source = null;
            WallSource.Text = "Wandmotiv kann nicht angezeigt werden.";
            Status.Text = ex.Message;
        }
    }

    void OpenWall(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Bilddateien|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.webp|Alle Dateien|*.*" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            if (new FileInfo(dialog.FileName).Length > 64 * 1024 * 1024) throw new InvalidDataException("Die Bilddatei ist größer als 64 MB.");
            var photo = ImageProcessor.Decode(File.ReadAllBytes(dialog.FileName), 1400);
            SetWall(ImageProcessor.Png(photo));
        }
        catch (Exception ex) { Status.Text = ex.Message; }
    }

    void UseText(object sender, RoutedEventArgs e)
    {
        string value = WallText.Text.Trim();
        if (value.Length == 0) { Status.Text = "Zuerst einen Text eingeben."; return; }
        try { SetWall(WallImages.Text(value, studio.Model.Width / studio.Model.Height)); }
        catch (Exception ex) { Status.Text = ex.Message; }
    }

    void ResetWall(object sender, RoutedEventArgs e) => SetWall(null);

    void SetWall(byte[]? bytes)
    {
        if (WallChoice.SelectedIndex < 0) return;
        if (bytes is { Length: > ProjectStore.WallImageLimit }) throw new InvalidDataException("Das Wandmotiv ist größer als 16 MB.");
        studio.Project.WallMotifs[WallChoice.SelectedIndex] = bytes;
        studio.Changed();
        Status.Text = "";
        RefreshWall();
    }

    async void ExportStl(object sender, RoutedEventArgs e) => await Export("stl");
    async void Export3Mf(object sender, RoutedEventArgs e) => await Export("3mf");
    void CancelWork(object sender, RoutedEventArgs e) => export?.Cancel();

    async Task Export(string format)
    {
        if (export is not null) return;
        var dialog = new SaveFileDialog { Filter = "ZIP-Archiv|*.zip", DefaultExt = ".zip", FileName = $"{studio.SelectedShape.Name}-{format.ToUpperInvariant()}.zip" };
        if (dialog.ShowDialog(this) != true) return;
        using var work = new CancellationTokenSource();
        export = work;
        Controls.IsEnabled = false;
        CancelExport.Visibility = Visibility.Visible;
        Status.Text = "Motive werden verarbeitet …";
        var progress = new Progress<string>(message => { if (export == work) Status.Text = message; });
        var settings = studio.Model.Clone();
        var printer = studio.Printer.Clone();
        var main = studio.OriginalBytes;
        var edits = studio.Edits.Clone();
        var bytes = (byte[]?[])studio.Project.WallMotifs.Clone();
        try
        {
            int count = await Task.Run(() =>
            {
                var primary = ModelComputation.ProcessForExport(main, settings, edits, work.Token).Gray;
                var walls = settings.HasWallMotifs ? WallImages.Decode(bytes, work.Token) : [];
                return DesignParts.Export(dialog.FileName, format, settings, primary, walls, printer, work.Token, progress);
            }, work.Token);
            Status.Text = $"{count} Druckdateien gespeichert · {dialog.FileName}";
        }
        catch (OperationCanceledException) { Status.Text = "Export abgebrochen."; }
        catch (Exception ex) { Status.Text = "Export fehlgeschlagen. " + ex.Message; }
        finally
        {
            export = null;
            CancelExport.Visibility = Visibility.Collapsed;
            Controls.IsEnabled = true;
        }
    }
}
