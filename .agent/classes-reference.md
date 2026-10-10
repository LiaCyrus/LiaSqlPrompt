# LiaSqlPrompt — Class & Subsystem Reference

This document summarizes every class, struct, enum, and interface across the LiaSqlPrompt solution according to the architectural structure defined in [AGENT.md](file:///c:/MyGitHub/LiaSqlPrompt/AGENT.md).
Agents can read this single reference instead of scanning individual source files.

---

## 1. Top-Level Extension & Management (`LiaSqlPrompt/`)

| File / Component | Type | Responsibility & Key Members |
| :--- | :--- | :--- |
| [LiaSqlPromptPackage.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/LiaSqlPromptPackage.cs) | `LiaSqlPromptPackage : AsyncPackage` | **VSIX Extension Package Entry Point**.<br>• Registers package with VS shell (`ProvideAutoLoad`, `ProvideOptionPage`, `ProvideMenuResource`).<br>• Registers VS command `LiaSqlPrompt.FormatKeywordsUppercase` via `OleMenuCommandService` with default shortcut **Ctrl+B, Ctrl+U**.<br>• Initializes options via `OptionService.Initialize(this)`.<br>• Initializes custom keywords via `KeywordsManager.Initialize()`. |
| [LiaSqlPromptPackage.vsct](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/LiaSqlPromptPackage.vsct) | XML Command Table | **Visual Studio Command Table**.<br>• Defines `LiaSqlPrompt.FormatKeywordsUppercase` command (`0x0100`).<br>• Binds the command to standard VS/SSMS keyboard shortcuts in Tools > Options > Environment > Keyboard (`Ctrl+B, Ctrl+U`). |
| [PackageIds.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/PackageIds.cs) | `static class PackageIds` | **Command & Package GUID Constants**.<br>• Defines `PackageGuid`, `CommandSetGuid`, and `FormatKeywordsUppercaseCommandId`. |
| [KeywordsManager.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/KeywordsManager.cs) | `static class KeywordsManager` | **Keywords File & Watcher Manager**.<br>• `Initialize()` / `Reload()`: Loads keywords from the configured JSON file (`C:\LiaSqlPrompt\keywords.json` by default). Generates default JSON file if missing.<br>• `GetKeywordsFilePath()`: Resolves full path to active keywords JSON file.<br>• `ResetToDefaultFile(out string message)`: Overwrites user keywords JSON with the extension's default embedded JSON, resetting `KeywordsLoader.Current` and `SqlKeywords` immediately.<br>• Syncs loaded keywords into `SqlEngine.SqlKeywords.SetKeywords(...)`.<br>• `SetupFileWatcher()`: Uses `FileSystemWatcher` with debounce timer to auto-reload keywords on file change. |
| [Logger.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Logger.cs) | `static class Logger` | **File Logging Helper**.<br>• `Log(string message)`: Appends timestamped log lines to `C:\LiaSqlPrompt\LiaSqlPrompt.log`. Swallows exceptions to prevent crashes. |

---

## 2. Editor Integration (`LiaSqlPrompt/Editor/`)

Visual Studio editor layer connecting editor events, commands, and text views to the completion system and SqlEngine.

| File / Component | Type | Responsibility & Key Members |
| :--- | :--- | :--- |
| [SqlEditorListener.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Editor/SqlEditorListener.cs) | `SqlEditorListener : IWpfTextViewCreationListener` | **MEF Text View Listener**.<br>• Listens for text view creation (`TextViewCreated`).<br>• Tracks active editor controller in `ActiveController`.<br>• Obtains `IVsTextView` adapter and injects `SqlCommandFilter` into the OLE command chain.<br>• Instantiates `SqlCompletionController` per editor view. |
| [SqlCommandFilter.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Editor/SqlCommandFilter.cs) | `SqlCommandFilter : IOleCommandTarget` | **Command & Keystroke Interceptor**.<br>• Intercepts VS command group `VSConstants.VSStd2K` (`TYPECHAR`, `BACKSPACE`, `TAB`, `RETURN`, `CANCEL`, `UP`, `DOWN`, `COMPLETEWORD`).<br>• Intercepts `PackageIds.CommandSetGuid` command `FormatKeywordsUppercaseCommandId`.<br>• Forwards key events to `SqlCompletionController` methods (`HandleSpace`, `HandleTypeChar`, `HandleTab`, etc.).<br>• Calls `CheckAndYieldIfNativeActive()` after delegating to the native target to avoid conflicts with native SSMS IntelliSense. |
| [SqlCompletionController.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Editor/SqlCompletionController.cs) | `sealed class SqlCompletionController` | **Editor Completion & Auto-Casing Orchestrator**.<br>• `HandleSpace()` / `HandleTypeChar(ch)`: Triggers automatic keyword capitalization via `FormatKeywordBeforeCaret()`.<br>• `FormatKeywordBeforeCaret()`: Calls `SqlEngine.SqlKeywordCaser.TryGetKeywordFormatReplacement` and updates the text buffer.<br>• `FormatAllKeywordsToUppercase()`: Formats SQL keywords in the currently highlighted/selected text to uppercase via the registered VS shortcut command (**Ctrl+B, Ctrl+U**).<br>• `UpdateCompletion(...)`: Extracts the current word at caret, checks context (suppresses popup after `.` or after `FROM`/`JOIN`/`INTO`/`UPDATE`/`TABLE`), queries `CompletionService.Search()`, and displays results via `CompletionPresenter`.<br>• `CommitSelection()`: Accepts current completion item into the text buffer.<br>• `CheckAndYieldIfNativeActive()`: Closes custom completion popup if native IntelliSense becomes active. |

---

## 3. Completion Subsystem (`LiaSqlPrompt/Completion/`)

Manages completion item models, item lookup, and UI popup presentation.

| File / Component | Type | Responsibility & Key Members |
| :--- | :--- | :--- |
| [CompletionItem.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Completion/CompletionItem.cs) | `sealed class CompletionItem` | **Completion Data Model**.<br>• `Label`: Display text shown in completion list.<br>• `InsertText`: Text inserted into the editor buffer upon commit.<br>• `Description`: Secondary description text (e.g. "SQL Keyword" or snippet description). |
| [CompletionService.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Completion/CompletionService.cs) | `sealed class CompletionService` | **Completion Query Service**.<br>• Aggregates suggestions from `SnippetService.Search(prefix)` and `SqlKeywords.All`.<br>• Formats keywords based on `OptionService.General.CapitalizeKeywords`.<br>• `Search(string prefix)`: Returns combined, alphabetically-ordered list of `CompletionItem`s matching the prefix. |
| [CompletionPresenter.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Completion/CompletionPresenter.cs) | `sealed class CompletionPresenter` | **WPF Completion UI Popup**.<br>• Hosts a WPF `Popup` and `ListBox` anchored to the caret position in `IWpfTextView`.<br>• `Show(...)` / `Hide()`: Controls popup visibility and item binding.<br>• `MoveNext()` / `MovePrevious()`: Navigates item selection.<br>• `Accept(view, span)`: Replaces the tracked snapshot span with the selected item's `InsertText`.<br>• Tracks viewport layout changes, focus loss, and view closure to dismiss or reposition the popup. |

---

## 4. Snippet Subsystem (`LiaSqlPrompt/Snippet/`)

Loads, parses, stores, persists, and searches XML-based SQL snippets (`*.snippet.xml`).

| File / Component | Type | Responsibility & Key Members |
| :--- | :--- | :--- |
| [SnippetDefinition.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Snippet/SnippetDefinition.cs) | `sealed class SnippetDefinition` | **Snippet Data Model**.<br>• Properties: `Title`, `Shortcut`, `Description`, `Code`, and optional `FilePath`. |
| [SnippetLoader.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Snippet/SnippetLoader.cs) | `sealed class SnippetLoader` | **Snippet XML Deserializer**.<br>• `Load(string filePath)`: Parses XML document using `XDocument`, extracts `<Header>` tags (`Title`, `Shortcut`, `Description`) and normalized `<Code>` CDATA/content. |
| [SnippetWriter.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Snippet/SnippetWriter.cs) | `sealed class SnippetWriter` | **Snippet XML Serializer & File Manager**.<br>• `Save(folderPath, snippet, overwrite)`: Writes or overwrites `<Snippet>` XML with CDATA code block to `{safeShortcut}.snippet.xml`.<br>• `Delete(filePath)`: Deletes snippet file from disk. |
| [SnippetRepository.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Snippet/SnippetRepository.cs) | `sealed class SnippetRepository` | **Snippet Storage Singleton**.<br>• `Instance`: Singleton accessor.<br>• `Load()` / `Reload()`: Reads all `*.snippet.xml` files from `OptionService.Snippet.SnippetFolder` via `SnippetLoader`.<br>• `GetAll()`: Returns cached snippet definitions. |
| [SnippetService.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Snippet/SnippetService.cs) | `sealed class SnippetService` | **Snippet Query Service**.<br>• Wraps `SnippetRepository`.<br>• `Search(string prefix)`: Finds snippet definitions whose `Shortcut` starts with the given prefix (case-insensitive). |

---

## 5. Options & Configuration (`LiaSqlPrompt/Option/`)

Manages user settings, Tools > Options dialog pages, and WPF UI management.

| File / Component | Type | Responsibility & Key Members |
| :--- | :--- | :--- |
| [GeneralOption.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Option/GeneralOption.cs) | `GeneralOption : DialogPage` | **General Settings Model**.<br>• `RootFolder`: Root directory (`C:\LiaSqlPrompt`).<br>• `CapitalizeKeywords`: Boolean controlling completion insertion casing.<br>• `KeywordCasingOnSpace`: `SqlEngine.KeywordCasing` (Uppercase, Lowercase, Disabled) applied on typing delimiters.<br>• `CustomKeywordsFilePath`: JSON file path for keywords.<br>• `OnApply(...)`: Invokes `KeywordsManager.Reload()`. |
| [GeneralOptionPage.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Option/GeneralOptionPage.cs) | `GeneralOptionPage : UIElementDialogPage` | **Visual Studio General Option Page Host**.<br>• Hosts `GeneralOptionControl` in Tools > Options > Lia SQL Prompt > General. |
| [GeneralOptionControl.xaml.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Option/GeneralOptionControl.xaml.cs) | `partial class GeneralOptionControl : UserControl` | **General Options GUI**.<br>• UI for configuring Root Folder, keywords JSON file path, casing preferences.<br>• Features **"Reset keywords.json to Default"** button that restores the built-in extension keyword definitions. |
| [SnippetOption.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Option/SnippetOption.cs) | `SnippetOption : DialogPage` | **Snippet Configuration Model**.<br>• `SnippetFolder`: Path to snippet files (`C:\LiaSqlPrompt\Snippet`).<br>• `OnApply(...)`: Invokes `SnippetRepository.Instance.Reload()`. |
| [SnippetOptionPage.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Option/SnippetOptionPage.cs) | `SnippetOptionPage : UIElementDialogPage` | **Visual Studio Option Page Host**.<br>• Hosts `SnippetOptionControl` inside Tools > Options > Lia SQL Prompt > Snippets. |
| [SnippetOptionControl.xaml.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Option/SnippetOptionControl.xaml.cs) | `partial class SnippetOptionControl : UserControl` | **Snippet Manager GUI**.<br>• WPF UI for browsing, creating, filtering, updating, and deleting snippets.<br>• Connects directly to `SnippetRepository` and `SnippetWriter`. |
| [OptionService.cs](file:///c:/MyGitHub/LiaSqlPrompt/LiaSqlPrompt/Option/OptionService.cs) | `static class OptionService` | **Global Settings Accessor**.<br>• `Initialize(package)`: Caches references to `GeneralOption` and `SnippetOption`.<br>• Provides static accessors `OptionService.General` and `OptionService.Snippet`. |

---

## 6. SQL Engine — Keywords & Lexing (`SqlEngine/Keywords/` & `SqlEngine/Lexing/`)

SQL-level lexical processing and keyword categorization (independent of Visual Studio SDK).

| File / Component | Type | Responsibility & Key Members |
| :--- | :--- | :--- |
| [KeywordCasing.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Keywords/KeywordCasing.cs) | `enum KeywordCasing` | `Uppercase`, `Lowercase`, `Disabled`. |
| [SqlKeywordType.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Keywords/SqlKeywordType.cs) | `enum SqlKeywordType` | **Semantic Keyword Categories**.<br>• Defines categories (`StatementStart`, `QueryClause`, `WhereExpression`, `JoinCondition`, `DataType`, `Function`, `CreateObject`, `Transaction`, etc.) for typed keyword declarations. |
| [SqlKeywords.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Keywords/SqlKeywords.cs) | `static class SqlKeywords` | **Central Keyword Registry**.<br>• Initializes built-in keywords from `TSqlTokenType` (ScriptDom) and embedded `sql-keywords.json`.<br>• `All`: Thread-safe collection of all active keywords (built-in + custom).<br>• `BuiltIn`: Built-in keywords collection.<br>• `IsKeyword(word)`: Checks case-insensitively if word is registered.<br>• `SetKeywords(custom)` / `ResetToDefault()`: Allows dynamic reloading from custom definitions.<br>• `GetDefaultKeywordsJsonStream()` / `GetDefaultKeywordsJsonString()`: Extracts default keywords embedded resource. |
| [KeywordsLoader.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Keywords/KeywordsLoader.cs) | `TypedSqlKeyword`<br>`KeywordsConfiguration`<br>`static class KeywordsLoader` | **Unified Single Source of Truth for Keywords**.<br>• `TypedSqlKeyword`: Model for explicit `{"keyword": "...", "type": "..."}` declaration.<br>• `KeywordsConfiguration`: Categorized lists for all contextual keyword groups (`statementStart`, `queryClauses`, `whereExpressions`, `dataTypes`, `functions`, `typedKeywords`, etc.). Provides `GetKeywordsForKind(SqlCompletionContextKind)` and `GetAllItems()`.<br>• `KeywordsLoader`: Maintains `KeywordsLoader.Current` instance reloaded at runtime via `KeywordsManager.Reload()`. |
| [SqlKeywordCaser.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Lexing/SqlKeywordCaser.cs) | `static class SqlKeywordCaser` | **Smart Keyword Casing & Context Evaluator**.<br>• Delegates `IsPrecededByDot` and `IsInTableSourceContext` directly to `SqlCompletionContextAnalyzer` to eliminate duplicate parsing.<br>• `TryGetKeywordFormatReplacement(...)`: Validates whether the token at offset requires casing change; outputs replacement span and formatted string.<br>• `FormatAllKeywords(...)`: Scans the entire document SQL text and returns replacements for all SQL keywords to match the given casing preference (ignoring table/column qualifiers and table names). |

---

## 7. SQL Engine — Parsing & AST (`SqlEngine/Parsing/` & `SqlEngine/Ast/`)

Transact-SQL AST representation and Microsoft ScriptDom integration.

| File / Component | Type | Responsibility & Key Members |
| :--- | :--- | :--- |
| [SqlParsedToken.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Parsing/SqlParsedToken.cs) | `enum SqlParserTokenType`<br>`sealed class SqlParsedToken` | **Parser Token Bridge**.<br>• `SqlParserTokenType`: `Whitespace`, `Keyword`, `Identifier`, `StringLiteral`, `NumberLiteral`, `Comment`, `Symbol`, `Dot`, `LeftParenthesis`, `RightParenthesis`, `EndOfFile`, `Other`.<br>• `SqlParsedToken`: Holds token classification, text, offset, and `IsKeyword` flag, decoupling consumers from ScriptDom types. |
| [SqlQueryParser.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Parsing/SqlQueryParser.cs) | `sealed class SqlQueryParser` | **ScriptDom Parser & AST Builder**.<br>• `Parse(sql)`: Uses `TSql160Parser` to produce `SqlQueryResult` containing AST statements (`SelectStatement`, `InsertStatement`, `UpdateStatement`, etc.) and syntax errors.<br>• `Tokenize(sql)`: Returns raw ScriptDom `TSqlParserToken` stream.<br>• `ParseTokens(sql)`: Returns strongly-typed `SqlParsedToken` list for lexing and casing checks.<br>• Contains conversion methods converting ScriptDom fragments into clean AST nodes. |
| [SqlAstEnums.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Ast/SqlAstEnums.cs) | `enum StatementType`<br>`enum JoinType`<br>`enum OrderDirection`<br>`enum BinaryOperator`<br>`enum UnaryOperator` | Enumerations representing statement types, join types (Inner, LeftOuter, Cross, etc.), order direction, binary and unary operators. |
| [SqlAstNode.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Ast/SqlAstNode.cs) | `struct SourceSpan`<br>`abstract class SqlAstNode` | Base types for all AST nodes containing text offsets (`StartOffset`, `Length`, `StartLine`, `StartColumn`). |
| [SqlStatements.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Ast/SqlStatements.cs) | `SqlStatement`<br>`SelectStatement`<br>`InsertStatement`<br>`UpdateStatement`<br>`DeleteStatement`<br>`MergeStatement`<br>`ExecuteStatement`<br>`UnknownStatement` | AST statement nodes representing T-SQL query types and their clauses. |
| [SqlClauses.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Ast/SqlClauses.cs) | Clause & Source AST classes | AST definitions for query clauses:<br>• `SelectItem`, `SelectClause`<br>• `TableSource`, `NamedTableSource`, `DerivedTableSource`, `TableValuedFunctionSource`<br>• `JoinClause`, `FromClause`<br>• `WhereClause`, `GroupByClause`, `HavingClause`<br>• `OrderByClause`, `OrderByElement`<br>• `CteDefinition`, `SetAssignment`. |
| [SqlExpressions.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Ast/SqlExpressions.cs) | Expression AST classes | AST definitions for SQL expressions:<br>• `SqlExpression` (base)<br>• `ColumnReferenceExpression`, `LiteralExpression`, `VariableExpression`<br>• `FunctionCallExpression`, `BinaryExpression`, `UnaryExpression`<br>• `CaseExpression`, `WhenClause`<br>• `CastExpression`, `ConvertExpression`<br>• `SubqueryExpression`, `InExpression`, `BetweenExpression`, `LikeExpression`, `ExistsExpression`, `IsNullExpression`, `StarExpression`, `RawTextExpression`. |
| [SqlQueryResult.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Ast/SqlQueryResult.cs) | `SqlParseError`<br>`SqlQueryResult` | Result wrapper returned by `SqlQueryParser.Parse(...)` containing parsed statements, error list (`HasErrors`), and convenient `FirstStatement` accessor. |

---

## 8. SQL Engine — Completion (`SqlEngine/Completion/`)

Syntactic position analysis and contextual SQL keyword suggestions (completely independent of Visual Studio SDK).

| File / Component | Type | Responsibility & Key Members |
| :--- | :--- | :--- |
| [SqlCompletionContextKind.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Completion/SqlCompletionContextKind.cs) | `enum SqlCompletionContextKind` | Expanded classification of syntactic positions covering all T-SQL statement families (`Select`, `Insert`, `Update`, `Delete`, `Merge`, `Create`, `Alter`, `Drop`, `Execute`, `Declare`, `Set`, `IfCondition`, `WhileCondition`, `BeginBlock`, `TransactionStatement`, `CteStart`, `ColumnReference`, etc.). |
| [SqlCompletionContext.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Completion/SqlCompletionContext.cs) | `sealed class SqlCompletionContext` | **Completion Context Model**.<br>• `Kind`: Active syntactic context kind.<br>• `StatementType`: Current statement type (`Select`, `Insert`, `Update`, `Delete`, `Merge`, `Execute`, `Unknown`).<br>• `Prefix`: Current typed completion prefix.<br>• `TableAlias` / `TableName`: Detected table qualifier/alias (for column reference context).<br>• `CursorPosition`: Cursor character offset. |
| [SqlCompletionContextAnalyzer.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Completion/SqlCompletionContextAnalyzer.cs) | `static class SqlCompletionContextAnalyzer` | **Cursor-Aware Multi-Statement Context Analyzer**.<br>• `Analyze(sql, cursorPosition)`: Handles incomplete SQL, multiple statements (separated by `;` or `GO`), subqueries (nested parentheses), CTEs, DML (`INSERT`, `UPDATE`, `DELETE`, `MERGE`), DDL (`CREATE`, `ALTER`, `DROP`), procedural execution (`EXEC`), variable declaration (`DECLARE`, `SET`), control flow (`IF`, `WHILE`, `BEGIN...END`), and transactions.<br>• Accurately extracts prefixes and isolates inner scopes. |
| [SqlCompletionKeywordProvider.cs](file:///c:/MyGitHub/LiaSqlPrompt/SqlEngine/Completion/SqlCompletionKeywordProvider.cs) | `static class SqlCompletionKeywordProvider` | **Contextual Keyword Filter**.<br>• `GetKeywords(context)`: Maps the active context to valid, filtered T-SQL keywords for query continuation, expressions, DML targets, DDL object types (`TABLE`, `VIEW`, `INDEX`, etc.), data types (`INT`, `NVARCHAR`, etc.), table alteration actions (`ADD`, `ALTER COLUMN`), and transaction commands. Suppresses keywords in object/column naming positions. |

