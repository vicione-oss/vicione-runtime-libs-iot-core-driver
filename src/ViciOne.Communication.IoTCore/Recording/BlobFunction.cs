namespace ViciOne.Communication.IoTCore.Recording;

internal enum BlobFunction : byte
{
    BLOB_Info_Read = 0x10,
    BLOB_Info_Write = 0x11,
    BLOB_Last_Segment = 0x30,
    BLOB_CRC = 0x40
}
