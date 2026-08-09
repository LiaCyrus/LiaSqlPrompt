using LiaSqlPrompt.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LiaSqlPrompt.Completion
{
    public sealed class CompletionPresenter
    {
        private readonly IWpfTextView _view;
        private readonly List<CompletionItem> _items = new();
        private readonly Popup _popup;
        private readonly ListBox _listBox;

        private bool _isVisible;

        public bool IsVisible => _isVisible;

        public CompletionPresenter(IWpfTextView view)
        {
            _view = view;

            _popup = new Popup
            {
                Placement = PlacementMode.Relative,
                PlacementTarget = view.VisualElement,
                StaysOpen = true,
                AllowsTransparency = true
            };

            _listBox = new ListBox
            {
                Width = 300,
                MaxHeight = 200,
                DisplayMemberPath = nameof(CompletionItem.Label)
            };

            _popup.Child = _listBox;

            _view.LayoutChanged += OnLayoutChanged;
            _view.LostAggregateFocus += OnLostAggregateFocus;
            _view.Closed += OnViewClosed;
        }

        public void Show(IReadOnlyList<CompletionItem> items)
        {
            Hide();

            _items.AddRange(items);

            if (_items.Count == 0)
                return;

            _listBox.ItemsSource = _items;
            _listBox.SelectedIndex = 0;

            UpdatePosition();

            _popup.IsOpen = true;
            _isVisible = true;
        }

        public void Hide()
        {
            _popup.IsOpen = false;

            _listBox.ItemsSource = null;
            _listBox.SelectedIndex = -1;

            _items.Clear();

            _isVisible = false;
        }

        public void MoveNext()
        {
            if (!_isVisible)
                return;

            if (_listBox.SelectedIndex < _listBox.Items.Count - 1)
                _listBox.SelectedIndex++;
        }

        public void MovePrevious()
        {
            if (!_isVisible)
                return;

            if (_listBox.SelectedIndex > 0)
                _listBox.SelectedIndex--;
        }

        public CompletionItem? GetSelectedItem()
        {
            if (!_isVisible)
                return null;

            return _listBox.SelectedItem as CompletionItem;
        }

        public void Accept(
            IWpfTextView view,
            ITrackingSpan completionSpan)
        {
            CompletionItem? item = GetSelectedItem();

            if (item == null)
                return;

            ITextSnapshot snapshot = view.TextSnapshot;
            SnapshotSpan span = completionSpan.GetSpan(snapshot);

            using (ITextEdit edit = view.TextBuffer.CreateEdit())
            {
                edit.Replace(span, item.InsertText);
                edit.Apply();
            }

            Hide();
        }

        private void UpdatePosition()
        {
            if (!_isVisible)
                return;

            ITextViewLine line = _view.Caret.ContainingTextViewLine;

            if (!line.IsValid)
            {
                Hide();
                return;
            }

            _popup.HorizontalOffset = line.Right;
            _popup.VerticalOffset = line.Bottom;
        }

        private void OnLayoutChanged(
            object sender,
            TextViewLayoutChangedEventArgs e)
        {
            if (!_isVisible)
                return;

            UpdatePosition();
        }

        private void OnLostAggregateFocus(
            object sender,
            EventArgs e)
        {
            Hide();
        }

        private void OnViewClosed(
            object sender,
            EventArgs e)
        {
            _view.LayoutChanged -= OnLayoutChanged;
            _view.LostAggregateFocus -= OnLostAggregateFocus;
            _view.Closed -= OnViewClosed;

            Hide();
        }
    }
}