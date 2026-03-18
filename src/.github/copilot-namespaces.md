File-scoped Namespace Preference

This repository prefers C# file-scoped namespace declarations to keep files concise and consistent.

Rule:
- Use file-scoped namespace syntax at the top of C# files.
  Example:

  namespace GatewayHost.Modules.Bff;

Rationale:
- Keeps files shorter and reduces indentation.
- Matches modern C# style and improves readability in vertical-slice features.

How to apply:
- When adding or updating C# files, use file-scoped namespaces.
- When refactoring, convert block namespaces to file-scoped namespaces where appropriate.

Notes:
- This is a repository-level preference. If a file-level exception is required, document the exception in the same folder's README.
