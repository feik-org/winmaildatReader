using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using WinmailDatReader.Core;

namespace WinmailDatReader.App;

public partial class MainWindow : Window
{
    // Fiktive Adresse (.invalid ist reserviert), unter der der HTML-Text ausgeliefert wird.
    // Alle anderen Anfragen werden blockiert.
    private const string BodyUri = "https://winmail.invalid/message.html";

    private const string ContentSecurityPolicy =
        "default-src 'none'; style-src 'unsafe-inline'; img-src data:; font-src data:; form-action 'none'; frame-src 'none'";

    private readonly MainViewModel _viewModel;
    private readonly Task<bool> _webViewReady;
    private string _currentHtml = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(new WpfDialogService(() => this));
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        DataContext = _viewModel;
        _webViewReady = InitializeWebViewAsync();
    }

    public MainViewModel ViewModel => _viewModel;

    /// <summary>Für Tests: Anzahl der blockierten Anfragen im HTML-Viewer.</summary>
    public int BlockedRequestCount { get; private set; }

    private async Task<bool> InitializeWebViewAsync()
    {
        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinmailDatReader", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await HtmlView.EnsureCoreWebView2Async(environment);

            var core = HtmlView.CoreWebView2;
            var settings = core.Settings;
            settings.IsScriptEnabled = false;
            settings.IsWebMessageEnabled = false;
            settings.AreDevToolsEnabled = false;
            settings.AreHostObjectsAllowed = false;
            settings.IsStatusBarEnabled = false;
            settings.IsPasswordAutosaveEnabled = false;
            settings.IsGeneralAutofillEnabled = false;

            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += (_, e) => OnWebResourceRequested(environment, e);
            core.NavigationStarting += OnNavigationStarting;
            core.NewWindowRequested += (_, e) => e.Handled = true;
            core.DownloadStarting += (_, e) => e.Cancel = true;
            core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            return true;
        }
        catch (Exception ex) when (ex is WebView2RuntimeNotFoundException or COMException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void OnWebResourceRequested(CoreWebView2Environment environment, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (string.Equals(e.Request.Uri, BodyUri, StringComparison.OrdinalIgnoreCase))
        {
            var content = new MemoryStream(Encoding.UTF8.GetBytes(_currentHtml));
            e.Response = environment.CreateWebResourceResponse(content, 200, "OK",
                "Content-Type: text/html; charset=utf-8\r\n" +
                $"Content-Security-Policy: {ContentSecurityPolicy}");
            return;
        }

        // Externe Inhalte (Bilder, Stylesheets, Tracking-Pixel …) werden nie geladen.
        BlockedRequestCount++;
        e.Response = environment.CreateWebResourceResponse(null, 403, "Blocked", string.Empty);
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (string.Equals(e.Uri, BodyUri, StringComparison.OrdinalIgnoreCase))
            return;

        e.Cancel = true;

        // Eine auf den HTML-Bereich gezogene Datei kommt als file://-Navigation an.
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) && uri.IsFile)
            Dispatcher.BeginInvoke(() => _viewModel.Open(uri.LocalPath));
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Message))
            _ = ShowBodyAsync();
    }

    private async Task ShowBodyAsync()
    {
        HtmlView.Visibility = RtfView.Visibility = TextView.Visibility = NoBodyText.Visibility = Visibility.Collapsed;
        BodyHint.Visibility = Visibility.Collapsed;

        if (_viewModel.Message is not { } message)
            return;

        var htmlAvailable = await _webViewReady;
        if (!ReferenceEquals(message, _viewModel.Message))
            return; // inzwischen wurde eine andere Datei geöffnet

        var format = DisplayHelpers.ChooseDisplayFormat(message, htmlAvailable);
        if (!htmlAvailable && message.HtmlBody != null)
            ShowHint("Die WebView2-Runtime ist nicht verfügbar. Der HTML-Text kann nicht formatiert angezeigt werden.");

        switch (format)
        {
            case BodyFormat.Html when htmlAvailable:
                _currentHtml = message.HtmlBody!;
                HtmlView.Visibility = Visibility.Visible;
                HtmlView.CoreWebView2.Navigate(BodyUri);
                break;

            case BodyFormat.Html:
                ShowText(message.HtmlBody!);
                break;

            case BodyFormat.Rtf:
                if (!TryShowRtf(message.RtfBody!))
                {
                    ShowHint("Der RTF-Text konnte nicht formatiert angezeigt werden.");
                    ShowText(message.TextBody ?? Encoding.Latin1.GetString(message.RtfBody!));
                }
                break;

            case BodyFormat.Text:
                ShowText(message.TextBody!);
                break;

            default:
                NoBodyText.Visibility = Visibility.Visible;
                break;
        }
    }

    private bool TryShowRtf(byte[] rtf)
    {
        try
        {
            var document = new FlowDocument();
            new TextRange(document.ContentStart, document.ContentEnd).Load(new MemoryStream(rtf), DataFormats.Rtf);
            RtfView.Document = document;
            RtfView.Visibility = Visibility.Visible;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or FormatException)
        {
            return false;
        }
    }

    private void ShowText(string text)
    {
        TextView.Text = text;
        TextView.ScrollToHome();
        TextView.Visibility = Visibility.Visible;
    }

    private void ShowHint(string text)
    {
        BodyHint.Text = text;
        BodyHint.Visibility = Visibility.Visible;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
            _viewModel.Open(files[0]);
    }

    private void Attachment_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            _viewModel.SaveSelectedAttachment();
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
