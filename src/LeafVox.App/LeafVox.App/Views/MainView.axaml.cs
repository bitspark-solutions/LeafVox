using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LeafVox.App.ViewModels;

namespace LeafVox.App.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Hook();
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void Hook()
    {
        if (Vm is { } vm)
        {
            vm.ScrollToSentence += index =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (!vm.UsePageView) SentenceList.ScrollIntoView(index);
                });
            vm.ScrollToPage += (page, yFraction, force) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ScrollToPage(vm, page, yFraction, force);
            });
            PageRepeater.ElementPrepared += (_, e) =>
            {
                if (e.Element.DataContext is PageVm p)
                    vm.RequestPageRender(p.PageNumber);
            };
        }
    }

    /// <summary>
    /// Vertical layout is deterministic (fixed pixel heights + uniform gap), so the scroll
    /// offset for any page/y-fraction can be computed directly — works across virtualized
    /// pages without realized-container lookups. Scrolls only when the highlight would be
    /// out of view, so in-page line stepping doesn't yank the viewport.
    /// </summary>
    private void ScrollToPage(MainViewModel vm, int page1Based, double yFraction, bool force)
    {
        if (vm.Pages.Count == 0 || page1Based < 1 || page1Based > vm.Pages.Count) return;
        double gap = MainViewModel.PageGapPx;
        double absolute = 0;
        for (int i = 0; i < page1Based - 1; i++)
            absolute += vm.Pages[i].DisplayHeight + gap;
        absolute += yFraction * vm.Pages[page1Based - 1].DisplayHeight;

        if (!force)
        {
            double offset = PageScroll.Offset.Y;
            double viewport = PageScroll.Bounds.Height;
            bool visible = absolute >= offset + viewport * 0.15 && absolute <= offset + viewport * 0.95;
            if (visible) return;
        }
        double anchor = PageScroll.Bounds.Height * 0.33;
        double y = Math.Max(0, absolute - anchor);
        PageScroll.Offset = new global::Avalonia.Point(PageScroll.Offset.X, y);
    }

    private void OnPageDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (Vm is null || sender is not Visual page) return;
        var p = e.GetPosition(page);
        if (page.DataContext is PageVm pageVm)
            Vm.PagePointDoubleTapped(pageVm.PageNumber, p.X, p.Y);
    }

    private void OnBookmarksOpening(object? sender, EventArgs e)
    {
        if (Vm is null || sender is not MenuFlyout flyout) return;
        flyout.Items.Clear();
        foreach (var idx in Vm.Bookmarks)
        {
            var s = idx < Vm.Sentences.Count ? Vm.Sentences[idx].Text : "";
            var preview = s.Length > 42 ? s[..42] + "…" : s;
            var item = new MenuItem { Header = $"Line {idx + 1}: {preview}" };
            int captured = idx;
            item.Click += (_, _) => _ = Vm.GoToBookmarkAsync(captured);
            flyout.Items.Add(item);
        }
        if (flyout.Items.Count == 0)
            flyout.Items.Add(new MenuItem { Header = "(no bookmarks — press ★)", IsEnabled = false });
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Vm is null) return;
        switch (e.Key)
        {
            case Key.Space:
                _ = Vm.TogglePlayCommand.ExecuteAsync(null);
                e.Handled = true;
                break;
            case Key.Right:
                Vm.NextCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Left:
                Vm.PreviousCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                Vm.StopCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.MediaPlayPause:
            case Key.MediaStop:
            case Key.MediaNextTrack:
            case Key.MediaPreviousTrack:
                switch (e.Key)
                {
                    case Key.MediaNextTrack: Vm.NextCommand.Execute(null); break;
                    case Key.MediaPreviousTrack: Vm.PreviousCommand.Execute(null); break;
                    case Key.MediaStop: Vm.StopCommand.Execute(null); break;
                    default: _ = Vm.TogglePlayCommand.ExecuteAsync(null); break;
                }
                e.Handled = true;
                break;
        }
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        var files = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a PDF",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("PDF documents") { Patterns = new[] { "*.pdf" } } },
        });
        if (files.Count > 0)
        {
            var path = files[0].TryGetLocalPath();
            if (path is not null) await Vm.OpenPdfAsync(path);
        }
    }

    private async void OnVoicesClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        var dialog = new VoiceManagerWindow { DataContext = new VoiceManagerViewModel(Vm.Voices, Vm.SetActiveVoice) };
        await dialog.ShowDialog((Window)TopLevel.GetTopLevel(this)!);
        dialog.Closed += (_, _) => Vm.Voices.Refresh();
    }

    private void OnRecentOpening(object? sender, EventArgs e)
    {
        if (Vm is null || sender is not Avalonia.Controls.MenuFlyout flyout) return;
        flyout.Items.Clear();
        foreach (var entry in Vm.Recents)
        {
            var item = new Avalonia.Controls.MenuItem { Header = entry.Title };
            var captured = entry;
            item.Click += (_, _) => _ = Vm.OpenRecentAsync(captured);
            flyout.Items.Add(item);
        }
        if (flyout.Items.Count == 0)
        {
            flyout.Items.Add(new Avalonia.Controls.MenuItem { Header = "(no recent files)", IsEnabled = false });
        }
    }

    private void OnLineDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (Vm?.ReadFromSelectedCommand.CanExecute(null) == true)
            Vm.ReadFromSelectedCommand.Execute(null);
    }
}
