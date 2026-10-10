using System;

namespace LiaSqlPrompt
{
  public static class PackageIds
  {
    public const string PackageGuidString = "7ab28415-54a1-47b2-95a4-9e910a75e118";
    public static readonly Guid PackageGuid = new Guid(PackageGuidString);

    public const string CommandSetGuidString = "8bb6cf4b-4a57-4b89-a2e6-a05d21a28723";
    public static readonly Guid CommandSetGuid = new Guid(CommandSetGuidString);

    public const int FormatKeywordsUppercaseCommandId = 0x0100;
    public const int FormatKeywordsLowercaseCommandId = 0x0101;
  }
}
