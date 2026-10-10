using LiaSqlPrompt.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;

namespace LiaSqlPrompt.Editor
{
  public sealed class SqlCompletionController
  {
    private readonly IWpfTextView _view;
    private readonly Microsoft.VisualStudio.Language.Intellisense.ICompletionBroker _completionBroker;
    private readonly CompletionService _completionService;
    private readonly CompletionPresenter _completionPresenter;

    private ITrackingSpan? _completionSpan;

    public bool IsCompletionActive => _completionPresenter.IsVisible;

    public SqlCompletionController(IWpfTextView view, Microsoft.VisualStudio.Language.Intellisense.ICompletionBroker completionBroker)
    {
      _view = view;
      _completionBroker = completionBroker;
      _completionService = new CompletionService();
      _completionPresenter = new CompletionPresenter(view);

      _view.TextBuffer.Changed += OnTextBufferChanged;
      _view.Closed += OnViewClosed;
    }

    public void CheckAndYieldIfNativeActive()
    {
      // Do not prematurely kill our active completion popup when SSMS native IntelliSense initializes in the background.
      // Native IntelliSense will yield or LiaSqlPrompt handles navigation and selection.
    }

    public bool HandleTypeChar(char ch)
    {
      if (ch == '(' || ch == ')' || ch == ';' || ch == ',')
      {
        if (_completionPresenter.IsVisible)
        {
          _completionPresenter.Hide();
          _completionSpan = null;
        }

        FormatKeywordBeforeCaret();
      }

      return false;
    }

    public bool HandleBackspace()
    {
      return false;
    }

    public bool HandleTab()
    {
      return CommitSelection();
    }

    public bool HandleReturn()
    {
      return CommitSelection();
    }

    public bool HandleEscape()
    {
      if (!_completionPresenter.IsVisible)
        return false;

      _completionPresenter.Hide();
      _completionSpan = null;

      return false;
    }

    public bool HandleCompleteWord()
    {
      UpdateCompletion(_view.TextSnapshot, _view.Caret.Position.BufferPosition.Position, forceShow: true);
      return _completionPresenter.IsVisible;
    }

    public bool HandleUp()
    {
      if (!_completionPresenter.IsVisible)
        return false;

      _completionPresenter.MovePrevious();

      return true;
    }

    public bool HandleDown()
    {
      if (!_completionPresenter.IsVisible)
        return false;

      _completionPresenter.MoveNext();

      return true;
    }

    public bool HandleSpace()
    {
      if (_completionPresenter.IsVisible)
      {
        _completionPresenter.Hide();
        _completionSpan = null;
      }

      FormatKeywordBeforeCaret();

      // Return false so SSMS / VS editor inserts the space character naturally.
      return false;
    }

