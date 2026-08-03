using LiaSqlPrompt.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;

namespace LiaSqlPrompt.Editor
{
    public sealed class SqlCompletionController
    {
        private readonly IWpfTextView _view;
        private readonly CompletionService _completionService;
        private readonly CompletionPresenter _completionPresenter;

        private CompletionSession? _session;

        public SqlCompletionController(IWpfTextView view)
        {
            _view = view;
            _completionService = new CompletionService();
            _completionPresenter = new CompletionPresenter(view);

            _view.TextBuffer.Changed += OnTextBufferChanged;
            _view.Closed += OnViewClosed;
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
            _session = null;

            return false;
        }

        public bool HandleCompleteWord()
        {
            UpdateCompletion(true);

            return true;
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
            _session = null;

            // Let SSMS insert the space.
            return false;
        }

        private bool CommitSelection()
        {
            if (!_completionPresenter.IsVisible)
                return false;

            _completionPresenter.Accept(_view, _session);

            _session = null;

            return true;
        }

        private void OnTextBufferChanged(object? sender, TextContentChangedEventArgs e)
        {
            UpdateCompletion();
        }

        private void OnViewClosed(object? sender, EventArgs e)
        {
            _view.TextBuffer.Changed -= OnTextBufferChanged;
            _view.Closed -= OnViewClosed;
        }

        private void UpdateCompletion(bool forceShow = false)
        {
            var current = GetCurrentWord();

            string word = current.Word;

            if (string.IsNullOrWhiteSpace(word))
            {
                _completionPresenter.Hide();
                _session = null;
                return;
            }

            var items = _completionService.Search(word);

            if (!forceShow && items.Count == 0)
            {
                _completionPresenter.Hide();
                _session = null;
                return;
            }

            _session = new CompletionSession(
                current.Start,
                word.Length);

            var caret = _view.Caret.ContainingTextViewLine;

            double x = caret.Right;
            double y = caret.Bottom;

            _completionPresenter.Show(items, x, y);
        }

        private (string Word, int Start) GetCurrentWord()
        {
            ITextSnapshot snapshot = _view.TextSnapshot;

            int position = _view.Caret.Position.BufferPosition.Position - 1;

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