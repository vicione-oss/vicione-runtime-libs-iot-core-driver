using IFM.SDK.VSE.Parameters;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// A single Vse variant - a named configuration profile that activates a subset of the device's objects.
/// </summary>
public sealed class Variant(IVariant instance)
{
    public int Id => instance.Id;
    public IReadOnlyList<int> ActivatedObjects => instance.ActivatedObjects;
}
