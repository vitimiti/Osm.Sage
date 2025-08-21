# Osm.Sage.BrowserDispatch

This library is simply used to open a link in the default browser in a cross-platform manner.

This was originally an IDL COM dispatch library using `stdole32.tlb` and `stdole2.tlb`. Since this system was used to
open a link in your browser in Windows, I've rewritten it to use the `System.Diagnostics.Process` class.

---

## Table of Contents

- [Usage](#usage)

---

## Usage

Simply pass in a URL to open in the default browser:

```csharp
BrowserDispatch.Open("https://www.google.com");
```