    private void FormatKeywordBeforeCaret()
    {
      try
      {
        var casing = Option.OptionService.General?.KeywordCasingOnSpace ?? SqlEngine.KeywordCasing.Uppercase;
        if (casing == SqlEngine.KeywordCasing.Disabled)
          return;

        var caret = _view.Caret.Position.BufferPosition;
        int caretPos = caret.Position;
        if (caretPos <= 0)
          return;

        var snapshot = _view.TextSnapshot;
        var line = snapshot.GetLineFromPosition(caretPos);
        string lineText = line.GetText();
        int offsetInLine = caretPos - line.Start.Position;

        if (SqlEngine.SqlKeywordCaser.TryGetKeywordFormatReplacement(
          lineText,
          offsetInLine,
          casing,
          out var targetSpan,
          out string formattedWord))
        {
          int replaceStart = line.Start.Position + targetSpan.Start;
          var spanToReplace = new SnapshotSpan(snapshot, replaceStart, targetSpan.Length);

          using (ITextEdit edit = _view.TextBuffer.CreateEdit())
          {
            edit.Replace(spanToReplace, formattedWord);
            edit.Apply();
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Log($"Error formatting keyword on space: {ex.Message}");
      }
    }

    public void FormatAllKeywordsToUppercase()
    {
      try
      {
        var selection = _view.Selection;
        if (selection.IsEmpty)
          return;

        var snapshot = _view.TextSnapshot;
        string fullSql = snapshot.GetText();
        if (string.IsNullOrEmpty(fullSql))
          return;

        // Get the highlighted selection range
        var selectedSpan = selection.SelectedSpans[0];
        var range = (selectedSpan.Start.Position, selectedSpan.Length);

        var replacements = SqlEngine.SqlKeywordCaser.FormatAllKeywords(fullSql, SqlEngine.KeywordCasing.Uppercase, range);
        if (replacements.Count == 0)
          return;

        using (ITextEdit edit = _view.TextBuffer.CreateEdit())
        {
          foreach (var r in replacements)
          {
            var span = new SnapshotSpan(snapshot, r.Span.Start, r.Span.Length);
            edit.Replace(span, r.FormattedText);
          }
          edit.Apply();
        }
      }
      catch (Exception ex)
      {
        Logger.Log($"Error formatting selected keywords to uppercase: {ex.Message}");
      }
    }

    public void FormatAllKeywordsToLowercase()
    {
      try
      {
        var selection = _view.Selection;
        if (selection.IsEmpty)
          return;

        var snapshot = _view.TextSnapshot;
        string fullSql = snapshot.GetText();
        if (string.IsNullOrEmpty(fullSql))
          return;

        // Get the highlighted selection range
        var selectedSpan = selection.SelectedSpans[0];
        var range = (selectedSpan.Start.Position, selectedSpan.Length);

        var replacements = SqlEngine.SqlKeywordCaser.FormatAllKeywords(fullSql, SqlEngine.KeywordCasing.Lowercase, range);
        if (replacements.Count == 0)
          return;

        using (ITextEdit edit = _view.TextBuffer.CreateEdit())
        {
          foreach (var r in replacements)
          {
            var span = new SnapshotSpan(snapshot, r.Span.Start, r.Span.Length);
            edit.Replace(span, r.FormattedText);
          }
          edit.Apply();
        }
      }
      catch (Exception ex)
      {
        Logger.Log($"Error formatting selected keywords to lowercase: {ex.Message}");
      }
    }

    private bool CommitSelection()
    {
      if (!_completionPresenter.IsVisible)
        return false;

      if (_completionSpan == null)
        return false;

      _completionPresenter.Accept(_view, _completionSpan);

      _completionSpan = null;

      return true;
    }

    private void OnViewClosed(object? sender, EventArgs e)
    {
      _view.TextBuffer.Changed -= OnTextBufferChanged;
      _view.Closed -= OnViewClosed;

      _completionSpan = null;
    }

    private void OnTextBufferChanged(object? sender, TextContentChangedEventArgs e)
    {
      ITextSnapshot snapshot = e.After;

      if (e.Changes.Count == 0)
        return;

      ITextChange change = e.Changes[e.Changes.Count - 1];

      int caretPosition = change.NewPosition + change.NewLength;

      UpdateCompletion(snapshot, caretPosition);
    }

    private void UpdateCompletion(ITextSnapshot snapshot, int caretPosition, bool forceShow = false)
    {
      // If native SQL Server IntelliSense is already active before we started, yield only if our popup is not active
      try
      {
        if (!_completionPresenter.IsVisible && _completionBroker.IsCompletionActive(_view))
        {
          return;
        }
      }
      catch
      {
      }

      string fullSql = snapshot.GetText();
      var context = SqlEngine.Completion.SqlCompletionContextAnalyzer.Analyze(fullSql, caretPosition);

      // In column reference or table source contexts, suppress keyword completion to allow native SSMS IntelliSense
      if (context.Kind == SqlEngine.Completion.SqlCompletionContextKind.ColumnReference ||
          context.Kind == SqlEngine.Completion.SqlCompletionContextKind.TableSource)
      {
        _completionPresenter.Hide();
        _completionSpan = null;
        return;
      }

      var items = _completionService.Search(context);

      // Do not auto-popup when prefix is empty unless explicitly invoked (e.g. Ctrl+Space / forceShow)
      if (!forceShow && (string.IsNullOrEmpty(context.Prefix) || items.Count == 0))
      {
        _completionPresenter.Hide();
        _completionSpan = null;
        return;
      }

      string prefix = context.Prefix ?? string.Empty;
      int prefixStart = Math.Max(0, caretPosition - prefix.Length);

      _completionSpan = snapshot.CreateTrackingSpan(prefixStart, prefix.Length, SpanTrackingMode.EdgeInclusive);
      _completionPresenter.Show(items);

      // Dismiss native SSMS completion so it does not steal focus, conflict, or trigger auto-dismissal
      try
      {
        _completionBroker.DismissAllSessions(_view);
      }
      catch
      {
      }
    }

    private (string Word, int Start) GetCurrentWord(ITextSnapshot snapshot, int caretPosition)
    {
      int position = caretPosition - 1;

      if (position < 0)
        return (string.Empty, 0);

      int start = position;

      while (start >= 0)
      {
        if (!char.IsLetterOrDigit(snapshot[start]))
          break;

        start--;
      }

      start++;

      string word = snapshot.GetText(start, position - start + 1);

      return (word, start);
    }
  }
}