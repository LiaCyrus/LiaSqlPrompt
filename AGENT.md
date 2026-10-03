# LiaSqlPrompt — Agent Instructions

## Project Overview

LiaSqlPrompt is a SQL Server VSIX extension that provides SQL editor assistance.

Current primary features:

* SQL snippet completion
* SQL keyword completion
* Automatic SQL keyword capitalization

The project targets .NET Framework 4.7.2.

## Repository Structure

```text
LiaSqlPrompt/
├── LiaSqlPrompt.slnx
├── LiaSqlPrompt/
│   ├── LiaSqlPrompt.csproj
│   ├── LiaSqlPromptPackage.cs
│   ├── KeywordsManager.cs
│   ├── Logger.cs
│   │
│   ├── Completion/
│   │   ├── CompletionItem.cs
│   │   ├── CompletionPresenter.cs
│   │   ├── CompletionService.cs
│   │   └── CompletionSession.cs
│   │
│   ├── Editor/
│   │   ├── SqlCommandFilter.cs
│   │   ├── SqlCompletionController.cs
│   │   └── SqlEditorListener.cs
│   │
│   ├── Option/
│   │   ├── GeneralOption.cs
│   │   ├── OptionService.cs
│   │   ├── SnippetOption.cs
│   │   ├── SnippetOptionControl.xaml
│   │   ├── SnippetOptionControl.xaml.cs
│   │   └── SnippetOptionPage.cs
│   │
│   ├── Snippet/
│   │   ├── SnippetDefinition.cs
│   │   ├── SnippetLoader.cs
│   │   ├── SnippetRepository.cs
│   │   ├── SnippetService.cs
│   │   ├── SnippetWriter.cs
│   │   └── *.snippet.xml
│   │
│   └── SqlEngine/
│       ├── SqlEngine.csproj
│       ├── Keywords/
│       │   ├── KeywordsLoader.cs
│       │   ├── SqlKeywords.cs
│       │   └── sql-keywords.json
│       ├── Lexing/
│       │   ├── SqlToken.cs
│       │   └── SqlTokenizer.cs
│       └── AssemblyInfo.cs
│
├── README.md
├── LICENSE
├── source.extension.vsixmanifest
├── UninstallScript.bat
└── ...
```

## Important Directories

### `Completion/`

Contains the completion system.

Responsibilities include:

* Completion items
* Completion presentation
* Completion service
* Completion sessions

Changes to completion behavior should generally be implemented here rather than directly inside the Visual Studio command filter.

### `Editor/`

Contains Visual Studio editor integration.

Responsibilities include:

* Attaching to SQL editor text views
* Intercepting editor commands/keystrokes
* Controlling completion behavior from editor events
* Connecting Visual Studio editor APIs to the completion system

`SqlCommandFilter` should primarily deal with editor command/input handling rather than containing the complete completion implementation.

### `Snippet/`

Contains SQL snippet functionality.

Responsibilities include:

* Loading snippet definitions
* Storing/accessing snippets
* Snippet-related services
* Sample `.snippet.xml` definitions

Snippet data and snippet processing should remain separate from the Visual Studio editor integration where practical.

### `Option/`

Contains user-configurable extension options and option-related services.

### `SqlEngine/`

Contains SQL-related engine functionality that should be independent from the Visual Studio editor layer where possible.

Avoid introducing Visual Studio editor dependencies into this layer unless there is a specific architectural reason.

## Build Artifacts

The following directories/files are generated and should generally **not be manually modified**:

```text
bin/
obj/
```

Build output such as:

```text
bin/Debug/net472/
obj/Debug/net472/
```

should be treated as generated artifacts.

## Architectural Guidelines

Prefer keeping responsibilities separated:

```text
Visual Studio Editor
        ↓
     Editor/
        ↓
   Completion/
   Snippet/
   SqlEngine/
```

The editor layer should coordinate with the underlying services rather than containing all business logic.

When adding functionality:

1. Determine which existing component owns the responsibility.
2. Prefer extending an existing service over adding duplicate logic.
3. Keep Visual Studio-specific code in `Editor/` when possible.
4. Keep SQL/snippet logic independent of the editor layer when possible.
5. Avoid unnecessary architectural changes for small feature changes.

## Code Changes

Before modifying code:

* Inspect the existing implementation and call flow.
* Follow existing naming and organization conventions.
* Reuse existing services and models where appropriate.
* Avoid introducing new abstractions unless they provide a clear benefit.
* Do not modify generated `bin/` or `obj/` files.

For changes involving completion, inspect both:

```text
Completion/
Editor/
```

because completion behavior is coordinated between these areas.

For changes involving snippets, inspect:

```text
Snippet/
Completion/
```

because snippets may be exposed through the completion system.

## Dependencies

This is a Visual Studio extension, so changes involving editor behavior should consider Visual Studio SDK/API constraints.

Do not replace Visual Studio SDK mechanisms with unrelated approaches without first understanding why the current implementation uses them.

## Agent Context

Additional project-specific documentation may be added under:

```text
.agent/
```

Example:

```text
.agent/
├── architecture.md
├── folder-structure.md
├── coding-rules.md
├── completion.md
├── snippets.md
└── decisions/
```

Read the relevant `.agent/` documentation when working on a specific subsystem.