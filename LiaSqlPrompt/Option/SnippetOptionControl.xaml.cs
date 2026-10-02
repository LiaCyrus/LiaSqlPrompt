using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiaSqlPrompt.Snippet;

namespace LiaSqlPrompt.Option
{
  public partial class SnippetOptionControl : UserControl
  {
    private readonly List<SnippetDefinition> _allSnippets = new List<SnippetDefinition>();
    private SnippetDefinition? _selectedSnippet;

    public SnippetOptionControl()
    {
      InitializeComponent();
      Loaded += SnippetOptionControl_Loaded;
    }

    private void SnippetOptionControl_Loaded(object sender, RoutedEventArgs e)
    {
      LoadFolderSetting();
      RefreshList();
    }

    private void LoadFolderSetting()
    {
      string currentFolder = OptionService.Snippet?.SnippetFolder ?? @"C:\LiaSqlPrompt\Snippet";
      SnippetFolderTextBox.Text = currentFolder;
    }

    private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
      using var dialog = new System.Windows.Forms.FolderBrowserDialog
      {
        Description = "Select Snippet Folder",
        SelectedPath = SnippetFolderTextBox.Text
      };

      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        SnippetFolderTextBox.Text = dialog.SelectedPath;
        SaveFolderSetting(dialog.SelectedPath);
      }
    }

    private void SaveFolderButton_Click(object sender, RoutedEventArgs e)
    {
      string folder = SnippetFolderTextBox.Text?.Trim() ?? string.Empty;

      if (string.IsNullOrWhiteSpace(folder))
      {
        ShowStatus("Snippet folder path cannot be empty.", isError: true);
        return;
      }

      SaveFolderSetting(folder);
    }

    private void SaveFolderSetting(string folder)
    {
      try
      {
        if (!Directory.Exists(folder))
        {
          Directory.CreateDirectory(folder);
        }

        if (OptionService.Snippet != null)
        {
          OptionService.Snippet.SnippetFolder = folder;
          OptionService.Snippet.SaveSettingsToStorage();
        }

        SnippetRepository.Instance.Reload();
        RefreshList();
        ShowStatus($"Snippet folder updated to: {folder}", isError: false);
      }
      catch (Exception ex)
      {
        ShowStatus($"Error updating folder: {ex.Message}", isError: true);
      }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
      SnippetRepository.Instance.Reload();
      RefreshList();
      ShowStatus("Snippet list refreshed.", isError: false);
    }

    public void RefreshList()
    {
      _allSnippets.Clear();
      _allSnippets.AddRange(SnippetRepository.Instance.GetAll().OrderBy(s => s.Shortcut, StringComparer.OrdinalIgnoreCase));
      ApplyFilter();
    }

    private void ApplyFilter()
    {
      string filter = SearchBox.Text?.Trim() ?? string.Empty;
      var filtered = string.IsNullOrEmpty(filter)
          ? _allSnippets
          : _allSnippets.Where(s => s.Shortcut.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                                 || s.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

      SnippetsListBox.ItemsSource = null;
      SnippetsListBox.ItemsSource = filtered;

      if (_selectedSnippet != null)
      {
        var match = filtered.FirstOrDefault(s => string.Equals(s.Shortcut, _selectedSnippet.Shortcut, StringComparison.OrdinalIgnoreCase));
        SnippetsListBox.SelectedItem = match;
      }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      ApplyFilter();
    }

    private void SnippetsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (SnippetsListBox.SelectedItem is SnippetDefinition snippet)
      {
        _selectedSnippet = snippet;
        HeaderTitleTextBlock.Text = $"Edit Snippet ({snippet.Shortcut})";
        ShortcutTextBox.Text = snippet.Shortcut;
        TitleTextBox.Text = snippet.Title;
        DescriptionTextBox.Text = snippet.Description;
        CodeTextBox.Text = snippet.Code;
        StatusTextBlock.Text = string.Empty;
      }
      else if (_selectedSnippet == null)
      {
        ClearEditor();
      }
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
      SnippetsListBox.SelectedItem = null;
      _selectedSnippet = null;
      ClearEditor();
      ShortcutTextBox.Focus();
    }

    private void ClearEditor()
    {
      HeaderTitleTextBlock.Text = "Create Snippet";
      ShortcutTextBox.Clear();
      TitleTextBox.Clear();
      DescriptionTextBox.Clear();
      CodeTextBox.Clear();
      StatusTextBlock.Text = string.Empty;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      string title = TitleTextBox.Text?.Trim() ?? string.Empty;
      string shortcut = ShortcutTextBox.Text?.Trim() ?? string.Empty;
      string description = DescriptionTextBox.Text?.Trim() ?? string.Empty;
      string code = CodeTextBox.Text ?? string.Empty;

      if (string.IsNullOrWhiteSpace(shortcut))
      {
        ShowStatus("Shortcut is required.", isError: true);
        ShortcutTextBox.Focus();
        return;
      }

      if (string.IsNullOrWhiteSpace(code))
      {
        ShowStatus("SQL Code cannot be empty.", isError: true);
        CodeTextBox.Focus();
        return;
      }

      string folder = OptionService.Snippet?.SnippetFolder ?? @"C:\LiaSqlPrompt\Snippet";

      try
      {
        var writer = new SnippetWriter();

        // If editing an existing snippet and shortcut was renamed, remove the old file
        if (_selectedSnippet != null &&
            !string.IsNullOrWhiteSpace(_selectedSnippet.FilePath) &&
            !string.Equals(_selectedSnippet.Shortcut, shortcut, StringComparison.OrdinalIgnoreCase))
        {
          writer.Delete(_selectedSnippet.FilePath!);
        }

        var snippet = new SnippetDefinition(title, shortcut, description, code);
        writer.Save(folder, snippet, overwrite: true);

        SnippetRepository.Instance.Reload();
        _selectedSnippet = snippet;
        RefreshList();

        ShowStatus($"Snippet '{shortcut}' saved successfully!", isError: false);
      }
      catch (Exception ex)
      {
        ShowStatus($"Error saving snippet: {ex.Message}", isError: true);
      }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
      if (SnippetsListBox.SelectedItem is not SnippetDefinition snippet)
      {
        ShowStatus("Select a snippet to delete.", isError: true);
        return;
      }

      var result = MessageBox.Show(
          $"Are you sure you want to delete snippet '{snippet.Shortcut}'?",
          "Confirm Delete",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

      if (result != MessageBoxResult.Yes)
        return;

      try
      {
        if (!string.IsNullOrWhiteSpace(snippet.FilePath) && File.Exists(snippet.FilePath))
        {
          var writer = new SnippetWriter();
          writer.Delete(snippet.FilePath!);
        }
        else
        {
          string folder = OptionService.Snippet?.SnippetFolder ?? @"C:\LiaSqlPrompt\Snippet";
          string fallbackPath = Path.Combine(folder, $"{snippet.Shortcut}.snippet.xml");
          if (File.Exists(fallbackPath))
          {
            File.Delete(fallbackPath);
          }
        }

        SnippetRepository.Instance.Reload();
        _selectedSnippet = null;
        ClearEditor();
        RefreshList();

        ShowStatus($"Snippet '{snippet.Shortcut}' deleted.", isError: false);
      }
      catch (Exception ex)
      {
        ShowStatus($"Error deleting snippet: {ex.Message}", isError: true);
      }
    }

    private void ShowStatus(string message, bool isError)
    {
      StatusTextBlock.Text = message;
      StatusTextBlock.Foreground = isError ? Brushes.IndianRed : Brushes.ForestGreen;
    }
  }
}
