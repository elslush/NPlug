using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static NPlugWebView.Webview;

namespace NPlugWebView;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(BrowserMessage))]
internal partial class NPlugSerializationContext : JsonSerializerContext
{
}
