using Dalamud.Plugin.Ipc;
using Gearbook.Core.Bis;
using Gearbook.Services;

namespace Gearbook.Adapters;

/// <summary>
/// Where best-in-slot information comes from, if anywhere.
/// </summary>
internal interface IBisProvider : IDisposable
{
    /// <summary>The last answer. Never null, and <see cref="BisSnapshot.Unavailable"/> until
    /// something answers.</summary>
    BisSnapshot Current { get; }

    /// <summary>
    /// Asks again, if anything could have changed. Cheap to call; it does the work at most once
    /// per invalidation.
    /// </summary>
    void RefreshIfStale();

    /// <summary>
    /// Marks the cached answer as out of date. Called when the gearset list changes, because the
    /// answer is keyed by gearset number and a reorder makes every key wrong.
    /// </summary>
    void Invalidate();
}

/// <summary>
/// Reads the sibling plugin's interface, if it is there.
/// </summary>
/// <remarks>
/// <para>
/// The dependency points one way: Eorzea Arsenal offers, Gearbook asks. Never the reverse and
/// never both, because two plugins that need each other can no longer be released
/// independently.
/// </para>
/// <para>
/// Three normal outcomes, all meaning the same thing to the interface: the gate is absent
/// because the other plugin is not installed, it carries a different version, or the call throws
/// because the other plugin is reloading. None of them is an error and none of them is reported
/// to the player as one.
/// </para>
/// <para>
/// The answer is cached and never fetched per frame. The cache is thrown away whenever the
/// gearset list changes, because the answer is keyed by the game's gearset number and a reorder
/// would otherwise show one set's badge against another. A wrong badge is worse than no badge:
/// it looks like an answer.
/// </para>
/// </remarks>
internal sealed class ArsenalBisProvider : IBisProvider
{
    /// <summary>
    /// The call gate. The version is in the name rather than in a field, so the other side can
    /// offer a second version alongside this one instead of changing both at once.
    /// </summary>
    private const string GateName = "EorzeaArsenal.GearsetBis.V1";

    private readonly ICallGateSubscriber<string?>? gate;
    private bool stale = true;
    private bool loggedFailure;

    public ArsenalBisProvider()
    {
        try
        {
            gate = GearbookServices.PluginInterface.GetIpcSubscriber<string?>(GateName);
        }
        catch (Exception ex)
        {
            // Subscribing is not supposed to throw, but it reaches another plugin's registry and
            // this must not be able to stop Gearbook from loading.
            GearbookServices.Log.Debug(ex, "The best-in-slot gate could not be subscribed to.");
            gate = null;
        }
    }

    /// <inheritdoc />
    public BisSnapshot Current { get; private set; } = BisSnapshot.Unavailable;

    /// <inheritdoc />
    public void Invalidate() => stale = true;

    /// <inheritdoc />
    public void RefreshIfStale()
    {
        if (!stale)
        {
            return;
        }

        stale = false;

        if (gate is null)
        {
            Current = BisSnapshot.Unavailable;
            return;
        }

        try
        {
            var payload = gate.InvokeFunc();
            Current = BisPayloadParser.Parse(payload, ReportOnce);
            loggedFailure = false;
        }
        catch (Exception ex)
        {
            // Absent, a different version, or reloading. All three are normal and mean the same
            // thing: no badge, carry on.
            Current = BisSnapshot.Unavailable;
            ReportOnce($"The best-in-slot provider did not answer: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to unsubscribe: a subscriber holds no registration on the other side. The
        // method exists so that the teardown block stays symmetrical with the rest and nobody
        // has to work out which adapters need releasing.
        Current = BisSnapshot.Unavailable;
    }

    /// <summary>
    /// Logs a failure once rather than on every refresh. A provider that is simply not installed
    /// would otherwise fill the log with a message about a normal state.
    /// </summary>
    private void ReportOnce(string message)
    {
        if (loggedFailure)
        {
            return;
        }

        loggedFailure = true;
        GearbookServices.Log.Debug("{Message}", message);
    }
}
