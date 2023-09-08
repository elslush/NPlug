// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.IO;

namespace NPlug;

/// <summary>
/// The controller part of an effect or instrument with parameter handling (export, definition, conversion...).
/// </summary>
public interface IAudioController : IAudioPluginComponent
{
    /// <summary>
    /// Receives the component state
    /// </summary>
    /// <param name="streamInput">The component input state.</param>
    void SetComponentState(Stream streamInput);

    /// <summary>
    /// Sets the controller state
    /// </summary>
    /// <param name="streamInput">The input state.</param>
    void SetState(Stream streamInput);

    /// <summary>
    /// Gets the controller state
    /// </summary>
    /// <param name="streamOutput">The output state.</param>
    void GetState(Stream streamOutput);

    /// <summary>
    /// Returns the number of parameters exported.
    /// </summary>
    int ParameterCount { get; }

    /// <summary>
    /// Gets for a given index the parameter information.
    /// </summary>
    /// <param name="paramIndex">The index of the parameter.</param>
    /// <returns>The parameter information.</returns>
    AudioParameter GetParameterInfo(int paramIndex);

    /// <summary>
    /// Gets for a given parameter id and normalized value its associated string representation
    /// </summary>
    /// <param name="id">The id of the parameter.</param>
    /// <param name="valueNormalized">The value normalized.</param>
    /// <returns>A string representation of the value.</returns>
    string GetParameterStringByValue(AudioParameterId id, double valueNormalized);

    /// <summary>
    /// Gets for a given parameter id and string its normalized value
    /// </summary>
    /// <param name="id">The id of the parameter.</param>
    /// <param name="valueAsString">The value as a string.</param>
    /// <returns>The normalized value of the string.</returns>
    double GetParameterValueByString(AudioParameterId id, string valueAsString);

    /// <summary>
    /// Returns for a given parameter id and a normalized value its plain representation.
    /// (for example -6 for -6dB).
    /// </summary>
    /// <param name="id">The id of the parameter.</param>
    /// <param name="valueNormalized">The value normalized.</param>
    /// <returns>The plain value.</returns>
    double NormalizedParameterToPlain(AudioParameterId id, double valueNormalized);

    /// <summary>
    /// Returns for a given parameter id and a plain value its normalized value.
    /// </summary>
    /// <param name="id">The id of the parameter.</param>
    /// <param name="plainValue">The plain value.</param>
    /// <returns>The normalized value.</returns>
    double PlainParameterToNormalized(AudioParameterId id, double plainValue);

    /// <summary>
    /// Returns the normalized value of the parameter associated to the parameter id.
    /// </summary>
    /// <param name="id">The id of the parameter.</param>
    /// <returns>The normalized value.</returns>
    double GetParameterNormalized(AudioParameterId id);

    /// <summary>
    /// Sets the normalized value to the parameter associated to the paramID. The controller must never
	/// pass this value-change back to the host via the <see cref="IAudioControllerHandler"/>.
	/// It should update the according GUI element(s) only.
    /// </summary>
    /// <param name="id">The id of the parameter.</param>
    /// <param name="valueNormalized">The value normalized.</param>
    void SetParameterNormalized(AudioParameterId id, double valueNormalized);

    /// <summary>
    /// Gets from host a handler which allows the Plugin-in to communicate with the host.
    /// Note: This is mandatory if the host is using the <see cref="IAudioController"/>.
    /// </summary>
    /// <param name="controllerHandler">The controller host.</param>
    void SetControllerHandler(IAudioControllerHandler? controllerHandler);

    /// <summary>
    /// Creates the editor view of the plug-in, currently only "editor" is supported, see \ref ViewType.
    /// The life time of the editor view will never exceed the life time of this controller instance.
    /// </summary>
    /// <returns>The plugin view.</returns>
    IAudioPluginView? CreateView();
}