**Accessibility changes — Folding & Announcements**

- Added focusable fold markers so keyboard users can expand/collapse folds.
- Added a hidden live-region `LiveRegionAnnouncer` in `TextView.xaml` that is updated when folds toggle.
- Added `AutomationProperties.Name` for fold markers using `FoldMarkerAutomationName` on `TextLineViewModel`.
- Keyboard shortcut `Ctrl+M` toggles fold at caret and announces the change.

How it works:
- Folding toggles update `LiveRegionAnnouncer.Text` with a short message (e.g., "Collapsed lines 10 to 20").
- Screen readers will announce the changed text as a live-region event.

Next accessibility improvements to consider:
- Make fold markers reachable via screen-reader-only semantics and ensure the tab order is intuitive.
- Add per-fold `AutomationProperties.HelpText` or `AccessKey` hints.
- E2E accessibility tests using a platform test runner (e.g., Axe + automation) to verify announcements.
