using NPlug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NPlug.Interop;
using System.Buffers;

namespace NPlugWebView;

public class WebviewControllerBindings<TControllerModel> : IBindings
    where TControllerModel : AudioProcessorModel, new()
{
    private readonly Dictionary<string, FunctionBinding> bindings = new();
    private readonly ThreadChecker threadChecker;
    private readonly AudioController<TControllerModel> controller;

    public WebviewControllerBindings(AudioController<TControllerModel> controller)
    {
        threadChecker = new();
        this.controller = controller;
        DeclareJSBinding("getParameterObject", BindCallback(GetParameterObject));
    }

    private static string Convert(string utf16Str)
    {
        // Convert a UTF-16 string to an UTF-8 string
        return Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(utf16Str));
    }

    private static string SerializeParameter(AudioParameter param)
    {
        var info = param.GetInfo();

        using MemoryBufferWriter memoryBufferWriter = MemoryBufferWriter.Get();
        using Utf8JsonWriter doc = new((IBufferWriter<byte>)memoryBufferWriter);

        doc.WriteStartObject();
        doc.WriteNumber("normalized", param.NormalizedValue);
        doc.WriteNumber("precision", param.Precision);
        doc.WriteNumber("unitID", param.Unit!.Id.Value);

        doc.WriteStartObject("info");
        doc.WriteNumber("id", info.Id.Value);
        doc.WriteString("title", Convert(info.Title));
        doc.WriteNumber("stepCount", info.StepCount);
        doc.WriteNumber("flags", (byte)info.Flags);
        doc.WriteNumber("defaultNormalizedValue", info.DefaultNormalizedValue);
        doc.WriteString("units", info.Units);
        doc.WriteString("shortTitle", Convert(info.ShortTitle));
        doc.WriteEndObject();  // end of "info" object
        

        if (param is AudioRangeParameter rangeParam)
        {
            doc.WriteBoolean("isRangeParameter", true);
            doc.WriteNumber("min", rangeParam.MinValue);
            doc.WriteNumber("max", rangeParam.MaxValue);
        }
        else
            doc.WriteBoolean("isRangeParameter", false);

        doc.WriteEndObject();
        doc.Flush();

        return Encoding.UTF8.GetString(memoryBufferWriter.GetSpan());
    }

    //public unsafe class ParameterDependenciesProxy : FObject
    //{
    //    private Webview webview_;

    //    public ParameterDependenciesProxy(Webview webview)
    //    {
    //        webview_ = webview;
    //    }

    //    public override void Update(LibVst.FUnknown changedUnknown, int message)
    //    {
    //        if (webview_ is null)
    //            return;

    //        changedUnknown.queryInterface(AudioRangeParameter)
    //        AudioRangeParameter changed_param;
    //        if (changed_param = LibVst.QueryInterface<LibVst.FUnknown, AudioParameter>(&changedUnknown) != Result.Ok)
    //            return;

    //        var serializedParameter = SerializeParameter(changed_param);
    //        webview_.EvalJS($"notifyParameterChange({serializedParameter});", r => { });
    //    }
    //}

    public void Bind(Webview webview)
    {
        foreach (var binding in bindings)
        {
            webview.BindFunction(binding.Key, binding.Value);
        }
    }

    public delegate string CallbackFn(Webview webview, string jsonArg);

    private static FunctionBinding BindCallback(CallbackFn fn)
    {
        return (arg1, arg2, arg3, jsonArg) => fn(arg1, jsonArg);
    }

    public string GetParameterObject(Webview webview, string json)
    {
        threadChecker.Test();
        var id = 0;
        var info = ((IAudioController)controller).GetParameterInfo(id);
        return SerializeParameter(info);
    }

    public void DeclareJSBinding(string name, FunctionBinding functionBinding)
    {
        bindings[name] = functionBinding;
    }
}
