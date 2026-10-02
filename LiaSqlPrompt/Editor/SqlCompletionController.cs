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

    public bool HandleTypeChar()
    {
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
      if (!_completionPresenter.IsVisible)
        return false;

      _completionPresenter.Hide();
      _completionSpan = null;

      // Let SSMS insert the space.
      return false;
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