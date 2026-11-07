using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafVox.App.Services;
using LeafVox.Core;
using LeafVox.Tts;

namespace LeafVox.App.ViewModels;

/// <summary>Root view-model: owns document parsing, the speech pipeline, library/settings and all transport state.</summary>
public sealed partial class MainViewModel : ViewModelBase
{
    private readonly ITtsEngine _engine;
    private readonly IAudioSink _sink;
    private readonly SpeechPipeline _pipeline;
    private readonly LibraryStore _library = new();
    private readonly AppSettings _settings = SettingsStore.Load();
    private readonly object _renderGate = new();
    private readonly HashSet<int> _renderPending = new();
    private readonly Queue<int> _renderedOrder = new();
    private DispatcherTimer? _sleepTimer;
    private readonly DispatcherTimer? _statusCooldown;
    private string? _currentPath;
    private int _saveCounter;
    private PageRenderer? _pages;
    private ReadingSpine? _spine;
    private int[] _scannedPages = Array.Empty<int>();
    private PageVm? _highlightedPage;
    private int _shownPage;
    private int _openEpoch;
    private bool _scrubbing;

    public const int RenderDpi = 150;
    /// <summary>Device pixels per PDF point at <see cref="RenderDpi"/>.</summary>
    public const double PxPerPt = RenderDpi / 72.0;
    /// <summary>Vertical gap between consecutive pages in the continuous viewer.</summary>
    public const double PageGapPx = 12;
    /// <summary>Rendered pages kept in memory for one open book (about 8 MB each at 150 DPI).</summary>
    private const int MaxRenderedPages = 24;

    public VoiceRepository Voices { get; }
    public ObservableCollection<SentenceVm> Sentences { get; } = new();
    public ObservableCollection<PageVm> Pages { get; } = new();

    /// <summary>Raised when the reader list must bring a sentence on screen (UI thread).</summary>
    public event Action<int>? ScrollToSentence;

    /// <summary>Raised when the page viewer should scroll so a page/y-fraction is on screen (UI thread); the bool forces the jump even when already visible (open/switch).</summary>
    public event Action<int, double, bool>? ScrollToPage;

    [ObservableProperty]
    private string _bookTitle = "LeafVox";

    [ObservableProperty]
    private string _status = "Open a PDF to begin.";

    [ObservableProperty]
    private int _currentIndex = -1;

    [ObservableProperty]
    private int _selectedIndex = -1;

