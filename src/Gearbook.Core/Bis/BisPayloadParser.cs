using System.Text.Json;

namespace Gearbook.Core.Bis;

/// <summary>
/// Turns the provider's answer into a <see cref="BisSnapshot"/>.
/// </summary>
/// <remarks>
/// <para>
/// The string comes from another plugin, over an interface neither side controls alone, and
/// versions on both sides move independently. So this accepts what it understands, ignores what
/// it does not, and never throws: an exception here would travel up through a draw callback.
/// </para>
/// <para>
/// Unknown fields are ignored rather than rejected. That is what lets the other side add fields
/// later without breaking older builds of this one, and it is the reason the contract is a JSON
/// string rather than a shared type. Plugins are loaded in separate contexts, so a type declared
/// on both sides with the same name is not the same type.
/// </para>
/// </remarks>
public static class BisPayloadParser
{
    /// <summary>
    /// Parses an answer. Anything unreadable yields
    /// <see cref="BisProviderState.Unavailable"/>, which shows nothing at all.
    /// </summary>
    /// <param name="payload">The provider's answer, or null when it did not answer.</param>
    /// <param name="onError">Called with a reason when the answer could not be read, so a
    /// malformed contract reaches the log rather than vanishing into a silent absence.</param>
    public static BisSnapshot Parse(string? payload, Action<string>? onError = null)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return BisSnapshot.Unavailable;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                onError?.Invoke("The best-in-slot provider returned something that is not an object.");
                return BisSnapshot.Unavailable;
            }

            var state = ReadState(root);
            var entries = ReadEntries(root);

            return new BisSnapshot(state, entries);
        }
        catch (JsonException ex)
        {
            onError?.Invoke($"The best-in-slot provider returned invalid JSON: {ex.Message}");
            return BisSnapshot.Unavailable;
        }
    }

    private static BisProviderState ReadState(JsonElement root)
    {
        if (!root.TryGetProperty("state", out var element) || element.ValueKind != JsonValueKind.String)
        {
            // No state field at all. An older provider that only ever sent entries, so treat
            // the presence of an answer as the answer.
            return BisProviderState.Ok;
        }

        return element.GetString()?.ToLowerInvariant() switch
        {
            "ok" => BisProviderState.Ok,
            "noaccount" => BisProviderState.NoAccount,
            "loading" => BisProviderState.Loading,
            "nodata" => BisProviderState.NoData,

            // A state this build has never heard of. Not an error and not a reason to discard
            // the entries, so it is treated as a working provider and the entries decide what
            // is shown.
            _ => BisProviderState.Ok,
        };
    }

    private static Dictionary<int, BisEntry> ReadEntries(JsonElement root)
    {
        var entries = new Dictionary<int, BisEntry>();

        if (!root.TryGetProperty("entries", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return entries;
        }

        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!TryReadInt(element, "id", out var slot))
            {
                continue;
            }

            if (!TryReadInt(element, "matched", out var matched)
                || !TryReadInt(element, "total", out var total))
            {
                continue;
            }

            // A total of zero would render as "3 of 0", which is worse than showing nothing.
            // Negative values are impossible and mean the other side is confused about
            // something, so the entry is dropped rather than displayed.
            if (total <= 0 || matched < 0 || matched > total)
            {
                continue;
            }

            var target = element.TryGetProperty("target", out var targetElement)
                         && targetElement.ValueKind == JsonValueKind.String
                ? targetElement.GetString() ?? string.Empty
                : string.Empty;

            // A duplicate id means the other side sent the same gearset twice. Keep the first
            // and ignore the rest rather than letting the last one win silently.
            if (!entries.ContainsKey(slot))
            {
                entries[slot] = new BisEntry(slot, matched, total, target);
            }
        }

        return entries;
    }

    private static bool TryReadInt(JsonElement element, string name, out int value)
    {
        value = 0;

        if (!element.TryGetProperty(name, out var property))
        {
            return false;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value);
    }
}
