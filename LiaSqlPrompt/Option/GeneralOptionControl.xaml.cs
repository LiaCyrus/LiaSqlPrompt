using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SqlEngine;

namespace LiaSqlPrompt.Option
{
  public partial class GeneralOptionControl : UserControl
  {
    public GeneralOptionControl()
    {
      InitializeComponent();
      Loaded += GeneralOptionControl_Loaded;
    }

    private void GeneralOptionControl_Loaded(object sender, RoutedEventArgs e)
    {
      LoadSettings();
    }

    public void LoadSettings()
    {
      var options = OptionService.General;
      if (options == null)
      {
        return;
      }

      RootFolderTextBox.Text = options.RootFolder ?? @"C:\LiaSqlPrompt";
      KeywordsFilePathTextBox.Text = options.CustomKeywordsFilePath ?? "keywords.json";
      CapitalizeKeywordsCheckBox.IsChecked = options.CapitalizeKeywords;

      string casingTag = options.KeywordCasingOnSpace.ToString();
      foreach (ComboBoxItem item in KeywordCasingComboBox.Items)
      {
        if (string.Equals(item.Tag?.ToString(), casingTag, StringComparison.OrdinalIgnoreCase))
        {
          KeywordCasingComboBox.SelectedItem = item;
          break;
        }
      }
    }

    public void SaveSettings()
    {
      var options = OptionService.General;
      if (options == null)
      {
        return;
      }

      options.RootFolder = RootFolderTextBox.Text?.Trim() ?? @"C:\LiaSqlPrompt";
      options.CustomKeywordsFilePath = KeywordsFilePathTextBox.Text?.Trim() ?? "keywords.json";
      options.CapitalizeKeywords = CapitalizeKeywordsCheckBox.IsChecked == true;

      if (KeywordCasingComboBox.SelectedItem is ComboBoxItem selectedItem &&
          Enum.TryParse<KeywordCasing>(selectedItem.Tag?.ToString(), true, out var casing))
      {
        options.KeywordCasingOnSpace = casing;
      }

      options.SaveSettingsToStorage();
      KeywordsManager.Reload();
    }

    private void BrowseRootFolderButton_Click(object sender, RoutedEventArgs e)
    {
      using var dialog = new System.Windows.Forms.FolderBrowserDialog
      {
        Description = "Select LiaSqlPrompt Root Folder",
        SelectedPath = RootFolderTextBox.Text
      };

      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        RootFolderTextBox.Text = dialog.SelectedPath;
        SaveSettings();
        ShowStatus("Updated root folder setting.", isError: false);
      }
    }

    private void BrowseKeywordsFileButton_Click(object sender, RoutedEventArgs e)
    {
      using var dialog = new System.Windows.Forms.OpenFileDialog
      {
        Title = "Select Keywords JSON File",
        Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
        FileName = KeywordsFilePathTextBox.Text
      };

      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        KeywordsFilePathTextBox.Text = dialog.FileName;
        SaveSettings();
        ShowStatus("Updated keywords file path setting.", isError: false);
      }
    }

    private void ResetKeywordsButton_Click(object sender, RoutedEventArgs e)
    {
      var result = MessageBox.Show(
          "Are you sure you want to reset 'keywords.json' back to default?\n\nThis will delete your customized keywords/functions list and restore all keywords using the built-in extension definitions.",
          "Reset Keywords to Default",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

      if (result != MessageBoxResult.Yes)
      {
        return;
      }

      // Save any pending path changes first
      SaveSettings();

      bool success = KeywordsManager.ResetToDefaultFile(out string message);
      if (success)
      {
        ShowStatus(message, isError: false);
        MessageBox.Show(
            message,
            "Keywords Reset Completed",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
      else
      {
        ShowStatus(message, isError: true);
        MessageBox.Show(
            message,
            "Keywords Reset Failed",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
    }

    private void OpenKeywordsFolderButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        string filePath = KeywordsManager.GetKeywordsFilePath();
        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
          Process.Start(new ProcessStartInfo("explorer.exe", directory) { UseShellExecute = true });
        }
        else
        {
          ShowStatus($"Directory '{directory}' does not exist yet.", isError: true);
        }
      }
      catch (Exception ex)
      {
        ShowStatus($"Could not open folder: {ex.Message}", isError: true);
      }
    }

    private void ShowStatus(string message, bool isError)
    {
      StatusTextBlock.Text = message;
      StatusTextBlock.Foreground = isError ? Brushes.Firebrick : Brushes.DarkGreen;
      StatusTextBlock.Visibility = Visibility.Visible;
    }
  }
}
