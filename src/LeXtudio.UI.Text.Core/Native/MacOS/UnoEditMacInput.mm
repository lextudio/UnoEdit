#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>
#import <stdlib.h>
#import <stdint.h>
#import <math.h>

typedef void (*unoedit_insert_text_fn)(void* context, const char* text);
typedef void (*unoedit_command_fn)(void* context, const char* command);

static BOOL unoedit_debug_enabled(void)
{
    const char* value = getenv("UNOEDIT_DEBUG_MACOS_IME");
    if (value == NULL) {
        return NO;
    }

    return strcmp(value, "1") == 0 || strcasecmp(value, "true") == 0;
}

static void unoedit_log(NSString* format, ...)
{
    if (!unoedit_debug_enabled()) {
        return;
    }

    va_list args;
    va_start(args, format);
    NSString* message = [[NSString alloc] initWithFormat:format arguments:args];
    va_end(args);
    NSLog(@"[UnoEdit macOS IME] %@", message);
}

@interface UnoEditInputTextView : NSTextView

@property(assign) void* managedContext;
@property(assign) unoedit_insert_text_fn insertTextCallback;
@property(assign) unoedit_command_fn commandCallback;
@property(assign) NSRect imeCaretRectInWindow;

@end

@implementation UnoEditInputTextView

+ (NSFont *)unoedit_editorFont
{
    NSFont* font = [NSFont fontWithName:@"Menlo" size:13.0];
    if (font != nil) {
        return font;
    }

    if (@available(macOS 10.15, *)) {
        return [NSFont monospacedSystemFontOfSize:13.0 weight:NSFontWeightRegular];
    }

    return [NSFont userFixedPitchFontOfSize:13.0] ?: [NSFont systemFontOfSize:13.0];
}

- (instancetype)initWithFrame:(NSRect)frameRect
{
    self = [super initWithFrame:frameRect];
    if (self) {
        self.drawsBackground = NO;
        self.editable = YES;
        self.selectable = YES;
        self.richText = NO;
        self.importsGraphics = NO;
        self.automaticQuoteSubstitutionEnabled = NO;
        self.automaticDashSubstitutionEnabled = NO;
        self.automaticDataDetectionEnabled = NO;
        self.automaticSpellingCorrectionEnabled = NO;
        self.automaticTextReplacementEnabled = NO;
        self.continuousSpellCheckingEnabled = NO;
        self.grammarCheckingEnabled = NO;
        self.font = [UnoEditInputTextView unoedit_editorFont];
        self.textColor = NSColor.clearColor;
        self.insertionPointColor = NSColor.clearColor;
        self.alphaValue = 1.0;
        self.selectedTextAttributes = @{
            NSBackgroundColorAttributeName: NSColor.clearColor,
            NSForegroundColorAttributeName: NSColor.clearColor,
        };
        self.typingAttributes = @{
            NSFontAttributeName: self.font,
            NSForegroundColorAttributeName: NSColor.clearColor,
        };
        self.string = @"";
        self.selectedRange = NSMakeRange(0, 0);
        self.imeCaretRectInWindow = frameRect;
        unoedit_log(@"UnoEditInputTextView init frame=%@", NSStringFromRect(frameRect));
    }

    return self;
}

- (BOOL)acceptsFirstResponder
{
    return YES;
}

- (BOOL)isOpaque
{
    return NO;
}

- (void)drawRect:(NSRect)dirtyRect
{
}

- (void)drawInsertionPointInRect:(NSRect)rect color:(NSColor *)color turnedOn:(BOOL)flag
{
}

- (BOOL)becomeFirstResponder
{
    BOOL result = [super becomeFirstResponder];
    unoedit_log(@"becomeFirstResponder result=%d firstResponder=%@", result, self.window.firstResponder);
    return result;
}

- (BOOL)resignFirstResponder
{
    BOOL result = [super resignFirstResponder];
    unoedit_log(@"resignFirstResponder result=%d firstResponder=%@", result, self.window.firstResponder);
    return result;
}

- (void)keyDown:(NSEvent *)event
{
    unoedit_log(@"keyDown keyCode=%hu characters=%@ charactersIgnoringModifiers=%@", event.keyCode, event.characters, event.charactersIgnoringModifiers);
    [super keyDown:event];
}

