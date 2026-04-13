# LeXtudio.UI.Text.Core

Minimal cross-platform text/IME primitives for editor integrations.

Usage example:

```csharp
using LeXtudio.UI.Text.Core;

var ctx = new CoreTextEditContext();

ctx.TextRequested += (s, e) =>
{
    // Provide current editor text
    e.Request.Text = "replacement text";
};

ctx.SelectionRequested += (s, e) =>
{
    e.Request.Start = 0;
    e.Request.Length = 0;
};
```

This project intentionally keeps a small, dependency-free API surface so it can be referenced
by editor hosts or platform bridge adapters.
