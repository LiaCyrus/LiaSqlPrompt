using LiaSqlPrompt.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LiaSqlPrompt.Completion
{
    public sealed class CompletionPresenter
    {
        private readonly List<CompletionItem> _items = new();

        private readonly Popup _popup;
        private readonly ListBox _listBox;

        private bool _isVisible;

        public bool IsVisible => _isVisible;

        public CompletionPresenter(IWpfTextView view)
        {
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
        }

        public void Show(IReadOnlyList<CompletionItem> items, double x, double y)
        {
            _items.Clear();
            _items.AddRange(items);

            _isVisible = _items.Count > 0;

            _listBox.ItemsSource = null;
            _listBox.ItemsSource = _items;

            if (_items.Count > 0)
                _listBox.SelectedIndex = 0;

            _popup.HorizontalOffset = x;
            _popup.VerticalOffset = y;
            _popup.IsOpen = _isVisible;
        }

        public void Hide()
        {
            _items.Clear();

            _listBox.ItemsSource = null;
            _listBox.SelectedIndex = -1;

            _isVisible = false;
            _popup.IsOpen = false;
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

        public void Accept(IWpfTextView view, CompletionSession? session)
        {
            if (session == null) return;

            CompletionItem? item = GetSelectedItem();

            if (item == null) return;

            ITextSnapshot snapshot = view.TextSnapshot;

            if (session.Start < 0 || session.Start >= snapshot.Length) return;

            int length = Math.Min(session.Length, snapshot.Length - session.Start);

            using (ITextEdit edit = view.TextBuffer.CreateEdit())
            {
                if (length > 0)
                    edit.Delete(session.Start, length);

                edit.Insert(session.Start, item.InsertText);

                edit.Apply();
            }

            Hide();
        }
    }
}