- (void)insertText:(id)string replacementRange:(NSRange)replacementRange
{
    NSString* committed = [string isKindOfClass:[NSAttributedString class]]
        ? [(NSAttributedString*)string string]
        : (NSString*)string;

    unoedit_log(@"insertText text=%@ replacementRange=%@", committed, NSStringFromRange(replacementRange));

    if (committed.length > 0 && self.insertTextCallback != NULL) {
        self.insertTextCallback(self.managedContext, committed.UTF8String);
    }

    self.string = @"";
    self.selectedRange = NSMakeRange(0, 0);
}

- (void)setMarkedText:(id)string selectedRange:(NSRange)selectedRange replacementRange:(NSRange)replacementRange
{
    NSString* marked = [string isKindOfClass:[NSAttributedString class]]
        ? [(NSAttributedString*)string string]
        : (NSString*)string;
    unoedit_log(@"setMarkedText text=%@ selectedRange=%@ replacementRange=%@", marked, NSStringFromRange(selectedRange), NSStringFromRange(replacementRange));
    [super setMarkedText:string selectedRange:selectedRange replacementRange:replacementRange];
}

- (void)unmarkText
{
    unoedit_log(@"unmarkText");
    [super unmarkText];
}

- (BOOL)hasMarkedText
{
    BOOL result = [super hasMarkedText];
    unoedit_log(@"hasMarkedText=%d", result);
    return result;
}

- (NSRange)markedRange
{
    NSRange range = [super markedRange];
    unoedit_log(@"markedRange=%@", NSStringFromRange(range));
    return range;
}

- (NSRange)selectedRange
{
    NSRange range = [super selectedRange];
    unoedit_log(@"selectedRange=%@", NSStringFromRange(range));
    return range;
}

- (NSRect)firstRectForCharacterRange:(NSRange)range actualRange:(nullable NSRangePointer)actualRange
{
    if (actualRange != NULL) {
        *actualRange = range;
    }

    NSRect windowRect = self.imeCaretRectInWindow;
    if (NSEqualRects(windowRect, NSZeroRect)) {
        windowRect = [self convertRect:self.bounds toView:nil];
    }

    NSRect rect = [self.window convertRectToScreen:windowRect];

    // Round returned rect to device pixels to avoid sub-pixel placement issues.
    NSScreen* screen = self.window.screen ?: [NSScreen mainScreen];
    CGFloat backing = screen.backingScaleFactor;
    NSRect rectInPixels = NSMakeRect(rect.origin.x * backing, rect.origin.y * backing, rect.size.width * backing, rect.size.height * backing);
    NSRect roundedRectInPixels = NSMakeRect(round(rectInPixels.origin.x), round(rectInPixels.origin.y), round(rectInPixels.size.width), round(rectInPixels.size.height));
    NSRect roundedRectInPoints = NSMakeRect(roundedRectInPixels.origin.x / backing, roundedRectInPixels.origin.y / backing, roundedRectInPixels.size.width / backing, roundedRectInPixels.size.height / backing);

    // convertRectToScreen: returns AppKit bottom-left origin screen coordinates,
    // which is exactly what firstRectForCharacterRange: must return per NSTextInputClient protocol.
    unoedit_log(@"firstRectForCharacterRange range=%@ screenRect=%@ rounded=%@ actualRange=%@", NSStringFromRange(range), NSStringFromRect(rect), NSStringFromRect(roundedRectInPoints), actualRange != NULL ? NSStringFromRange(*actualRange) : @"<null>");
    return roundedRectInPoints;
}

- (void)doCommandBySelector:(SEL)selector
{
    if (self.commandCallback != NULL) {
        NSString* name = NSStringFromSelector(selector);
        unoedit_log(@"doCommandBySelector %@", name);
        self.commandCallback(self.managedContext, name.UTF8String);
        return;
    }

    [super doCommandBySelector:selector];
}

- (void)copy:(id)sender
{
    if (self.commandCallback != NULL) {
        self.commandCallback(self.managedContext, "copy:");
    }
}

- (void)cut:(id)sender
{
    if (self.commandCallback != NULL) {
        self.commandCallback(self.managedContext, "cut:");
    }
}

- (void)paste:(id)sender
{
    if (self.commandCallback != NULL) {
        self.commandCallback(self.managedContext, "paste:");
    }
}

