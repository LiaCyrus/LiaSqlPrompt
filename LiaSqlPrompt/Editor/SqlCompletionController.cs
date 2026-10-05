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
      try
      {
        if (_completionPresenter.IsVisible && _completionBroker.IsCompletionActive(_view))
        {
          _completionPresenter.Hide();
          _completionSpan = null;
        }
      }
      catch
      {
      }
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
      return false;
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
      var current = GetCurrentWord(snapshot, caretPosition);

      string word = current.Word;

      if (string.IsNullOrWhiteSpace(word))
      {
        _completionPresenter.Hide();
        _completionSpan = null;

        return;
      }

      // If native SQL Server IntelliSense is already active, yield to it and turn off our completion.
      try
      {
        if (_completionBroker.IsCompletionActive(_view))
        {
          _completionPresenter.Hide();
          _completionSpan = null;
          return;
        }
      }
      catch
      {
      }

      // Check if current word is preceded by a dot '.', indicating a column/member access (e.g. x.i or a.id)
      // In that case, do not show keyword completions (let SSMS column IntelliSense take over).
      var line = snapshot.GetLineFromPosition(caretPosition);
      string lineText = line.GetText();
      int offsetInLine = caretPosition - line.Start.Position;

      if (SqlEngine.SqlKeywordCaser.IsPrecededByDot(lineText, offsetInLine))
      {
        _completionPresenter.Hide();
        _completionSpan = null;
        return;
      }

      // Check if current word is immediately after FROM, JOIN, INTO, UPDATE, TABLE
      // where a table/object name or alias is expected rather than a keyword recommendation (e.g. select * from select).
      if (SqlEngine.SqlKeywordCaser.IsInTableSourceContext(lineText, offsetInLine))
      {
        _completionPresenter.Hide();
        _completionSpan = null;
        return;
      }

      var items = _completionService.Search(word);

      if (!forceShow && items.Count == 0)
      {
        _completionPresenter.Hide();
        _completionSpan = null;

        return;
      }

      _completionSpan = snapshot.CreateTrackingSpan(current.Start, word.Length, SpanTrackingMode.EdgeInclusive);

      _completionPresenter.Show(items);
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