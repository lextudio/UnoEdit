global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading.Tasks;
#if WINDOWS_APP_SDK
global using Windows.UI.Text.Core;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
#else
global using CoreTextEditContext = LeXtudio.UI.Text.Core.CoreTextEditContext;
global using CoreTextServicesManager = LeXtudio.UI.Text.Core.CoreTextServicesManager;
global using CoreTextInputScope = LeXtudio.UI.Text.Core.CoreTextInputScope;
global using CoreTextRange = LeXtudio.UI.Text.Core.CoreTextRange;
global using CoreTextTextRequestedEventArgs = LeXtudio.UI.Text.Core.CoreTextTextRequestedEventArgs;
global using CoreTextTextUpdatingEventArgs = LeXtudio.UI.Text.Core.CoreTextTextUpdatingEventArgs;
global using CoreTextSelectionRequestedEventArgs = LeXtudio.UI.Text.Core.CoreTextSelectionRequestedEventArgs;
global using CoreTextSelectionUpdatingEventArgs = LeXtudio.UI.Text.Core.CoreTextSelectionUpdatingEventArgs;
global using CoreTextLayoutRequestedEventArgs = LeXtudio.UI.Text.Core.CoreTextLayoutRequestedEventArgs;
global using CoreTextTextRequest = LeXtudio.UI.Text.Core.CoreTextTextRequest;
global using CoreTextCompositionStartedEventArgs = LeXtudio.UI.Text.Core.CoreTextCompositionStartedEventArgs;
global using CoreTextCompositionCompletedEventArgs = LeXtudio.UI.Text.Core.CoreTextCompositionCompletedEventArgs;
global using CoreTextCommandReceivedEventArgs = LeXtudio.UI.Text.Core.CoreTextCommandReceivedEventArgs;
#endif
