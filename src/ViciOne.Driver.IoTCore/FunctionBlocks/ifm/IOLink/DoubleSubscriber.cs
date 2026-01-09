using System;
using System.Globalization;
using ViciOne.Core.Contracts.Runtime;

namespace ifm.IOLink;

public sealed partial class DoubleSubscriber
{
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;
    protected override IRuntimeConnector<string> UnitRuntimeConnector => ProcessDataOutputs.Unit;

    protected override void SetValueToConnectors(object value, string displayFormat)
    {
        if (value is
            decimal or
            double or
            float or
            byte or
            ushort or
            uint or
            ulong or
            sbyte or
            short or
            int or
            long)
        {
            // TODO: passen alle Konvertierungen?
            SetConnectorValue(ProcessDataOutputs.Value, RemoveConversionError(Convert.ToDouble(value, CultureInfo.InvariantCulture)));
        }
        else if (value is string stringValue)
        {
            if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDoubleValue))
            {
                SetConnectorValue(ProcessDataOutputs.Value, RemoveConversionError(parsedDoubleValue));
            }
        }
    }

    private static double RemoveConversionError(double value)
        => (double)(decimal)value;
}
