using System;
using System.Globalization;
using ViciOne.Core.Contracts.Runtime;

namespace ifm.IOLink;

public sealed partial class LongSubscriber
{
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;
    protected override IRuntimeConnector<string> UnitRuntimeConnector => ProcessDataOutputs.Unit;

    protected override void SetValueToConnectors(object value, string displayFormat)
    {
        if (value is
            byte or
            ushort or
            uint or
            sbyte or
            short or
            int or
            long)
        {
            SetConnectorValue(ProcessDataOutputs.Value, Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
        else if (value is string stringValue)
        {
            if (long.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out long parsedLongValue))
            {
                SetConnectorValue(ProcessDataOutputs.Value, parsedLongValue);
            }
        }
    }
}
