#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>
#import <stdlib.h>

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
    unoedit_log(@"firstRectForCharacterRange range=%@ rect=%@ actualRange=%@", NSStringFromRange(range), NSStringFromRect(rect), actualRange != NULL ? NSStringFromRange(*actualRange) : @"<null>");
    return rect;
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

void unoedit_ime_update_caret_rect(void* bridgeHandle, double x, double y, double width, double height)
{
    UnoEditMacInputBridge* bridge = (__bridge UnoEditMacInputBridge*)bridgeHandle;
    if (bridge == nil || bridge.window == nil || bridge.window.contentView == nil || bridge.textView == nil) {
        unoedit_log(@"unoedit_ime_update_caret_rect ignored: bridge/window/contentView/textView missing.");
        return;
    }

    NSView* contentView = bridge.window.contentView;
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
    unoedit_log(@"unoedit_ime_update_caret_rect frame=%@ bounds=%@ flipped=%d", NSStringFromRect(bridge.textView.frame), NSStringFromRect(bounds), isFlipped);
}

}