- (void)selectAll:(id)sender
{
    if (self.commandCallback != NULL) {
        self.commandCallback(self.managedContext, "selectAll:");
    }
}

- (void)undo:(id)sender
{
    if (self.commandCallback != NULL) {
        self.commandCallback(self.managedContext, "undo:");
    }
}

- (void)redo:(id)sender
{
    if (self.commandCallback != NULL) {
        self.commandCallback(self.managedContext, "redo:");
    }
}

@end

@interface UnoEditMacInputBridge : NSObject

@property(weak) NSWindow* window;
@property(strong) UnoEditInputTextView* textView;
@property(strong) id keyEventMonitor;

@end

@implementation UnoEditMacInputBridge
@end

extern "C" {

void* unoedit_ime_create(void* windowHandle, void* managedContext, unoedit_insert_text_fn insertTextCallback, unoedit_command_fn commandCallback)
{
    NSWindow* window = (__bridge NSWindow*)windowHandle;
    if (window == nil || window.contentView == nil) {
        unoedit_log(@"unoedit_ime_create failed: window or contentView was nil.");
        return NULL;
    }

    UnoEditMacInputBridge* bridge = [[UnoEditMacInputBridge alloc] init];
    bridge.window = window;
    bridge.textView = [[UnoEditInputTextView alloc] initWithFrame:NSMakeRect(0, 0, 2, 18)];
    bridge.textView.managedContext = managedContext;
    bridge.textView.insertTextCallback = insertTextCallback;
    bridge.textView.commandCallback = commandCallback;
    [window.contentView addSubview:bridge.textView];
    bridge.keyEventMonitor = [NSEvent addLocalMonitorForEventsMatchingMask:NSEventMaskKeyDown handler:^NSEvent * _Nullable(NSEvent * _Nonnull event) {
        if (bridge.window == nil || bridge.textView == nil) {
            return event;
        }

        if (bridge.window.firstResponder != bridge.textView) {
            return event;
        }

        unoedit_log(@"local key monitor keyCode=%hu characters=%@ charactersIgnoringModifiers=%@", event.keyCode, event.characters, event.charactersIgnoringModifiers);
        [bridge.textView keyDown:event];
        return nil;
    }];
    unoedit_log(@"unoedit_ime_create success window=%p contentView=%@ textView=%@", window, window.contentView, bridge.textView);
    NSScreen* screen = window.screen ?: [NSScreen mainScreen];
    CGFloat backing = screen.backingScaleFactor;
    unoedit_log(@"unoedit_ime_create backingScaleFactor=%f", backing);
    return (__bridge_retained void*)bridge;
}

void unoedit_ime_destroy(void* bridgeHandle)
{
    if (bridgeHandle == NULL) {
        return;
    }

    UnoEditMacInputBridge* bridge = (__bridge_transfer UnoEditMacInputBridge*)bridgeHandle;
    unoedit_log(@"unoedit_ime_destroy textView=%@", bridge.textView);
    if (bridge.keyEventMonitor != nil) {
        [NSEvent removeMonitor:bridge.keyEventMonitor];
        bridge.keyEventMonitor = nil;
    }
    [bridge.textView removeFromSuperview];
}

void unoedit_ime_focus(void* bridgeHandle, bool focus)
{
    UnoEditMacInputBridge* bridge = (__bridge UnoEditMacInputBridge*)bridgeHandle;
    if (bridge == nil || bridge.window == nil || bridge.textView == nil) {
        unoedit_log(@"unoedit_ime_focus ignored: bridge/window/textView missing.");
        return;
    }

    if (focus) {
        unoedit_log(@"unoedit_ime_focus requesting first responder. Existing firstResponder=%@", bridge.window.firstResponder);
        [bridge.window makeFirstResponder:bridge.textView];
        unoedit_log(@"unoedit_ime_focus completed. New firstResponder=%@", bridge.window.firstResponder);
    }
}

bool unoedit_ime_is_focused(void* bridgeHandle)
{
    UnoEditMacInputBridge* bridge = (__bridge UnoEditMacInputBridge*)bridgeHandle;
    if (bridge == nil || bridge.window == nil || bridge.textView == nil) {
        return false;
    }

    BOOL focused = bridge.window.firstResponder == bridge.textView;
    unoedit_log(@"unoedit_ime_is_focused=%d firstResponder=%@", focused, bridge.window.firstResponder);
    return focused;
}

void unoedit_ime_update_caret_rect(void* bridgeHandle, unsigned long long eventId, double x, double y, double width, double height)
{
    UnoEditMacInputBridge* bridge = (__bridge UnoEditMacInputBridge*)bridgeHandle;
    if (bridge == nil || bridge.window == nil || bridge.window.contentView == nil || bridge.textView == nil) {
        unoedit_log(@"unoedit_ime_update_caret_rect ignored: bridge/window/contentView/textView missing. id=%llu", (unsigned long long)eventId);
        return;
    }

    NSView* contentView = bridge.window.contentView;
    unoedit_log(@"unoedit_ime_update_caret_rect id=%llu raw x=%f y=%f w=%f h=%f", (unsigned long long)eventId, x, y, width, height);
    NSScreen* screen = bridge.window.screen ?: [NSScreen mainScreen];
    CGFloat backing = screen.backingScaleFactor;
    unoedit_log(@"unoedit_ime_update_caret_rect id=%llu backingScaleFactor=%f", (unsigned long long)eventId, backing);
    NSRect bounds = contentView.bounds;
    CGFloat caretWidth = (CGFloat)MAX(width, 2.0);
    CGFloat caretHeight = (CGFloat)MAX(height, 18.0);
    CGFloat caretX = (CGFloat)x;
    BOOL isFlipped = contentView.isFlipped;
    CGFloat caretY = isFlipped
        ? (CGFloat)y
        : (CGFloat)(bounds.size.height - y - caretHeight);

    bridge.textView.frame = NSMakeRect(caretX, caretY, caretWidth, caretHeight);
    bridge.textView.imeCaretRectInWindow = [contentView convertRect:bridge.textView.frame toView:nil];
    NSRect imeRectInWindow = bridge.textView.imeCaretRectInWindow;
    NSRect screenRect = [bridge.window convertRectToScreen:imeRectInWindow];

    // Convert to device pixels and round to integer pixel boundaries.
    NSRect screenRectInPixels = NSMakeRect(screenRect.origin.x * backing, screenRect.origin.y * backing, screenRect.size.width * backing, screenRect.size.height * backing);
    NSRect roundedScreenRectInPixels = NSMakeRect(round(screenRectInPixels.origin.x), round(screenRectInPixels.origin.y), round(screenRectInPixels.size.width), round(screenRectInPixels.size.height));
    NSRect roundedScreenRectInPoints = NSMakeRect(roundedScreenRectInPixels.origin.x / backing, roundedScreenRectInPixels.origin.y / backing, roundedScreenRectInPixels.size.width / backing, roundedScreenRectInPixels.size.height / backing);

    // Store the rounded rectangle in window coordinates so `firstRectForCharacterRange:` returns the rounded rect.
    NSRect roundedWindowRect = [bridge.window convertRectFromScreen:roundedScreenRectInPoints];
    bridge.textView.imeCaretRectInWindow = roundedWindowRect;

    unoedit_log(@"unoedit_ime_update_caret_rect id=%llu frame=%@ bounds=%@ imeCaretRectInWindow=%@ screenRect_points=%@ screenRect_pixels=%@ rounded_points=%@ rounded_pixels=%@ flipped=%d", (unsigned long long)eventId, NSStringFromRect(bridge.textView.frame), NSStringFromRect(bounds), NSStringFromRect(imeRectInWindow), NSStringFromRect(screenRect), NSStringFromRect(screenRectInPixels), NSStringFromRect(roundedScreenRectInPoints), NSStringFromRect(roundedScreenRectInPixels), isFlipped);
}

double unoedit_ime_get_backing_scale(void* bridgeHandle)
{
    UnoEditMacInputBridge* bridge = (__bridge UnoEditMacInputBridge*)bridgeHandle;
    if (bridge == nil || bridge.window == nil) {
        unoedit_log(@"unoedit_ime_get_backing_scale ignored: bridge/window missing.");
        return 1.0;
    }

    NSScreen* screen = bridge.window.screen ?: [NSScreen mainScreen];
    double backing = (double)screen.backingScaleFactor;
    unoedit_log(@"unoedit_ime_get_backing_scale=%f", backing);
    return backing;
}

void unoedit_ime_get_first_rect(void* bridgeHandle, double* outX, double* outY, double* outW, double* outH)
{
    UnoEditMacInputBridge* bridge = (__bridge UnoEditMacInputBridge*)bridgeHandle;
    if (bridge == nil || bridge.window == nil || bridge.textView == nil) {
        if (outX) *outX = 0.0;
        if (outY) *outY = 0.0;
        if (outW) *outW = 0.0;
        if (outH) *outH = 0.0;
        return;
    }

    NSRect windowRect = bridge.textView.imeCaretRectInWindow;
    if (NSEqualRects(windowRect, NSZeroRect)) {
        windowRect = [bridge.textView convertRect:bridge.textView.bounds toView:nil];
    }

    NSRect rect = [bridge.window convertRectToScreen:windowRect];
    NSScreen* screen = bridge.window.screen ?: [NSScreen mainScreen];
    CGFloat backing = screen.backingScaleFactor;

    NSRect rectInPixels = NSMakeRect(rect.origin.x * backing, rect.origin.y * backing, rect.size.width * backing, rect.size.height * backing);
    NSRect roundedRectInPixels = NSMakeRect(round(rectInPixels.origin.x), round(rectInPixels.origin.y), round(rectInPixels.size.width), round(rectInPixels.size.height));
    NSRect roundedRectInPoints = NSMakeRect(roundedRectInPixels.origin.x / backing, roundedRectInPixels.origin.y / backing, roundedRectInPixels.size.width / backing, roundedRectInPixels.size.height / backing);

    if (outX) *outX = roundedRectInPoints.origin.x;
    if (outY) *outY = roundedRectInPoints.origin.y;
    if (outW) *outW = roundedRectInPoints.size.width;
    if (outH) *outH = roundedRectInPoints.size.height;
}

void unoedit_ime_compute_first_rect_from_rect(void* bridgeHandle, double x, double y, double width, double height, double* outX, double* outY, double* outW, double* outH)
{
    UnoEditMacInputBridge* bridge = (__bridge UnoEditMacInputBridge*)bridgeHandle;
    if (bridge == nil || bridge.window == nil || bridge.window.contentView == nil) {
        if (outX) *outX = 0.0;
        if (outY) *outY = 0.0;
        if (outW) *outW = 0.0;
        if (outH) *outH = 0.0;
        return;
    }

    NSView* contentView = bridge.window.contentView;
    NSRect bounds = contentView.bounds;
    CGFloat caretWidth = (CGFloat)MAX(width, 2.0);
    CGFloat caretHeight = (CGFloat)MAX(height, 18.0);
    CGFloat caretX = (CGFloat)x;
    BOOL isFlipped = contentView.isFlipped;
    CGFloat caretY = isFlipped ? (CGFloat)y : (CGFloat)(bounds.size.height - y - caretHeight);

    NSRect frame = NSMakeRect(caretX, caretY, caretWidth, caretHeight);
    NSRect imeRectInWindow = [contentView convertRect:frame toView:nil];
    NSRect screenRect = [bridge.window convertRectToScreen:imeRectInWindow];

    NSScreen* screen = bridge.window.screen ?: [NSScreen mainScreen];
    CGFloat backing = screen.backingScaleFactor;

    NSRect screenRectInPixels = NSMakeRect(screenRect.origin.x * backing, screenRect.origin.y * backing, screenRect.size.width * backing, screenRect.size.height * backing);
    NSRect roundedScreenRectInPixels = NSMakeRect(round(screenRectInPixels.origin.x), round(screenRectInPixels.origin.y), round(screenRectInPixels.size.width), round(screenRectInPixels.size.height));
    NSRect roundedScreenRectInPoints = NSMakeRect(roundedScreenRectInPixels.origin.x / backing, roundedScreenRectInPixels.origin.y / backing, roundedScreenRectInPixels.size.width / backing, roundedScreenRectInPixels.size.height / backing);

    if (outX) *outX = roundedScreenRectInPoints.origin.x;
    if (outY) *outY = roundedScreenRectInPoints.origin.y;
    if (outW) *outW = roundedScreenRectInPoints.size.width;
    if (outH) *outH = roundedScreenRectInPoints.size.height;
}

}