    [ObservableProperty]
    private double _speed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PositionLabel))]
    [NotifyPropertyChangedFor(nameof(Recents))]
    [NotifyPropertyChangedFor(nameof(HasDocument))]
    [NotifyPropertyChangedFor(nameof(ScrubMax))]
    [NotifyCanExecuteChangedFor(nameof(ReadFromSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReadThisLineCommand))]
    private int _totalSentences;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _statusHot;

    [ObservableProperty]
    private int _scrub;

    [ObservableProperty]
    private string _voiceLabel = "voice: —";

    [ObservableProperty]
    private string _sleepLabel = "Sleep";

    [ObservableProperty]
    private bool _usePageView = true;

    public MainViewModel() : this(new SherpaOnnxEngine(), SinkFactory.CreateDefault(), new VoiceRepository()) { }

    public MainViewModel(ITtsEngine engine, IAudioSink sink, VoiceRepository voices)
    {
        _engine = engine;
        _sink = sink;
        Voices = voices;
        _speed = Math.Clamp(_settings.Speed, 0.5, 3.0);
        _pipeline = new SpeechPipeline(engine, sink) { Speed = (float)_speed };
        _pipeline.SentenceStarted += (_, i) => Dispatcher.UIThread.Post(() => SetCurrent(i));
        _pipeline.SentenceFinished += (_, _) => Dispatcher.UIThread.Post(RefreshTransport);
        _pipeline.Finished += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = false;
            Status = "Finished reading.";
            SavePositionNow();
        });
        _pipeline.Failed += (_, ex) => Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = false;
            Status = "Playback error: " + ex.Message;
        });

        // Boarding-pass discipline (donated by the gate-board challenger): a status
        // change flashes the accent and holds briefly before settling back to quiet.
        _statusCooldown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _statusCooldown.Tick += (_, _) =>
        {
            _statusCooldown!.Stop();
            StatusHot = false;
        };
    }

    public IReadOnlyList<LibraryEntry> Recents => _library.Entries;

    /// <summary>True once a document (or page set) is open — drives the empty state.</summary>
    public bool HasDocument => TotalSentences > 0;

    /// <summary>Whole-book scrubber upper bound.</summary>
    public int ScrubMax => Math.Max(TotalSentences - 1, 0);

    public string SpeedLabel => $"{Speed:0.##}×";

    public string ViewLabel => UsePageView ? "Flow" : "Page";

    public bool HasScannedPages => _scannedPages.Length > 0;

    [RelayCommand]
    private async Task OcrScannedAsync()
    {
        if (IsBusy || _currentPath is null || _pages is null || _scannedPages.Length == 0) return;
        IsBusy = true;
        var ocr = new OcrService();
        try
        {
            Status = "Downloading OCR language data (free, one-time ~4 MB)…";
            await ocr.EnsureLanguageAsync();

            var targets = _scannedPages;
            var renderer = _pages;
            var overrides = await Task.Run(() =>
            {
                var dict = new Dictionary<int, IReadOnlyList<Line>>();
                foreach (var page in targets)
                {
                    Dispatcher.UIThread.Post(() => Status = $"OCR page {page} of {targets[^1]}…");
                    var png = renderer.RenderPngBytes(page, 300);
                    var lines = ocr.OcrPngLines(png, renderer.PageHeightPt(page), 300);
                    if (lines.Count > 0) dict[page] = lines;
                }
                return dict;
            });

            if (overrides.Count == 0)
            {
                Status = "OCR found no text on the scanned pages.";
                return;
            }

            var path = _currentPath;
            var spine = await Task.Run(() =>
            {
                using var book = PdfBook.Open(path);
                return ReadingSpine.Build(book, overrides);
            });

            var keep = CurrentIndex;
            _pipeline.SetSpine(spine);
            _spine = spine;
            Sentences.Clear();
            foreach (var s in spine.Sentences) Sentences.Add(new SentenceVm(s));
            TotalSentences = spine.Sentences.Count;
            _scannedPages = spine.ScannedPages.Count == 0
                ? Array.Empty<int>()
                : spine.ScannedPages.OrderBy(x => x).ToArray();
            OnPropertyChanged(nameof(HasScannedPages));
            if (keep >= 0 && keep < TotalSentences)
            {
                SelectedIndex = keep;
                SetSelectedHighlight(keep);
                if (UsePageView) RefreshPageHighlight(keep, forceScroll: true);
            }
            Status = $"OCR recovered {overrides.Count} page(s); now {TotalSentences} sentences.";
        }
        catch (Exception ex)
        {
            Status = "OCR failed: " + (ex is DllNotFoundException
                ? "tesseract native library not available on this OS."
                : ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public string PositionLabel => TotalSentences == 0
        ? ""
        : $"sentence {Math.Max(CurrentIndex + 1, 1)} / {TotalSentences}";

    partial void OnSpeedChanged(double value)
    {
        _pipeline.Speed = (float)value;
        _settings.Speed = value;
        SettingsStore.Save(_settings);
    }

    partial void OnCurrentIndexChanged(int value) => OnPropertyChanged(nameof(PositionLabel));

    // Boarding-pass discipline: every status change flashes the accent and holds it
    // ~4 s (the _statusCooldown tick settles it back to quiet chalk).
    partial void OnStatusChanged(string value)
    {
        StatusHot = true;
        _statusCooldown?.Stop();
        _statusCooldown?.Start();
    }

    /// <summary>Whole-book scrubber: jump to a sentence. Audible seek while playing/paused; position-only when stopped (no voice needed for a silent jump).</summary>
    [RelayCommand]
    private void Seek(int? index)
    {
        if (index is not int target || TotalSentences == 0) return;
        target = Math.Clamp(target, 0, TotalSentences - 1);
        if (_pipeline.State == SpeechState.Stopped)
        {
            _pipeline.PositionOnly(target);
            SetCurrent(target);
            ScrollToSentence?.Invoke(target);
            if (UsePageView) RefreshPageHighlight(target, forceScroll: true);
        }
        else
        {
            _pipeline.Seek(target);
        }
        RefreshTransport();
        SavePositionNow();
    }

    /// <summary>Scrubber thumb grabbed: freeze live position sync until it releases.</summary>
    public void BeginScrub() => _scrubbing = true;

    /// <summary>Scrubber thumb released: commit the dragged position.</summary>
    public void EndScrub()
    {
        _scrubbing = false;
        if (TotalSentences > 0) Seek(Scrub);
    }

    /// <summary>Opens from the recents menu.</summary>
    [RelayCommand]
    public async Task OpenRecentAsync(LibraryEntry? entry)
    {
        if (entry is null) return;
        if (!File.Exists(entry.Path))
        {
            Status = $"File is gone: {entry.Title}";
            return;
        }
        await OpenPdfAsync(entry.Path);
    }

    /// <summary>Loads a PDF off the UI thread and rebuilds the reader list.</summary>
    public async Task OpenPdfAsync(string path)
    {
        if (IsBusy) return;
        SavePositionNow(); // flush previous book position
        IsBusy = true;
        Status = "Parsing document…";
        try
        {
            var (spine, title) = await Task.Run(() =>
            {
                using var book = PdfBook.Open(path);
                var built = ReadingSpine.Build(book);
                return (built, book.Title ?? System.IO.Path.GetFileName(path));
            });

            _pipeline.SetSpine(spine);
            _spine = spine;
            _currentPath = path;
            var pages = await Task.Run(() => new PageRenderer(File.ReadAllBytes(path)));
            Interlocked.Increment(ref _openEpoch);
            _pages?.Dispose();
            _pages = pages;

            Pages.Clear();
            _highlightedPage = null;
            _shownPage = 0;
            for (int p = 1; p <= pages.PageCount; p++)
                Pages.Add(new PageVm(p, pages.PageWidthPt(p) * PxPerPt, pages.PageHeightPt(p) * PxPerPt));

            Sentences.Clear();
            foreach (var s in spine.Sentences) Sentences.Add(new SentenceVm(s));
            TotalSentences = spine.Sentences.Count;
            BookTitle = title;

            var entry = _library.Touch(path, title);
            _saveCounter = 0;

            var resume = entry.LastSentence >= 0 && entry.LastSentence < TotalSentences
                ? entry.LastSentence
                : 0;
            CurrentIndex = -1;
            SelectedIndex = resume;
            _pipeline.PositionOnly(resume);
            if (resume > 0) SetSelectedHighlight(resume);
            if (UsePageView && spine.Sentences.Count > 0)
            {
                int resumePage = spine.Sentences[resume].PageNumber;
                RequestPageRender(resumePage);
                RequestPageRender(resumePage + 1);
                RequestPageRender(resumePage - 1);
                RefreshPageHighlight(resume, forceScroll: true);
            }

            _scannedPages = spine.ScannedPages.Count == 0
                ? Array.Empty<int>()
                : spine.ScannedPages.OrderBy(x => x).ToArray();
            OnPropertyChanged(nameof(HasScannedPages));
            var notes = _scannedPages.Length;
            var sb = $"{TotalSentences} sentences";
            if (resume > 0) sb += $" · resumed at sentence {resume + 1}";
            if (notes > 0) sb += $" · {notes} scanned page(s) without text";
            Status = sb + ". Press play to listen.";
            OnPropertyChanged(nameof(Recents));
        }
        catch (LeafVoxException ex)
        {
            Status = ex.Message;
        }
        catch (Exception ex)
        {
            Status = "Could not open file: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TogglePlayAsync()
    {
        switch (_pipeline.State)
        {
            case SpeechState.Playing:
                _pipeline.Pause();
                break;
            case SpeechState.Paused:
                _pipeline.Resume();
                break;
            default:
                if (!await EnsureVoiceAsync()) return;
                _pipeline.Play(CurrentIndex < 0 ? SelectedIndex : CurrentIndex);
                break;
        }
        RefreshTransport();
    }

    private bool CanReadFromSelected() => SelectedIndex >= 0 && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanReadFromSelected))]
    private async Task ReadFromSelectedAsync()
    {
        if (SelectedIndex < 0) return;
        if (!await EnsureVoiceAsync()) return;
        _pipeline.Play(SelectedIndex);
        RefreshTransport();
    }

    private bool CanReadThisLine() => TotalSentences > 0 && !IsBusy;

    /// <summary>Speak exactly one line (the current one, or the selected one) and stop.</summary>
    [RelayCommand(CanExecute = nameof(CanReadThisLine))]
    private async Task ReadThisLineAsync()
    {
        var target = CurrentIndex >= 0 ? CurrentIndex : SelectedIndex;
        if (target < 0 || target >= TotalSentences) return;
        if (!await EnsureVoiceAsync()) return;
        _pipeline.PlayOnce(target);
        RefreshTransport();
    }

    [RelayCommand]
    private void Next() => _pipeline.Next();

    [RelayCommand]
    private void Previous() => _pipeline.Previous();

    [RelayCommand]
    private void Stop()
    {
        _pipeline.Stop();
        SavePositionNow();
        RefreshTransport();
    }

    [RelayCommand]
    private void SetSleep(string? minutesText)
    {
        if (_sleepTimer is not null)
        {
            _sleepTimer.Stop();
            _sleepTimer = null;
            SleepLabel = "Sleep";
            Status = "Sleep timer off.";
            return;
        }
        if (!int.TryParse(minutesText, out var minutes) || minutes <= 0) return;
        _sleepTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(minutes) };
        _sleepTimer.Tick += (_, _) =>
        {
            _sleepTimer?.Stop();
            _sleepTimer = null;
            SleepLabel = "Sleep";
            Stop();
            Status = "Sleep timer: stopped playback. Good night.";
        };
        _sleepTimer.Start();
        SleepLabel = $"Sleep {minutes}m";
        Status = $"Will stop in {minutes} minutes.";
    }

    private void RefreshTransport()
    {
        var playing = _pipeline.State == SpeechState.Playing;
        IsPlaying = playing;
        ReadFromSelectedCommand.NotifyCanExecuteChanged();
        ReadThisLineCommand.NotifyCanExecuteChanged();
    }

    private void SetCurrent(int index)
    {
        if (CurrentIndex >= 0 && CurrentIndex < Sentences.Count) Sentences[CurrentIndex].IsCurrent = false;
        CurrentIndex = index;
        if (!_scrubbing) Scrub = Math.Clamp(index, 0, ScrubMax);
        if (index >= 0 && index < Sentences.Count)
        {
            Sentences[index].IsCurrent = true;
            SelectedIndex = index;
            ScrollToSentence?.Invoke(index);
            if (++_saveCounter % 10 == 0) SavePositionNow();
            if (UsePageView) RefreshPageHighlight(index, forceScroll: false);
        }
    }

    private void SetSelectedHighlight(int index)
    {
        foreach (var s in Sentences) s.IsCurrent = false;
        if (index >= 0 && index < Sentences.Count) Sentences[index].IsCurrent = true;
    }

    private void SavePositionNow()
    {
        if (_currentPath is null) return;
        var pos = CurrentIndex >= 0 ? CurrentIndex : SelectedIndex;
        if (pos >= 0) _library.SavePosition(_currentPath, pos, Speed);
        OnPropertyChanged(nameof(Recents));
    }

    /// <summary>Lazily renders a page bitmap on a worker thread; belongs to no UI element until done.</summary>
    public void RequestPageRender(int page1Based)
    {
        var pages = _pages;
        if (pages is null || page1Based < 1 || page1Based > Math.Min(Pages.Count, pages.PageCount)) return;
        var vm = Pages[page1Based - 1];
        if (vm.Loaded) return;
        lock (_renderGate)
        {
            if (!_renderPending.Add(page1Based)) return;
        }
        _ = RenderPageAsync(page1Based, pages, vm);
    }

    private async Task RenderPageAsync(int page1Based, PageRenderer renderer, PageVm page)
    {
        int epoch = Volatile.Read(ref _openEpoch);
        try
        {
            var bmp = await Task.Run(() => renderer.Render(page1Based, RenderDpi)).ConfigureAwait(true);
            // Document switched while rendering → never let a stale bitmap reach the UI.
            if (!ReferenceEquals(_pages, renderer) || epoch != Volatile.Read(ref _openEpoch)) { bmp.Dispose(); return; }
            if (page.Loaded) return; // a newer render already won
            page.Image = bmp;
            page.Loaded = true;
            _renderedOrder.Enqueue(page1Based);
            TrimRenderedBitmaps();
        }
        catch (Exception ex) { Status = $"Page {page1Based} render failed: {ex.Message}"; }
        finally
        {
            lock (_renderGate) { _renderPending.Remove(page1Based); }
        }
    }

    /// <summary>
    /// Keeps at most <see cref="MaxRenderedPages"/> page bitmaps alive, never dropping the
    /// pages immediately visible while playback runs; dropped pages re-render when re-visited.
    /// </summary>
    private void TrimRenderedBitmaps()
    {
        if (_renderedOrder.Count <= MaxRenderedPages) return;
        var keep = new List<int>();
        while (_renderedOrder.Count > 0)
        {
            int page = _renderedOrder.Dequeue();
            bool nearReadingPage = Math.Abs(page - _shownPage) <= 2;
            if (nearReadingPage || keep.Count >= MaxRenderedPages) { keep.Add(page); continue; }
            var vm = page >= 1 && page <= Pages.Count ? Pages[page - 1] : null;
            if (vm is null || !vm.Loaded) continue;
            var img = vm.Image;
            vm.Image = null;
            img?.Dispose();
            vm.Loaded = false;
        }
        // Requeue appended survivors; a compact book (< MaxRenderedPages) keeps its order.
        foreach (var p in keep) _renderedOrder.Enqueue(p);
    }

    /// <summary>
    /// Moves the karaoke overlay to the sentence at <paramref name="index"/> and scrolls the
    /// page viewer when the reading position crosses to a new page (scroll only on page change
    /// so in-page stepping doesn't yank the view).
    /// </summary>
    private void RefreshPageHighlight(int index, bool forceScroll)
    {
        var spine = _spine;
        var pages = _pages;
        if (spine is null || pages is null || index < 0 || index >= spine.Sentences.Count
            || index >= Pages.Count) return;

        var s = spine.Sentences[index];
        var targetPage = Pages[s.PageNumber - 1];
        if (_highlightedPage is { } old && !ReferenceEquals(old, targetPage)) old.HighlightBoxes.Clear();
        targetPage.HighlightBoxes.Clear();
        foreach (var b in s.Boxes)
        {
            targetPage.HighlightBoxes.Add(new HighlightBoxVm(
                b.Left * PxPerPt,
                (pages.PageHeightPt(s.PageNumber) - b.Top) * PxPerPt,
                b.Width * PxPerPt,
                b.Height * PxPerPt));
        }
        _highlightedPage = targetPage;

        if (s.PageNumber != _shownPage)
        {
            _shownPage = s.PageNumber;
            RequestPageRender(s.PageNumber + 1); // pre-render the next page ahead of playback
        }
        double topFraction = s.Boxes.Count > 0
            ? Math.Clamp(s.Boxes[0].Top / pages.PageHeightPt(s.PageNumber), 0, 1)
            : 0;
        ScrollToPage?.Invoke(s.PageNumber, topFraction, forceScroll);
    }

    partial void OnUsePageViewChanged(bool value)
    {
        OnPropertyChanged(nameof(ViewLabel));
        if (!value || _pages is null) return;
        var spine = _spine;
        if (spine is null || spine.Sentences.Count == 0) return;
        int idx = CurrentIndex >= 0 && CurrentIndex < spine.Sentences.Count
            ? CurrentIndex
            : Math.Min(Math.Max(SelectedIndex, 0), spine.Sentences.Count - 1);
        if (idx >= 0) RefreshPageHighlight(idx, forceScroll: true);
        else
        {
            _shownPage = 1;
            RequestPageRender(1);
            ScrollToPage?.Invoke(1, 0, true);
        }
    }

    [RelayCommand]
    private void ToggleView() => UsePageView = !UsePageView;

    [RelayCommand]
    private void ToggleBookmark()
    {
        if (_currentPath is null || CurrentIndex < 0) return;
        _library.ToggleBookmark(_currentPath, CurrentIndex);
        var mark = _library.Entries.FirstOrDefault(e => e.Path == _currentPath);
        Status = mark is not null && mark.Bookmarks.Contains(CurrentIndex)
            ? $"Bookmark added at sentence {CurrentIndex + 1}."
            : "Bookmark removed.";
        OnPropertyChanged(nameof(Bookmarks));
    }

    public IReadOnlyList<int> Bookmarks =>
        _currentPath is null
            ? Array.Empty<int>()
            : (IReadOnlyList<int>?)(_library.Entries.FirstOrDefault(e => e.Path == _currentPath)?.Bookmarks) ?? Array.Empty<int>();

    [RelayCommand]
    public async Task GoToBookmarkAsync(int index)
    {
        if (_spine is null || index < 0 || index >= _spine.Sentences.Count) return;
        SelectedIndex = index;
        await ReadFromSelectedAsync();
    }

    /// <summary>Double-click on a page: start reading from the nearest sentence on that spot.</summary>
    public void PagePointDoubleTapped(int page1Based, double xPx, double yPx)
    {
        var spine = _spine;
        var pages = _pages;
        if (spine is null || pages is null || page1Based < 1 || page1Based > pages.PageCount || PxPerPt <= 0) return;
        double yPdf = pages.PageHeightPt(page1Based) - yPx / PxPerPt;
        int index = spine.IndexNearest(page1Based, yPdf);
        SelectedIndex = index;
        if (_engine.IsReady) _pipeline.Play(index);
        else _ = ReadFromSelectedAsync();
        RefreshTransport();
    }

    private async Task<bool> EnsureVoiceAsync()
    {
        if (_engine.IsReady) return true;
        var voice = ResolveVoice(out var dir);
        if (voice is null || dir is null)
        {
            var pref = Voices.PreferredVoice();
            Status = $"No voice installed yet — download '{pref.DisplayName}' (free) from the Voices panel.";
            return false;
        }
        try
        {
            Status = "Loading voice…";
            await _engine.LoadAsync(voice, dir);
            _pipeline.SpeakerId = _settings.SpeakerId;
            VoiceLabel = $"voice: {voice.DisplayName}";
            Status = "Voice ready.";
            return true;
        }
        catch (LeafVoxTtsException ex)
        {
            Status = "Voice load failed: " + ex.Message;
            return false;
        }
    }

    private VoiceInfo? ResolveVoice(out string? dir)
    {
        var wanted = _settings.VoiceId;
        if (wanted is not null)
        {
            var info = VoiceCatalog.Find(wanted);
            var d = Voices.ModelDir(wanted);
            if (info is not null && d is not null)
            {
                dir = d;
                return info;
            }
        }
        var preferred = Voices.PreferredVoice();
        dir = Voices.ModelDir(preferred.Id);
        return dir is null ? null : preferred;
    }

    public void SetActiveVoice(string voiceId)
    {
        _settings.VoiceId = voiceId;
        SettingsStore.Save(_settings);
        _engine.Unload();
        VoiceLabel = $"voice: {VoiceCatalog.Find(voiceId)?.DisplayName ?? voiceId}";
    }
}
