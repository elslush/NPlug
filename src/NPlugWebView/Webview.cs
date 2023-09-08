using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NPlugWebView;

public enum SizeHint
{
    None,  // Width and height are default size
    Min,   // Width and height are minimum bounds
    Max,   // Width and height are maximum bounds
    Fixed  // Window size cannot be changed by a user
}

public delegate void DispatchFunction();
public delegate void ResultCallback(string jsonResult);
public delegate string FunctionBinding(Webview webview, int seq, string name, string jsonData);


public abstract class Webview
{
    private Dictionary<string, FunctionBinding> bindings = new();

    public abstract void SetTitle(string title);
    public abstract void SetViewSize(int width, int height, SizeHint hint = SizeHint.None);
    public abstract string ContentRootURI();
    public abstract void Navigate(string url);
    public abstract void EvalJS(string js, ResultCallback callback);
    public abstract void OnDocumentCreate(string js);
    public abstract IntPtr PlatformWindow();
    public abstract void Terminate();

    public void BindFunction(string name, FunctionBinding binding)
    {
        var js = $@"(function() {{ 
            var name = '{name}';
            var RPC = window._rpc = (window._rpc || {{nextSeq: 1}});
            window[name] = function() {{
                var seq = RPC.nextSeq++;
                var promise = new Promise(function(resolve, reject) {{
                    RPC[seq] = {{
                        resolve: resolve,
                        reject: reject,
                    }};
                }});
                window.external.invoke(JSON.stringify({{
                    id: seq,
                    method: name,
                    params: Array.prototype.slice.call(arguments),
                }}));
                return promise;
            }}
        }})()
        ";

        OnDocumentCreate(js);
        bindings[name] = binding;
    }

    public void UnbindFunction(string name)
    {
        if (bindings.ContainsKey(name))
        {
            var js = $"delete window['{name}'];";

            OnDocumentCreate(js);
            EvalJS(js, j => { });
            bindings.Remove(name);
        }
    }

    public void ResolveFunctionDispatch(int seq, int status, ReadOnlyMemory<char> result)
    {
        DispatchIn(() => {
            if (status == 0)
            {
                EvalJS($"window._rpc[{seq}].resolve({result}); delete window._rpc[{seq}];", j => { });
            }
            else
            {
                EvalJS($"window._rpc[{seq}].reject({result}); delete window._rpc[{seq}];", j => { });
            }
        });
    }

    internal readonly struct BrowserMessage
    {
        [JsonPropertyName("id")]
        public int Id { get; }

        [JsonPropertyName("method")]
        public string Name { get; }

        [JsonPropertyName("params")]
        public string Params { get; }
    }

    protected virtual void OnBrowserMessage(string message)
    {
        var msgParsed = JsonSerializer.Deserialize(message, NPlugSerializationContext.Default.BrowserMessage);
        int seq = msgParsed.Id;
        string name = msgParsed.Name;
        var args = msgParsed.Params;

        if (!bindings.TryGetValue(name, out var binding))
        {
            return;
        }

        var result = binding(this, seq, name, args);
        if (result is not null)
            ResolveFunctionDispatch(seq, 0, result.AsMemory());
    }

    protected abstract void DispatchIn(DispatchFunction function);
}
